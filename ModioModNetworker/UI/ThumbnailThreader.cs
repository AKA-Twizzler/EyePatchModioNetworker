using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;

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

		MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Starting download via HttpClient: {url}");

		System.Threading.Tasks.Task.Run(async delegate
		{
			try
			{
				using (var handler = new System.Net.Http.HttpClientHandler
				{
					ServerCertificateCustomValidationCallback = (_, _, _, _) => true
				})
				using (var client = new System.Net.Http.HttpClient(handler)
				{
					Timeout = TimeSpan.FromSeconds(15)
				})
				{
					client.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.15");
					byte[] imageBytes = await client.GetByteArrayAsync(url);

					MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Downloaded {imageBytes.Length} bytes via HttpClient");

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
			catch (Exception ex)
			{
				MelonLoader.MelonLogger.Error($"[Diag] DownloadThumbnail: HttpClient download failed: {ex.Message}");
			}
		});
	}
}
