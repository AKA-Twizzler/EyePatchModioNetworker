using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Warehouse;
using MelonLoader;
using ModioModNetworker.UI;
using ModioModNetworker.Data;
using ModioModNetworker.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace ModioModNetworker;

public class ModFileManager
{
	public static string OAUTH_KEY = "";

	public static string API_PATH = "https://mod.io/v1/games/3809/mods/";

	public static string MOD_FOLDER_PATH = Application.persistentDataPath + "/Mods";

	public static string downloadingModId = "";

	public static string downloadPath = "";

	private static readonly HttpClient sharedApiClient = CreateApiClient();
	private static HttpClient CreateApiClient()
	{
		try
		{
			var handler = new SocketsHttpHandler
			{
				SslOptions = new System.Net.Security.SslClientAuthenticationOptions
				{
					RemoteCertificateValidationCallback = (sender, cert, chain, errors) => true
				},
				ConnectCallback = async (context, cancellationToken) =>
				{
					try
					{
						var host = context.DnsEndPoint.Host;
						var addresses = await Dns.GetHostEntryAsync(host);
						var ipv4 = Array.Find(addresses.AddressList, a => a.AddressFamily == AddressFamily.InterNetwork);
						if (ipv4 == null)
						{
							MelonLogger.Error("[HTTP] No IPv4 address found for " + host + " — trying default");
							var defaultSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
							await defaultSocket.ConnectAsync(context.DnsEndPoint, cancellationToken);
							return new NetworkStream(defaultSocket, ownsSocket: true);
						}
						var socket = new Socket(ipv4.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						await socket.ConnectAsync(new IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);
						return new NetworkStream(socket, ownsSocket: true);
					}
					catch (Exception ex)
					{
						MelonLogger.Error("[HTTP] ConnectCallback failed for " + context.DnsEndPoint.Host + ": " + ex.Message);
						throw;
					}
				},
				PooledConnectionLifetime = TimeSpan.FromMinutes(5),
				PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
				MaxConnectionsPerServer = 10
			};
			var client = new HttpClient(handler);
			client.Timeout = TimeSpan.FromSeconds(30);
			MelonLogger.Msg("[HTTP] Shared API HttpClient created (IPv4-forcing, pooled connections)");
			return client;
		}
		catch (Exception ex)
		{
			MelonLogger.Error("[HTTP] Failed to create shared HttpClient: " + ex.Message);
			return new HttpClient();
		}
	}

	public static bool isDownloading = false;

	public static bool queueAvailable = false;

	private static List<DownloadQueueElement> queue = new List<DownloadQueueElement>();

	public static bool fetchingSubscriptions = false;

	public static bool IsFetchingSubscriptions()
	{
		return fetchingSubscriptions;
	}

	public static bool fetchingTrending = false;

	public static DownloadAction activeDownloadAction = null;

	public static DownloadQueueElement activeDownloadQueueElement = null;

	public static UnityWebRequest activeDownloadWebRequest;

	public static string[] targetVersionStrings = new string[2] { "1.1", "1.2" };

	private static bool previousQueueHadItems = false;

	public static void Initialize()
	{
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
	}

	private static void OnDownloadFileCompleted()
	{
		activeDownloadAction = new DownloadAction(10);
	}

	public static string FindFile(string path, string fileName)
	{
		try
		{
			string[] files = Directory.GetFiles(path);
			foreach (string text in files)
			{
				string text2 = text.Split('\\')[^1];
				if (text2.EndsWith(fileName))
				{
					return text;
				}
			}
			string[] directories = Directory.GetDirectories(path);
			foreach (string path2 in directories)
			{
				string text3 = FindFile(path2, fileName);
				if (text3 != "")
				{
					return text3;
				}
			}
		}
		catch (Exception)
		{
			return "";
		}
		return "";
	}

	public static void OnDownloadProgressChanged(double progress)
	{
		if (ModlistMenu.activeDownloadModInfo != null)
		{
			ModlistMenu.activeDownloadModInfo.modDownloadPercentage = progress;
		}
	}

	public static void StopDownload()
	{
		if (isDownloading)
		{
			if (activeDownloadQueueElement != null && activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value))
			{
				value.Finish();
			}
			isDownloading = false;
			activeDownloadQueueElement = null;
			activeDownloadWebRequest = null;
			ModlistMenu.activeDownloadModInfo = null;
		}
	}

	public static void CheckQueue()
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Invalid comparison between Unknown and I4
		// Only log when there's actually something to process or a state change
		if (queue.Count == 0 && !MainClass.warehouseReloadRequested)
			return;
		// Only log on state transitions to avoid spam
		bool queueHasItems = queue.Count > 0 || isDownloading;
		if (queueHasItems != previousQueueHadItems)
		{
			previousQueueHadItems = queueHasItems;
			if (queueHasItems)
				MelonLogger.Msg("[DownloadQueue] Queue now active — items=" + queue.Count + " isDownloading=" + isDownloading);
			else
				MelonLogger.Msg("[DownloadQueue] Queue idle — all downloads complete");
		}
		if (isDownloading || AssetWarehouse.Instance == null || SceneStreamer._session == null || (int)SceneStreamer._session.Status == 1 || queue.Count <= 0)
		{
			return;
		}
		DownloadQueueElement downloadQueueElement = queue[0];
		activeDownloadQueueElement = downloadQueueElement;  // MUST be set BEFORE Download() for async capture
		if (downloadQueueElement.info.Download())
		{
			queue.RemoveAt(0);
			MelonLogger.Msg("[DownloadQueue] CheckQueue: Starting download for " + (downloadQueueElement.info.modName ?? downloadQueueElement.info.modId ?? "unknown"));
			MelonLogger.Msg("Downloading mod " + downloadQueueElement.info.modId);
			if (activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value))
			{
				value.Show();
			}
		}
		else
		{
			activeDownloadQueueElement = null;  // Download didn't start, clear
		}
		MainClass.menuRefreshRequested = true;
	}

	public static bool AddToQueue(DownloadQueueElement queueElement, bool ignoreTag = false)
	{
		ModInfo info = queueElement.info;
		MelonLogger.Msg("[DownloadQueue] AddToQueue: mod=" + (info.modName ?? info.modId ?? "unknown") + " numericalId=" + (info.numericalId ?? "0") + " sizeKB=" + info.fileSizeKB + " fromPlayer=" + (queueElement.associatedPlayer != null ? queueElement.associatedPlayer.ToString() : "none"));
		if (!info.isValidMod)
		{
			return false;
		}
		if (MainClass.blacklistedModIoIds.Contains(info.modId) || MainClass.blacklistedModIoIds.Contains(info.numericalId))
		{
			MelonLogger.Msg("[DownloadQueue] REJECTED: blacklisted for " + (info.modName ?? info.modId ?? "unknown"));
			return false;
		}
		if (info.IsSubscribed())
		{
			MelonLogger.Msg("[DownloadQueue] REJECTED: already subscribed for " + (info.modName ?? info.modId ?? "unknown"));
			return false;
		}
		if (!ignoreTag)
		{
			bool flag = false;
			foreach (string tag in info.tags)
			{
				if (Enumerable.Contains(targetVersionStrings, tag))
				{
					flag = true;
				}
			}
			if (!flag)
			{
				MelonLogger.Msg("[DownloadQueue] REJECTED: no version tag for " + (info.modName ?? info.modId ?? "unknown"));
				return false;
			}
		}
		if (activeDownloadQueueElement != null && (activeDownloadQueueElement.info.modId == info.modId || activeDownloadQueueElement.info.numericalId == info.numericalId))
		{
			MelonLogger.Msg("[DownloadQueue] REJECTED: already downloading for " + (info.modName ?? info.modId ?? "unknown"));
			return false;
		}
		if (info.mature && !MainClass.downloadMatureContent)
		{
			MelonLogger.Msg("[DownloadQueue] REJECTED: mature for " + (info.modName ?? info.modId ?? "unknown"));
			return false;
		}
		if (info.version == null)
		{
			info.version = "0.0.0";
		}
		bool flag2 = false;
		bool flag3 = false;
		foreach (ModInfo installedMod in MainClass.installedMods)
		{
			if (installedMod.numericalId == info.numericalId || installedMod.modId == info.modId)
			{
				flag2 = true;
				if (installedMod.version != info.version)
				{
					flag3 = true;
				}
				break;
			}
		}
		if (flag2 && !flag3)
		{
			MelonLogger.Msg("[DownloadQueue] REJECTED: already installed for " + (info.modName ?? info.modId ?? "unknown"));
			return false;
		}
		foreach (DownloadQueueElement item in queue)
		{
			if (item.info.modId == info.modId || item.info.numericalId == info.numericalId)
			{
				MelonLogger.Msg("[DownloadQueue] REJECTED: already in queue for " + (info.modName ?? info.modId ?? "unknown"));
				return false;
			}
		}
		queue.Add(queueElement);
		MelonLogger.Msg("[DownloadQueue] QUEUED: " + (info.modName ?? info.modId ?? "unknown") + " at position " + (queue.Count - 1) + " (queue size: " + queue.Count + ")");
		return true;
	}

	public static async void DownloadFileHttpClient(string url, string path)
	{
		ModInfo modInfo = activeDownloadQueueElement?.info;
		if (string.IsNullOrEmpty(url))
		{
			MelonLogger.Error("[DownloadQueue] DownloadFileHttpClient: URL is null or empty — cannot download");
			isDownloading = false;
			activeDownloadQueueElement = null;
			activeDownloadWebRequest = null;
			ModlistMenu.activeDownloadModInfo = null;
			return;
		}
		int lastProgressReported = 0;
		try
		{
			using HttpClient client = new HttpClient(new HttpClientHandler
			{
				ClientCertificateOptions = ClientCertificateOption.Manual,
				ServerCertificateCustomValidationCallback = (HttpRequestMessage httpRequestMessage, X509Certificate2? cert, X509Chain? cetChain, SslPolicyErrors policyErrors) => true
			});
			client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);
			client.Timeout = TimeSpan.FromSeconds(90);
			using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
			{
				if (response == null || response.Content == null)
				{
					MelonLogger.Error("[DownloadQueue] DownloadFileHttpClient: HTTP response or content is null for " + url);
					isDownloading = false;
					activeDownloadQueueElement = null;
					ModlistMenu.activeDownloadModInfo = null;
					return;
				}
				using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
				long totalBytes = response.Content.Headers.ContentLength ?? 0;
				long bytesRead = 0L;
				byte[] buffer = new byte[4096];
				using FileStream fs = new FileStream(path, FileMode.CreateNew);
				while (true)
				{
					int num;
					int bytesReceived = (num = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length));
					if (num <= 0)
					{
						break;
					}
					await fs.WriteAsync(buffer, 0, bytesReceived);
					bytesRead += bytesReceived;
					int progress = totalBytes > 0 ? (int)(bytesRead * 100 / totalBytes) : 0;
					if (lastProgressReported == 0)
					{
						MelonLogger.Msg("[DownloadProgress] " + (modInfo?.modName ?? modInfo?.modId ?? "unknown") + ": 0% — download started (" + bytesRead + "/" + totalBytes + " bytes)");
					}
					if (totalBytes > 0 && progress >= lastProgressReported + 25)
					{
						lastProgressReported = progress;
						MelonLogger.Msg("[DownloadProgress] " + (modInfo.modName ?? modInfo.modId ?? "unknown") + ": " + progress + "% (" + bytesRead + "/" + totalBytes + " bytes)");
					}
					double percentage = totalBytes > 0 ? (double)bytesRead / (double)totalBytes * 100.0 : 0.0;
					OnDownloadProgressChanged(percentage);
				}
				MelonLogger.Msg("[DownloadProgress] " + (modInfo.modName ?? modInfo.modId ?? "unknown") + ": Download complete (" + totalBytes + " bytes total)");
			}
			OnDownloadFileCompleted();
		}
		catch (Exception ex)
		{
			string modName = modInfo?.modName ?? modInfo?.modId ?? "unknown";
			MelonLogger.Error("[DownloadQueue] Download FAILED for " + modName + ": " + ex.Message);
			isDownloading = false;
			activeDownloadQueueElement = null;
			activeDownloadWebRequest = null;
			ModlistMenu.activeDownloadModInfo = null;
			MelonLogger.Msg("[DownloadQueue] Download queue state reset — ready for next download");
		}
	}

	public static async Task DownloadFileAsync(string url, string path)
	{
		DownloadFileHttpClient(url, path);
	}

	public static void DownloadFile(string url, string path)
	{
		downloadPath = path;
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		try
		{
			DownloadFileAsync(url, path);
		}
		catch (WebException ex)
		{
			isDownloading = false;
			ModlistMenu.activeDownloadModInfo = null;
			activeDownloadQueueElement = null;
			activeDownloadWebRequest = null;
			MelonLogger.Error("Failed to download file: " + ex.Message);
			throw;
		}
	}

	public static void QueueSubscriptions(int shown)
	{
		if (!fetchingSubscriptions)
		{
			fetchingSubscriptions = true;
			string url = "https://mod.io/v1/me/subscribed?_offset=" + shown + "&limit=400&game_id=3809";
			MelonLogger.Msg("[Subscription] QueueSubscriptions: Requesting URL: " + url);

			System.Threading.Tasks.Task.Run(async delegate
			{
				try
				{
				sharedApiClient.DefaultRequestHeaders.Remove("Authorization");
				sharedApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);

				HttpResponseMessage response = await sharedApiClient.GetAsync(url);
				string responseText = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					MainThreadManager.QueueAction(delegate
					{
						MainClass.subscriptionThreadString = responseText;
						MelonLogger.Msg("[Subscription] QueueSubscriptions SUCCESS: code=" + (int)response.StatusCode + " body length=" + responseText.Length);
						fetchingSubscriptions = false;
					});
				}
				else
				{
					string errorText = "url=" + url + " code=" + (int)response.StatusCode + " body=" + (responseText.Length > 200 ? responseText.Substring(0, 200) + "..." : responseText);
					MelonLogger.Error("[Subscription] QueueSubscriptions HTTP ERROR: " + errorText);
					MainThreadManager.QueueAction(delegate
					{
						fetchingSubscriptions = false;
						MainClass.HandleSubscriptionFailure();
					});
			}
			}
			catch (Exception ex)
			{
				string errorText = "url=" + url + " error=" + ex.GetType().Name + ": " + ex.Message;
				MelonLogger.Error("[Subscription] QueueSubscriptions FAILED: " + errorText);
				MainThreadManager.QueueAction(delegate
				{
					fetchingSubscriptions = false;
					MainClass.HandleSubscriptionFailure();
				});
			}
			});
		}
		else
		{
			MelonLogger.Warning("[Subscription] QueueSubscriptions skipped — already fetching");
		}
	}

	public static void QueueTrending(int offset, string searchQuery = "")
	{
		if (!fetchingTrending)
		{
			fetchingTrending = true;
			string text = "&_q=" + searchQuery;
			if (searchQuery == "")
			{
				text = "";
			}
			SpotlightOverride.LoadFromRegularURL();
			string url = $"https://mod.io/v1/games/@bonelab/mods?_limit=100&_offset={offset}&_sort=-popular" + text;
			MelonLogger.Msg("[Trending] QueueTrending: Requesting URL: " + url);

			System.Threading.Tasks.Task.Run(async delegate
			{
				try
				{
				sharedApiClient.DefaultRequestHeaders.Remove("Authorization");
				sharedApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);
				sharedApiClient.DefaultRequestHeaders.Remove("Accept");
				sharedApiClient.DefaultRequestHeaders.Add("Accept", "application/json");
				sharedApiClient.DefaultRequestHeaders.Remove("X-Modio-Platform");
				sharedApiClient.DefaultRequestHeaders.Add("X-Modio-Platform", "windows");
				sharedApiClient.DefaultRequestHeaders.Remove("X-Modio-Portal");
				sharedApiClient.DefaultRequestHeaders.Add("X-Modio-Portal", "steam");

				HttpResponseMessage response = await sharedApiClient.GetAsync(url);
				string responseText = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					MainThreadManager.QueueAction(delegate
					{
						MainClass.trendingThreadString = responseText;
						MelonLogger.Msg("[Trending] QueueTrending SUCCESS: code=" + (int)response.StatusCode);
						fetchingTrending = false;
					});
				}
				else
				{
					string bodyPreview = responseText.Length > 100 ? responseText.Substring(0, 100) + "..." : responseText;
					MelonLogger.Error("[Trending] QueueTrending HTTP ERROR: url=" + url + " code=" + (int)response.StatusCode + " body=" + bodyPreview);
					MainThreadManager.QueueAction(delegate
					{
						fetchingTrending = false;
					});
				}
				}
				catch (Exception ex)
				{
					MelonLogger.Error("[Trending] QueueTrending FAILED: url=" + url + " error=" + ex.GetType().Name + ": " + ex.Message);
					MainThreadManager.QueueAction(delegate
					{
						fetchingTrending = false;
					});
				}
			});
		}
	}

	public static void Subscribe(string numericalid)
	{
		string url = "https://mod.io/v1/games/3809/mods/" + numericalid + "/subscribe";
		MelonLogger.Msg("[Subscribe] Requesting: POST " + url);

		System.Threading.Tasks.Task.Run(async delegate
		{
			try
			{
				sharedApiClient.DefaultRequestHeaders.Remove("Authorization");
				sharedApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);

				var content = new FormUrlEncodedContent(new Dictionary<string, string>());
				HttpResponseMessage response = await sharedApiClient.PostAsync(url, content);
				string responseBody = await response.Content.ReadAsStringAsync();
				int statusCode = (int)response.StatusCode;

				if (statusCode == 200 || statusCode == 201)
				{
					MelonLogger.Msg("[Subscribe] SUCCESS: code=" + statusCode + " for mod " + numericalid);
					MainThreadManager.QueueAction(delegate
					{
						// Add to local subscribed list so UI updates immediately
						if (!MainClass.subscribedModIoNumericalIds.Contains(numericalid))
						{
							MainClass.subscribedModIoNumericalIds.Add(numericalid);
							MelonLogger.Msg("[Subscribe] Added " + numericalid + " to subscribedModIoNumericalIds");
						}
						if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
						{
							NetworkerMenuController.instance.UpdateModPopupButtons();
						}
					});
				}
				else
				{
					MelonLogger.Error("[Subscribe] FAILED: code=" + statusCode + " body=" + (responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody) + " for mod " + numericalid);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("[Subscribe] Exception for mod " + numericalid + ": " + ex.GetType().Name + ": " + ex.Message);
			}
		});
	}

	public static void UninstallAndUnsubscribe(string modId)
	{
		UnInstall(modId);
		UnSubscribe(modId);
	}

	public static void UnSubscribe(string numericalId)
	{
		string url = "https://mod.io/v1/games/3809/mods/" + numericalId + "/subscribe";
		MelonLogger.Msg("[UnSubscribe] Requesting: DELETE " + url);

		System.Threading.Tasks.Task.Run(async delegate
		{
			try
			{
				sharedApiClient.DefaultRequestHeaders.Remove("Authorization");
				sharedApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);

				// Must use HttpRequestMessage to set Content-Type on DELETE requests
				var request = new HttpRequestMessage(HttpMethod.Delete, url);
				request.Content = new FormUrlEncodedContent(new Dictionary<string, string>());
				HttpResponseMessage response = await sharedApiClient.SendAsync(request);
				string responseBody = await response.Content.ReadAsStringAsync();
				int statusCode = (int)response.StatusCode;

				if (statusCode == 204)
				{
					MelonLogger.Msg("[UnSubscribe] SUCCESS: code=" + statusCode + " for mod " + numericalId);
					MainThreadManager.QueueAction(delegate
					{
						MainClass.subscribedModIoNumericalIds.Remove(numericalId);
						MelonLogger.Msg("[UnSubscribe] Removed " + numericalId + " from subscribedModIoNumericalIds");

						if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
						{
							NetworkerMenuController.instance.UpdateModPopupButtons();
						}
					});
				}
				else
				{
					MelonLogger.Error("[UnSubscribe] FAILED: code=" + statusCode + " body=" + (responseBody.Length > 200 ? responseBody.Substring(0, 200) + "..." : responseBody) + " for mod " + numericalId);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("[UnSubscribe] Exception for mod " + numericalId + ": " + ex.GetType().Name + ": " + ex.Message);
			}
		});
	}

	public static void UnInstallMainThread(string numericalId)
	{
		MelonLogger.Msg("[UnInstall] UnInstallMainThread called for numericalId=" + numericalId);

		InstalledModInfo installedModInfo = null;
		foreach (InstalledModInfo installedModInfo2 in MainClass.InstalledModInfos)
		{
			if (installedModInfo2.ModInfo.numericalId == numericalId)
			{
				installedModInfo = installedModInfo2;
				break;
			}
		}

		if (installedModInfo == null)
		{
			MelonLogger.Warning("[UnInstall] No InstalledModInfo found for numericalId=" + numericalId + " — cannot uninstall");
		}

		try
		{
			if (installedModInfo != null)
			{
				string palletBarcode = installedModInfo.palletBarcode;
				MelonLogger.Msg("[UnInstall] Unloading pallet: " + palletBarcode);
				UnloadPallet(palletBarcode);
				MelonLogger.Msg("[UnInstall] Deleting manifest: " + installedModInfo.manifestPath);
				File.Delete(installedModInfo.manifestPath);
				string fullName = Directory.GetParent(installedModInfo.catalogPath).FullName;
				MelonLogger.Msg("[UnInstall] Deleting directory: " + fullName);
				Directory.Delete(fullName, recursive: true);
				MelonLogger.Msg("[UnInstall] Successfully uninstalled numericalId=" + numericalId);
			}
		}
		catch (Exception ex)
		{
			if (ex is System.IO.DirectoryNotFoundException || ex is System.IO.FileNotFoundException)
			{
				MelonLogger.Warning("[UnInstall] Files already deleted for " + numericalId + " — skipping RequestInstallCheck to prevent retry loop");
				// Don't call RequestInstallCheck - files are already gone
				return;
			}
			MelonLogger.Error("[UnInstall] Exception when uninstalling mod " + numericalId + ": " + ex);
		}
		MelonLogger.Msg("[UnInstall] Calling RequestInstallCheck after uninstall");
		MainClass.RequestInstallCheck();
	}

	private static void UnloadPallet(string palletBarcode)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		DeleteExistingModObjects(palletBarcode);
		try
		{
			AssetWarehouse.Instance.UnloadPallet(new Barcode(palletBarcode));
		}
		catch (Exception)
		{
		}
	}

	public static void DeleteExistingModObjects(string palletBarcode)
	{
		try
		{
			var enumerator = AssetWarehouse.Instance.GetPallets().GetEnumerator();
			while (enumerator.MoveNext())
			{
				Pallet current = enumerator.Current;
				if (((Scannable)current)._barcode._id != palletBarcode)
				{
					continue;
				}
				var enumerator2 = current._crates.GetEnumerator();
				while (enumerator2.MoveNext())
				{
					Crate current2 = enumerator2.Current;
					var enumerator3 = AssetSpawner._instance._poolList.GetEnumerator();
					while (enumerator3.MoveNext())
					{
						Pool current3 = enumerator3.Current;
						if (!(((Scannable)current3._crate)._barcode != ((Scannable)current2)._barcode))
						{
							var enumerator4 = current3._spawned.GetEnumerator();
							while (enumerator4.MoveNext())
							{
								Poolee current4 = enumerator4.Current;
								UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)current4).gameObject);
							}
						}
					}
				}
			}
		}
		catch (Exception)
		{
		}
	}

	public static void UnInstall(string numericalId)
	{
		MelonLogger.Msg("[UnInstall] Called for numericalId=" + numericalId);
		InstalledModInfo installedModInfo = null;
		foreach (InstalledModInfo installedModInfo2 in MainClass.InstalledModInfos)
		{
			if (installedModInfo2.ModInfo.numericalId == numericalId)
			{
				installedModInfo = installedModInfo2;
			}
		}
		Thread thread = new Thread((ThreadStart)delegate
		{
			MelonLogger.Msg("[UnInstall] Background thread started for numericalId=" + numericalId);
			try
			{
				if (installedModInfo != null)
				{
					string barcode = installedModInfo.palletBarcode;
					MelonLogger.Msg("[UnInstall] Found installedModInfo for numericalId=" + numericalId + " barcode=" + barcode);
					MainThreadManager.QueueAction(delegate
					{
						UnloadPallet(barcode);
					});
					MelonLogger.Msg("[UnInstall] Deleting manifest: " + installedModInfo.manifestPath);
					File.Delete(installedModInfo.manifestPath);
					string fullName = Directory.GetParent(installedModInfo.catalogPath).FullName;
					MelonLogger.Msg("[UnInstall] Deleting directory: " + fullName);
					Directory.Delete(fullName, recursive: true);
					MelonLogger.Msg("[UnInstall] Successfully uninstalled numericalId=" + numericalId);
				}
				else
				{
					MelonLogger.Warning("[UnInstall] No InstalledModInfo found for numericalId=" + numericalId + " — nothing to delete");
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("[UnInstall] Exception when uninstalling mod " + numericalId + ": " + ex);
			}
			MainClass.RequestInstallCheck();
		});
		thread.Start();
	}

	public static void GetJson(string mod, Action<string> onCompleted)
	{
		string text = API_PATH + mod + "/files";
		UnityWebRequest httpWebRequest = UnityWebRequest.Get(text);
		httpWebRequest.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
		UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			if (httpWebRequest.result != UnityWebRequest.Result.Success)
			{
				MelonLogger.Error("[ModFileManager] GetJson FAILED: " + text + " — result=" + httpWebRequest.result + " error=" + (httpWebRequest.error ?? "none"));
				return;
			}
			string text2 = httpWebRequest.downloadHandler.text;
			onCompleted?.Invoke(text2);
		});

	}

	public static void GetRawModInfoJson(string mod, Action<dynamic> onCompleted)
	{
		string json = "";
		try
		{
			string text = API_PATH + mod;
			UnityWebRequest httpWebRequest = UnityWebRequest.Get(text);
			httpWebRequest.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
			UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			if (httpWebRequest.result != UnityWebRequest.Result.Success)
			{
				MelonLogger.Error("[ModFileManager] GetRawModInfoJson FAILED: " + text + " — result=" + httpWebRequest.result + " error=" + (httpWebRequest.error ?? "none"));
				// Call callback with null so caller knows it failed
				onCompleted(null);
				return;
			}
			json = httpWebRequest.downloadHandler.text;
			dynamic val2 = JsonConvert.DeserializeObject<object>(json);
			((Action<object>)onCompleted)?.Invoke(val2);
		});

		}
		catch (Exception ex)
		{
			MelonLogger.Error("Error when fetching raw mod info for " + mod + ": ");
			MelonLogger.Error((object)ex);
		}
	}
}
