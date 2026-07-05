using System;
using System.Collections.Concurrent;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;

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
			MelonLoader.MelonLogger.Warning("Thumbnail URL is null or empty");
			MelonLoader.MelonLogger.Msg("[Diag] DownloadThumbnail: Called with null or empty URL.");
			return;
		}
		MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Starting download from URL: {url}");
		UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
		webRequest.SetRequestHeader("Authorization", "Bearer " + ModFileManager.OAUTH_KEY);
		webRequest.SetRequestHeader("X-Modio-Platform", "windows");
		webRequest.SetRequestHeader("X-Modio-Portal", "steam");
		UnityWebRequestAsyncOperation val = webRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			ThumbnailCompletionJob item = new ThumbnailCompletionJob
			{
				callback = delegate
				{
					try
					{
						if ((int)webRequest.result == 1)
						{
							DownloadHandlerTexture val2 = ((Il2CppObjectBase)webRequest.downloadHandler).Cast<DownloadHandlerTexture>();
							Texture texture = (Texture)(object)val2.texture;
							MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Download succeeded. Texture: {texture.name}, Size: {texture.width}x{texture.height}");
							action(texture);
						}
						else
						{
							MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Download failed. Result: {(UnityWebRequest.Result)webRequest.result}, URL: {url}");
						}
					}
					catch (Exception ex)
					{
						MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Exception during download processing: {ex.Message}");
					}
				}
			};
			thumbnailCompletionJobs.Enqueue(item);
		});

	}
}
