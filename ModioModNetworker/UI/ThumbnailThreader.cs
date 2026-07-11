using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace ModioModNetworker.UI;

public class ThumbnailThreader
{
    private static ConcurrentQueue<ThumbnailCompletionJob> thumbnailCompletionJobs = new ConcurrentQueue<ThumbnailCompletionJob>();
    private static Dictionary<string, byte[]> thumbnailBytesCache = new Dictionary<string, byte[]>();
    private static readonly object thumbnailCacheLock = new object();

    private static HttpClient CreateIPv4HttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = async (context, cancellationToken) =>
            {
                MelonLogger.Msg("[ThumbnailThreader] Resolving IPv4 for " + context.DnsEndPoint.Host);
                var hostEntry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken);
                var ipv4 = hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                
                if (ipv4 == null)
                {
                    MelonLogger.Warning("[ThumbnailThreader] No IPv4 address found for " + context.DnsEndPoint.Host + " — using first available");
                    ipv4 = hostEntry.AddressList.FirstOrDefault();
                }
                
                MelonLogger.Msg("[ThumbnailThreader] Connecting to " + ipv4 + " for " + context.DnsEndPoint.Host);
                var socket = new Socket(ipv4.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(new IPEndPoint(ipv4, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            },
            ConnectTimeout = TimeSpan.FromSeconds(15)
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
    }

    private static readonly HttpClient httpClientIPv4 = CreateIPv4HttpClient();

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
            MelonLogger.Warning("[ThumbnailThreader] URL is null or empty — skipping");
            return;
        }

        // Check texture cache first (store bytes, recreate Texture on hit to avoid white textures)
        lock (thumbnailCacheLock)
        {
            if (thumbnailBytesCache.TryGetValue(url, out byte[] cachedBytes))
            {
                MelonLogger.Msg("[ThumbnailThreader] Recreating texture from cached bytes for " + url + " (" + cachedBytes.Length + " bytes)");
                MainThreadManager.QueueAction(delegate
                {
                    try
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (ImageConversion.LoadImage(texture, cachedBytes))
                        {
                            MelonLogger.Msg("[ThumbnailThreader] Cached texture recreated: " + texture.width + "x" + texture.height);
                            action(texture);
                        }
                        else
                        {
                            MelonLogger.Error("[ThumbnailThreader] Failed to recreate texture from cached bytes for " + url);
                            // Remove bad cache and re-download
                            thumbnailBytesCache.Remove(url);
                            MelonLogger.Msg("[ThumbnailThreader] Removed bad cache entry, re-downloading: " + url);
                            DownloadThumbnail(url, action);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        MelonLogger.Error("[ThumbnailThreader] Cache texture recreation failed for " + url + ": " + cacheEx.Message);
                    }
                });
                return;
            }
        }

        MelonLogger.Msg("[ThumbnailThreader] Starting download for " + url);
        
        Task.Run(async () =>
        {
            byte[] imageBytes = null;
            string usedMethod = "IPv4 HttpClient";
            
            try
            {
                // PRIMARY: IPv4-forced HttpClient
                httpClientIPv4.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.12");
                imageBytes = await httpClientIPv4.GetByteArrayAsync(url);
                MelonLogger.Msg("[ThumbnailThreader] Downloaded " + imageBytes.Length + " bytes via " + usedMethod + " from " + url);
            }
            catch (Exception ipv4Ex)
            {
                MelonLogger.Warning("[ThumbnailThreader] IPv4 HttpClient failed for " + url + ": " + ipv4Ex.Message);
                
                // FALLBACK: Default HttpClient (no IPv4 forcing)
                try
                {
                    usedMethod = "Default HttpClient";
                    using (var fallbackClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) })
                    {
                        fallbackClient.DefaultRequestHeaders.UserAgent.ParseAdd("ModioModNetworker/2.8.12");
                        imageBytes = await fallbackClient.GetByteArrayAsync(url);
                        MelonLogger.Msg("[ThumbnailThreader] Downloaded " + imageBytes.Length + " bytes via " + usedMethod + " from " + url);
                    }
                }
                catch (Exception fallbackEx)
                {
                    MelonLogger.Error("[ThumbnailThreader] ALL download methods failed for " + url + ": " + fallbackEx.Message);
                    return;
                }
            }
            
            if (imageBytes == null || imageBytes.Length == 0)
            {
                MelonLogger.Error("[ThumbnailThreader] Downloaded 0 bytes from " + url);
                return;
            }
            
            // Verify it looks like an image (not a JSON error)
            if (imageBytes.Length > 100 && imageBytes[0] == 0x7b) // '{'
            {
                string preview = System.Text.Encoding.UTF8.GetString(imageBytes, 0, Math.Min(imageBytes.Length, 200));
                MelonLogger.Error("[ThumbnailThreader] Response from " + url + " is JSON, not an image: " + preview);
                return;
            }
            
            // Create texture on main thread
            byte[] capturedBytes = imageBytes;
            string capturedMethod = usedMethod;
            MainThreadManager.QueueAction(delegate
            {
                try
                {
                    Texture2D texture = new Texture2D(2, 2);
                    if (ImageConversion.LoadImage(texture, capturedBytes))
                    {
                        MelonLogger.Msg("[ThumbnailThreader] Success — created " + texture.width + "x" + texture.height + " texture via " + capturedMethod + " from " + capturedBytes.Length + " bytes");
                        // Cache raw bytes for future re-use (not Texture, which becomes white after UI destroy)
                        lock (thumbnailCacheLock)
                        {
                            thumbnailBytesCache[url] = capturedBytes;
                            MelonLogger.Msg("[ThumbnailThreader] Cached " + capturedBytes.Length + " bytes for " + url + " (cache size: " + thumbnailBytesCache.Count + ")");
                        }
                        action(texture);
                    }
                    else
                    {
                        MelonLogger.Error("[ThumbnailThreader] ImageConversion.LoadImage returned false for " + url);
                    }
                }
                catch (Exception texEx)
                {
                    MelonLogger.Error("[ThumbnailThreader] Texture creation failed for " + url + ": " + texEx.Message);
                }
            });
        });
    }
}
