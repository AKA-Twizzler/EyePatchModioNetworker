using System;
using System.Collections.Concurrent;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;
using MelonLogger = MelonLoader.MelonLogger;

namespace ModioModNetworker.UI;

public class ThumbnailThreader
{
	private static ConcurrentQueue<ThumbnailCompletionJob> thumbnailCompletionJobs = new ConcurrentQueue<ThumbnailCompletionJob>();

	public static void HandleQueue()
	{
		if (thumbnailCompletionJobs.Count > 0)
		{
			MelonLogger.Msg("[ThumbnailThreader] HandleQueue — " + thumbnailCompletionJobs.Count + " jobs pending");
		}
		if (thumbnailCompletionJobs.Count > 0 && thumbnailCompletionJobs.TryDequeue(out ThumbnailCompletionJob result))
		{
			MelonLogger.Msg("[ThumbnailThreader] HandleQueue — dequeued job, invoking callback");
			result.callback();
		}
	}

	public static void DownloadThumbnail(string url, Action<Texture> action)
	{
		MelonLogger.Msg("[ThumbnailThreader] DownloadThumbnail called — url length: " + (url != null ? url.Length.ToString() : "NULL"));
		if (string.IsNullOrEmpty(url))
		{
			MelonLogger.Warning("[ThumbnailThreader] URL is null or empty — call will likely fail");
		}
		UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
		UnityWebRequestAsyncOperation val = webRequest.SendWebRequest();
		((AsyncOperation)val).m_completeCallback = ((AsyncOperation)val).m_completeCallback + new Action<AsyncOperation>(delegate
		{
			ThumbnailCompletionJob item = new ThumbnailCompletionJob
			{
				callback = delegate
				{
					//IL_0007: Unknown result type (might be due to invalid IL or missing references)
					//IL_000d: Invalid comparison between Unknown and I4
					MelonLogger.Msg("[ThumbnailThreader] Completion callback fired — url=" + url + " result=" + (int)webRequest.result);
					if ((int)webRequest.result == 1)
					{
						DownloadHandlerTexture val2 = ((Il2CppObjectBase)webRequest.downloadHandler).Cast<DownloadHandlerTexture>();
						Texture texture = (Texture)(object)val2.texture;
						MelonLogger.Msg("[ThumbnailThreader] Thumbnail downloaded successfully — texture size: " + (texture ? (texture.width + "x" + texture.height) : "null"));
						action(texture);
					}
					else
					{
						MelonLogger.Error("[ThumbnailThreader] Thumbnail download FAILED — url=" + url + " result=" + (int)webRequest.result + " error=" + (webRequest.error ?? "none"));
					}
				}
			};
			thumbnailCompletionJobs.Enqueue(item);
		});

	}
}
