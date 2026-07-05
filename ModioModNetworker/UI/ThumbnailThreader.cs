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
				// Attempt CDN download first — force IPv4 to avoid IPv6 routing issues
				try
				{
					var handler = new System.Net.Http.SocketsHttpHandler
					{
						ConnectCallback = async (context, cancellationToken) =>
						{
							var hostEntry = await System.Net.Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken);
							var ipv4 = hostEntry.AddressList.First(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
							var socket = new System.Net.Sockets.Socket(ipv4.AddressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
							{
								NoDelay = true
							};
							await socket.ConnectAsync(new System.Net.IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);
							return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
						},
						ConnectTimeout = TimeSpan.FromSeconds(15)
					};

					using (var cdnClient = new System.Net.Http.HttpClient(handler)
					{
						Timeout = TimeSpan.FromSeconds(15)
					})
					{
						cdnClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.17");
						byte[] imageBytes = await cdnClient.GetByteArrayAsync(url);
						MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: CDN (IPv4 forced) success: {imageBytes.Length} bytes");
						CreateTexture(imageBytes, action);
						return;
					}
				}
				catch (Exception ex)
				{
					MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: CDN (IPv4 forced) failed: {ex.Message}");
				}

				// Fallback: try API endpoint with Accept header for image content
				if (apiFallbackUrl != null)
				{
					MelonLoader.MelonLogger.Msg("[Diag] DownloadThumbnail: Trying API logo endpoint...");
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

						// Try requesting the image directly with Accept header
						var imageRequest = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, apiFallbackUrl);
						imageRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/png", 0.8));
						imageRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/jpeg", 0.8));
						imageRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/*", 0.5));
						imageRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("*/*", 0.1));

						var response = await apiClient.SendAsync(imageRequest);
						if (response.IsSuccessStatusCode)
						{
							byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
							if (imageBytes.Length > 1000 && imageBytes[0] != 0x7b) // Not JSON (not starting with '{')
							{
								MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: API image request success: {imageBytes.Length} bytes, content-type: {response.Content.Headers.ContentType?.MediaType}");
								CreateTexture(imageBytes, action);
								return;
							}
							else
							{
								// Got JSON instead of image — log it
								string jsonText = System.Text.Encoding.UTF8.GetString(imageBytes);
								MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: API returned JSON, length={imageBytes.Length}");
							}
						}
						else
						{
							MelonLoader.MelonLogger.Msg($"[Diag] DownloadThumbnail: API image request failed: HTTP {(int)response.StatusCode}");
						}
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
