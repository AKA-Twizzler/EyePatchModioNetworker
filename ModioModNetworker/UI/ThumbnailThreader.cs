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
					if ((int)webRequest.result == 1)
					{
						DownloadHandlerTexture val2 = ((Il2CppObjectBase)webRequest.downloadHandler).Cast<DownloadHandlerTexture>();
						Texture texture = (Texture)(object)val2.texture;
						action(texture);
					}
				}
			};
			thumbnailCompletionJobs.Enqueue(item);
		});

	}
}
