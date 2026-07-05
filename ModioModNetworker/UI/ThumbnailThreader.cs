using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

		// Try HTTPS first, then HTTP as fallback for TLS issues
		List<string> urlsToTry = new List<string>();
		urlsToTry.Add(url);  // Original HTTPS URL
		if (url.StartsWith("https://"))
		{
			urlsToTry.Add("http://" + url.Substring(8));  // HTTP fallback
		}
		if (url.Contains("thumb.modcdn.io"))
		{
			string altUrl = url.Replace("thumb.modcdn.io", "assets.modcdn.io");
			MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Also trying alt CDN: {altUrl}");
			urlsToTry.Add(altUrl);  // Alt CDN HTTPS
			urlsToTry.Add("http://" + altUrl.Substring(8));  // Alt CDN HTTP
		}

		foreach (string tryUrl in urlsToTry)
		{
			UnityWebRequest webRequest = UnityWebRequest.Get(tryUrl);
			DownloadHandlerTexture handler = new DownloadHandlerTexture(true);
			webRequest.downloadHandler = handler;
			webRequest.SetRequestHeader("User-Agent", "ModioModNetworker/2.8.10");
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
								MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Download failed. Result: {(UnityWebRequest.Result)webRequest.result}, Error: {webRequest.error}, URL: {tryUrl}");
								MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: Response code: {webRequest.responseCode}");
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
}
