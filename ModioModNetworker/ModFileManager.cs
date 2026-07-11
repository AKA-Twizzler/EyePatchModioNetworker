using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
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

	public static string API_PATH = "https://g-3809.modapi.io/v1/games/3809/mods/";

	public static string MOD_FOLDER_PATH = Application.persistentDataPath + "/Mods";

	public static string downloadingModId = "";

	public static string downloadPath = "";

	public static bool isDownloading = false;

	public static bool queueAvailable = false;

	private static List<DownloadQueueElement> queue = new List<DownloadQueueElement>();

	public static bool fetchingSubscriptions = false;

	public static bool fetchingTrending = false;

	public static DownloadAction activeDownloadAction = null;

	public static DownloadQueueElement activeDownloadQueueElement = null;

	public static UnityWebRequest activeDownloadWebRequest;

	public static string[] targetVersionStrings = new string[2] { "1.1", "1.2" };

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
		MelonLogger.Msg("[DownloadQueue] CheckQueue: queue=" + queue.Count + " isDownloading=" + isDownloading + " warehouseReady=" + (AssetWarehouse.Instance != null));
		// Suppress log spam — no need to log every frame when idle
		if (queue.Count == 0 && !isDownloading && !MainClass.warehouseReloadRequested)
			return;
		if (isDownloading || AssetWarehouse.Instance == null || SceneStreamer._session == null || (int)SceneStreamer._session.Status == 1 || queue.Count <= 0)
		{
			return;
		}
		DownloadQueueElement downloadQueueElement = queue[0];
		if (downloadQueueElement.info.Download())
		{
			queue.RemoveAt(0);
			activeDownloadQueueElement = downloadQueueElement;
			MelonLogger.Msg("[DownloadQueue] CheckQueue: Starting download for " + (downloadQueueElement.info.modName ?? downloadQueueElement.info.modId ?? "unknown"));
			MelonLogger.Msg("Downloading mod " + downloadQueueElement.info.modId);
			if (activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value))
			{
				value.Show();
			}
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

	public static async Task DownloadFileHttpClient(string url, string path)
	{
		ModInfo modInfo = activeDownloadQueueElement?.info;
		int lastProgressReported = 0;
		try
		{
			using HttpClient client = new HttpClient(new HttpClientHandler
			{
				ClientCertificateOptions = ClientCertificateOption.Manual,
				ServerCertificateCustomValidationCallback = (HttpRequestMessage httpRequestMessage, X509Certificate2? cert, X509Chain? cetChain, SslPolicyErrors policyErrors) => true
			});
			client.DefaultRequestHeaders.Add("Authorization", "Bearer " + OAUTH_KEY);
			using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
			{
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
			MelonLogger.Error("[DownloadProgress] " + (modInfo.modName ?? modInfo.modId ?? "unknown") + ": Download FAILED at " + lastProgressReported + "%: " + ex.Message);
			isDownloading = false;
			activeDownloadQueueElement = null;
			activeDownloadWebRequest = null;
			ModlistMenu.activeDownloadModInfo = null;
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
			UnityWebRequest httpWebRequest = UnityWebRequest.Get("https://mod.io/v1/me/subscribed?_offset=" + shown + "&limit=400&game_id=3809");
			httpWebRequest.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
			httpWebRequest.SetRequestHeader("X-Modio-Platform", "windows");
			httpWebRequest.SetRequestHeader("X-Modio-Portal", "steam");
			MelonLogger.Msg("QueueSubscriptions: Requesting URL: " + "https://mod.io/v1/me/subscribed?_offset=" + shown + "&limit=400&game_id=3809");
			UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			if (httpWebRequest.result != UnityWebRequest.Result.Success)
			{
				string errorText = (httpWebRequest.downloadHandler != null && !string.IsNullOrEmpty(httpWebRequest.downloadHandler.text)) ? httpWebRequest.downloadHandler.text : "(no response body)";
				MelonLogger.Error("QueueSubscriptions FAILED: url=" + httpWebRequest.url + " result=" + httpWebRequest.result + " code=" + httpWebRequest.responseCode + " error=" + httpWebRequest.error + " body=" + (errorText.Length > 200 ? errorText.Substring(0, 200) : errorText));
				fetchingSubscriptions = false;
				return;
			}
			string responseText = httpWebRequest.downloadHandler.text;
			MelonLogger.Msg("QueueSubscriptions SUCCESS: url=" + httpWebRequest.url + " code=" + httpWebRequest.responseCode + " body=" + (responseText.Length > 200 ? responseText.Substring(0, 200) : responseText));
			MainClass.subscriptionThreadString = responseText;
			fetchingSubscriptions = false;
		});

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
			UnityWebRequest httpWebRequest = UnityWebRequest.Get($"https://mod.io/v1/games/@bonelab/mods?_limit=100&_offset={offset}&_sort=-popular" + text);
			httpWebRequest.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
			UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			MainClass.trendingThreadString = httpWebRequest.downloadHandler.text;
			fetchingTrending = false;
		});

		}
	}

	public static bool Subscribe(string numericalid)
	{
		string text = API_PATH + numericalid + "/subscribe";
		UnityWebRequest httpWebRequest = UnityWebRequest.Get(text);
		httpWebRequest.method = "POST";
		httpWebRequest.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
		httpWebRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
		UnityWebRequestAsyncOperation val = httpWebRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			if (httpWebRequest.responseCode == 201)
			{
				MainThreadManager.QueueAction(delegate
				{
					if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
					{
						NetworkerMenuController.instance.UpdateModPopupButtons();
					}
				});

			}
		});

		return false;
	}

	public static void UninstallAndUnsubscribe(string modId)
	{
		UnInstall(modId);
		UnSubscribe(modId);
	}

	public static void UnSubscribe(string numericalId)
	{
		string text = API_PATH + numericalId + "/subscribe";
		UnityWebRequest val = UnityWebRequest.Get(text);
		val.method = "DELETE";
		val.SetRequestHeader("Authorization", "Bearer " + OAUTH_KEY);
		val.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
		UnityWebRequestAsyncOperation val2 = val.SendWebRequest();
		((AsyncOperation)val2).m_completeCallback = ((AsyncOperation)val2).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			MainThreadManager.QueueAction(delegate
			{
				if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
				{
					MainClass.subscribedModIoNumericalIds.Remove(numericalId);
					NetworkerMenuController.instance.UpdateModPopupButtons();
				}
			});

		});

	}

	public static void UnInstallMainThread(string numericalId)
	{
		InstalledModInfo installedModInfo = null;
		foreach (InstalledModInfo installedModInfo2 in MainClass.InstalledModInfos)
		{
			if (installedModInfo2.ModInfo.numericalId == numericalId)
			{
				installedModInfo = installedModInfo2;
			}
		}
		try
		{
			if (installedModInfo != null)
			{
				string palletBarcode = installedModInfo.palletBarcode;
				UnloadPallet(palletBarcode);
				File.Delete(installedModInfo.manifestPath);
				string fullName = Directory.GetParent(installedModInfo.catalogPath).FullName;
				Directory.Delete(fullName, recursive: true);
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Exception when uninstalling mod: " + ex);
		}
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
			try
			{
				if (installedModInfo != null)
				{
					string barcode = installedModInfo.palletBarcode;
					MainThreadManager.QueueAction(delegate
					{
						UnloadPallet(barcode);
					});
					File.Delete(installedModInfo.manifestPath);
					string fullName = Directory.GetParent(installedModInfo.catalogPath).FullName;
					Directory.Delete(fullName, recursive: true);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("Exception when uninstalling mod: " + ex);
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
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Invalid comparison between Unknown and I4
			if ((int)httpWebRequest.result == 2 || (int)httpWebRequest.result == 3)
			{
				Debug.LogError(httpWebRequest.error);
				onCompleted?.Invoke(null);
			}
			else
			{
				string text2 = httpWebRequest.downloadHandler.text;
				onCompleted?.Invoke(text2);
			}
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
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Invalid comparison between Unknown and I4
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Invalid comparison between Unknown and I4
			json = httpWebRequest.downloadHandler.text;
			dynamic val2 = JsonConvert.DeserializeObject<object>(json);
			if ((int)httpWebRequest.result == 2 || (int)httpWebRequest.result == 3)
			{
				Debug.LogError(httpWebRequest.error);
				((Action<object>)onCompleted)?.Invoke(val2);
			}
			else
			{
				((Action<object>)onCompleted)?.Invoke(val2);
				}
			});

		}
		catch (Exception ex)
		{
			MelonLogger.Error("Error when fetching raw mod info for " + mod + ": ");
			MelonLogger.Error((object)ex);
		}
	}
}
