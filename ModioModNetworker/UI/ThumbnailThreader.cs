using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
// using UnityEngine.Networking; // removed - replaced with HttpClient for background thread safety
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
				// CDN download with pure .NET HttpClient (thread-safe on background threads)
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

				// Build URL fallback chain: original → HTTP original → alt CDNs → API fallback
				List<string> urlsToTry = new List<string>();

				// 1. Original CDN URL (with 45s timeout)
				urlsToTry.Add(url);

				// 2. HTTP version of original URL (https → http)
				if (url.StartsWith("https://"))
				{
					urlsToTry.Add("http://" + url.Substring(8));
				}

				// 3. Alternative CDN hostnames using same path
				if (path != null)
				{
					urlsToTry.Add($"https://thumb.modcdn.io{path}");
					urlsToTry.Add($"http://thumb.modcdn.io{path}");
					urlsToTry.Add($"https://thumb.modapi.io{path}");
					urlsToTry.Add($"http://thumb.modapi.io{path}");
				}

					using (var cdnClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(45) })
					{
						cdnClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.49");

						bool cdnSucceeded = false;
						foreach (string tryUrl in urlsToTry)
						{
							try
							{
								byte[] imageBytes = await cdnClient.GetByteArrayAsync(tryUrl);
								if (imageBytes != null && imageBytes.Length > 100)
								{
									cdnSucceeded = AttemptCreateTexture(imageBytes, action);
									if (cdnSucceeded)
										return;
								}
							}
							catch (Exception ex)
							{
								MelonLoader.MelonLogger.Error($"[Diag] CDN {tryUrl} failed: {ex.Message}");
							}
						}
					}
				}
				catch (Exception ex)
				{
					MelonLoader.MelonLogger.Error($"[Diag] CDN download failed: {ex.Message}");
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
							Timeout = TimeSpan.FromSeconds(30)
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
											AttemptCreateTexture(imageBytes, action);
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

	private static bool AttemptCreateTexture(byte[] imageBytes, Action<Texture> action)
	{
		try
		{
			Texture2D texture = new Texture2D(2, 2);
			if (UnityEngine.ImageConversion.LoadImage(texture, imageBytes))
			{
				action(texture);
				return true;
			}
			else
			{
				MelonLoader.MelonLogger.Error("[Diag] DownloadThumbnail: ImageConversion.LoadImage returned false");
				return false;
			}
		}
		catch (Exception ex)
		{
			MelonLoader.MelonLogger.Error($"[Diag] CreateTexture failed: {ex.Message}");
			return false;
		}
	}
}
