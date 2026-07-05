using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
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
			MelonLoader.MelonLogger.Msg("[Diag] DownloadThumbnail: Called with null or empty URL.");
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

		MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: CDN URL: {url}");
		if (apiFallbackUrl != null)
			MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: API fallback URL: {apiFallbackUrl}");

		System.Threading.Tasks.Task.Run(async delegate
		{
			try
			{
				// Attempt CDN download first with 10s timeout
				using (var cdnClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) })
				{
					cdnClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.16");
					try
					{
						byte[] imageBytes = await cdnClient.GetByteArrayAsync(url);
						MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: CDN success: {imageBytes.Length} bytes");
						CreateTexture(imageBytes, action);
						return;
					}
					catch (Exception ex)
					{
						MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: CDN failed: {ex.Message}");
					}
				}

				// Fallback: try API endpoint with OAuth
				if (apiFallbackUrl != null)
				{
					MelonLoader.MelonLogger.Msg("[Diag] DownloadThumbnail: Trying API fallback...");
					using (var handler = new System.Net.Http.HttpClientHandler
					{
						ServerCertificateCustomValidationCallback = (_, _, _, _) => true
					})
					using (var apiClient = new System.Net.Http.HttpClient(handler)
					{
						Timeout = TimeSpan.FromSeconds(30)
					})
					{
						apiClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.16");
						apiClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ModFileManager.OAUTH_KEY);
						apiClient.DefaultRequestHeaders.Add("X-Modio-Platform", "windows");
						apiClient.DefaultRequestHeaders.Add("X-Modio-Portal", "steam");

						byte[] imageBytes = await apiClient.GetByteArrayAsync(apiFallbackUrl);
						MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: API fallback success: {imageBytes.Length} bytes");
						CreateTexture(imageBytes, action);
					}
				}
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
					MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Texture created: {texture.width}x{texture.height}");
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
