using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Linq;
using ModioModNetworker;

namespace ModioModNetworker.UI;

public class ThumbnailThreader
{
	private static ConcurrentQueue<ThumbnailCompletionJob> thumbnailCompletionJobs = new ConcurrentQueue<ThumbnailCompletionJob>();

	public static void HandleQueue()
	{
		if (thumbnailCompletionJobs.Count > 0 && thumbnailCompletionJobs.TryDequeue(out ThumbnailCompletionJob result))
		{
			result.callback();
		}
	}

	public static void DownloadThumbnail(string url, Action<Texture> action)
	{
		if (string.IsNullOrEmpty(url))
		{
			MelonLoader.MelonLogger.Warning("[Diag] DownloadThumbnail: url is null/empty - no thumbnail to download");
			return;
		}

		// Extract mod numerical ID from the CDN URL for API fallback
		// CDN URL format: https://thumb.modcdn.io/mods/{hash}/{numericalId}/crop_640x360/{filename}
		string apiFallbackUrl = null;
		string[] urlParts = url.Split('/');
		for (int i = 0; i < urlParts.Length - 1; i++)
		{
			if (int.TryParse(urlParts[i], out int modId) && modId > 0)
			{
				apiFallbackUrl = "https://g-3809.modapi.io/v1/games/3809/mods/" + modId + "/logo";
				break;
			}
		}

		System.Threading.Tasks.Task.Run(async delegate
		{
			try
			{
				// Attempt CDN download with URL fallback chain using UnityWebRequest (IL2CPP-safe)
				try
				{
					// Extract path for fallback URL construction
					string path = null;
					try
					{
						path = new Uri(url).AbsolutePath;
					}
					catch
					{
						MelonLoader.MelonLogger.Error("[Diag] DownloadThumbnail: Failed to parse CDN URL as URI");
					}

					// Build URL fallback chain: original → HTTP CDN → HTTPS alt CDN → HTTP alt CDN
					List<string> urlsToTry = new List<string> { url };
					if (path != null)
					{
						urlsToTry.Add($"http://thumb.modcdn.io{path}");
						urlsToTry.Add($"https://thumb.modapi.io{path}");
						urlsToTry.Add($"http://thumb.modapi.io{path}");
					}

					foreach (string tryUrl in urlsToTry)
					{
						try
						{
							var uwr = UnityWebRequestTexture.GetTexture(tryUrl);
							uwr.timeout = 15;
							var op = uwr.SendWebRequest();
							while (!op.isDone)
							{
								await Task.Delay(50);
							}

							if (uwr.result == UnityWebRequest.Result.Success)
							{
								Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
								MainThreadManager.QueueAction(() => action(texture));
								return;
							}
							else
							{
								MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: CDN {tryUrl} failed: {uwr.result} - {uwr.error}");
							}
						}
						catch (Exception ex)
						{
							MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: CDN {tryUrl} exception: {ex.Message}");
						}
					}
				}
				catch (Exception ex)
				{
					MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: CDN download failed: {ex.Message}");
				}

				// Fallback: try API endpoint to get logo URL, then download
				if (apiFallbackUrl != null)
				{
					try
					{
						using (var handler = new System.Net.Http.HttpClientHandler
						{
							ServerCertificateCustomValidationCallback = (_, _, _, _) => true
						})
						using (var apiClient = new System.Net.Http.HttpClient(handler)
						{
							Timeout = TimeSpan.FromSeconds(10)
						})
						{
							apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.17");
							apiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ModFileManager.OAUTH_KEY);
							apiClient.DefaultRequestHeaders.Add("X-Modio-Platform", "windows");
							apiClient.DefaultRequestHeaders.Add("X-Modio-Portal", "steam");

							// Request the logo metadata from API (returns JSON with logo URLs)
							var jsonResponse = await apiClient.GetAsync(apiFallbackUrl);
							if (jsonResponse.IsSuccessStatusCode)
							{
								string jsonBody = await jsonResponse.Content.ReadAsStringAsync();
								if (jsonBody.Length > 10 && jsonBody[0] == '{')
								{
									var logoData = Newtonsoft.Json.Linq.JObject.Parse(jsonBody);
									// Try thumb_640x360 first, then original, then any URL found
									string logoUrl = logoData["thumb_640x360"]?.ToString();
									if (string.IsNullOrEmpty(logoUrl))
										logoUrl = logoData["original"]?.ToString();
									if (string.IsNullOrEmpty(logoUrl))
										logoUrl = logoData["url"]?.ToString();

									if (!string.IsNullOrEmpty(logoUrl))
									{
										byte[] imageBytes = await apiClient.GetByteArrayAsync(logoUrl);
										if (imageBytes.Length > 1000)
										{
											CreateTexture(imageBytes, action);
											return;
										}
									}
								}
							}
						}
					}
					catch (Exception ex)
					{
						MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: API fallback failed: {ex.Message}");
					}
				}

				MelonLoader.MelonLogger.Error("[Diag] DownloadThumbnail: All methods failed");
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: All methods failed: {ex.Message}");
			}
		});
	}

	private static void CreateTexture(byte[] imageBytes, Action<Texture> action)
	{
		MainThreadManager.QueueAction(delegate
		{
			try
			{
				Texture2D texture = new Texture2D(2, 2);
				if (UnityEngine.ImageConversion.LoadImage(texture, imageBytes))
				{
					action(texture);
				}
				else
				{
					MelonLoader.MelonLogger.Error("[Diag] DownloadThumbnail: ImageConversion.LoadImage returned false");
				}
			}
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: Texture creation failed: {ex.Message}");
			}
		});
	}
}
