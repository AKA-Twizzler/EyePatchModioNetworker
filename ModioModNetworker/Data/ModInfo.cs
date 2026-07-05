using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using BoneLib;
using Il2CppSLZ.Marrow.Forklift.Model;
using LabFusion.Player;
using MelonLoader;
using ModioModNetworker.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ModioModNetworker.Data;

public class ModInfo
{
	public bool isValidMod;

	public bool downloading;

	public bool mature;

	public bool isTracked = true;

	public string modId;

	public float fileSizeKB;

	public string thumbnailLink;

	public string modName;

	public string modSummary;

	public string fileName;

	public string windowsDownloadLink = "nothing";

	public string androidDownloadLink = "nothing";

	public string directDownloadLink = "nothing";

	public double modDownloadPercentage;

	public string numericalId;

	public string version = "0.0.0";

	public List<string> tags = new List<string>();

	public string author = "Networker";

	public int structureVersion = 0;

	public bool temp = false;

	private static Action onFinished;

	public static int globalStructureVersion = 4;

	public static float requestSize = 0f;

	public static ConcurrentQueue<ModInfoThreadRequest> modInfoThreadRequests = new ConcurrentQueue<ModInfoThreadRequest>();

	public bool Download()
	{
		if (isValidMod && !downloading && !ModFileManager.isDownloading)
		{
			ModFileManager.isDownloading = true;
			ModlistMenu.activeDownloadModInfo = this;
			ModFileManager.downloadingModId = modId;
			if (!HelperMethods.IsAndroid())
			{
				ModFileManager.DownloadFile(windowsDownloadLink, MelonLoader.Utils.MelonEnvironment.GameRootDirectory + "\\temp.zip");
			}
			else
			{
				ModFileManager.DownloadFile(androidDownloadLink, Path.Combine(Application.persistentDataPath, "temp.zip"));
			}
			return true;
		}
		return false;
	}

	public bool IsTracked()
	{
		return isTracked;
	}

	public bool IsBlacklisted()
	{
		if (MainClass.blacklistedModIoIds.Contains(modId) || MainClass.blacklistedModIoIds.Contains(numericalId))
		{
			return true;
		}
		return false;
	}

	public static void HandleQueue()
	{
		if (modInfoThreadRequests.Count > 0 && modInfoThreadRequests.TryDequeue(out ModInfoThreadRequest result))
		{
			requestSize -= 1f;
			ModInfo.Make(result.modId, result.json, result.destination, result.originalModInfo, result.mature);
			if (requestSize == 0f)
			{
				onFinished?.Invoke();
				onFinished = null;
			}
		}
	}

	public bool IsSubscribed()
	{
		return MainClass.subscribedModIoNumericalIds.Contains(numericalId);
	}

	public bool IsInstalled()
	{
		bool result = false;
		foreach (ModInfo installedMod in MainClass.installedMods)
		{
			if (installedMod.numericalId == numericalId)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public ModListing ToModListing()
	{
		ModListing val = new ModListing();
		try
		{
			val.Author = "ModIoModNetworker";
			val.Title = modId;
			val.Description = modSummary;
			val.ThumbnailUrl = thumbnailLink;
			val.Version = version;
			val.Targets = new StringModTargetListingDictionary();
			ModIOModTarget val2 = new ModIOModTarget();
			((ModTarget)val2).ThumbnailOverride = null;
			val2.GameId = 3809L;
			val2.ModId = long.Parse(numericalId);
			try
			{
				if (windowsDownloadLink != null)
				{
					val2.ModfileId = long.Parse(windowsDownloadLink.Split("/files/")[1].Replace("/download", ""));
				}
			}
			catch (Exception)
			{
			}
			((Dictionary<string, ModTarget>)(object)val.Targets).Add("pc", (ModTarget)(object)val2);
			ModIOModTarget val3 = new ModIOModTarget();
			((ModTarget)val3).ThumbnailOverride = null;
			val3.GameId = 3809L;
			val3.ModId = long.Parse(numericalId);
			try
			{
				if (androidDownloadLink != null)
				{
					val3.ModfileId = long.Parse(androidDownloadLink.Split("/files/")[1].Replace("/download", ""));
				}
			}
			catch (Exception)
			{
			}
			((Dictionary<string, ModTarget>)(object)val.Targets).Add("android", (ModTarget)(object)val3);
			string text = ToInfoString();
			((Dictionary<string, ModTarget>)(object)val.Targets).Add(text, (ModTarget)(object)val2);
		}
		catch (Exception ex3)
		{
			MelonLogger.Error((object)ex3);
		}
		return val;
	}

	public string ToInfoString()
	{
		string text = $"networker;{mature};{temp};{fileSizeKB};{fileName};{structureVersion};{ToSafeString(modName).Replace(";", "")};{tags.Count}";
		foreach (string tag in tags)
		{
			text = text + ";" + tag;
		}
		return text;
	}

	public string ToSafeString(string initial)
	{
		return initial.Replace("&amp;", "&");
	}

	public void PopulateFromInfoString(string targetString)
	{
		string[] array = ToSafeString(targetString).Split(";");
		mature = bool.Parse(array[1]);
		temp = bool.Parse(array[2]);
		fileSizeKB = float.Parse(array[3]);
		fileName = array[4];
		structureVersion = int.Parse(array[5]);
		modName = array[6];
		int num = int.Parse(array[7]);
		int num2 = 8;
		for (int i = 0; i < num; i++)
		{
			tags.Add(array[i + num2]);
		}
		isValidMod = true;
	}

	public static void SetFinishedAction(Action action)
	{
		onFinished = action;
	}

	public static void RequestModInfo(string modId, string destination)
	{
		ModFileManager.GetJson("@" + modId, delegate(string json)
		{
			modInfoThreadRequests.Enqueue(new ModInfoThreadRequest
			{
				modId = modId,
				json = json,
				destination = destination
			});
		});
	}

	public static void RequestModInfoNumerical(string modIdNumerical, string destination)
	{
		if (modIdNumerical == null)
		{
			MelonLogger.Msg("Mod ID Numerical was null, skipping");
			return;
		}
		ModFileManager.GetRawModInfoJson(modIdNumerical, delegate(dynamic totalModInfo)
		{
			if ((object)totalModInfo != null)
			{
				string modId = (string)totalModInfo["name_id"];
				bool mature = (int)totalModInfo["maturity_option"] > 0;
				ModFileManager.GetJson("@" + modId, delegate(string json)
				{
					modInfoThreadRequests.Enqueue(new ModInfoThreadRequest
					{
						modId = modId,
						json = json,
						destination = destination,
						mature = mature,
						originalModInfo = totalModInfo
					});
				});
			}
		});
	}

	public static ModInfo MakeFromDynamic(dynamic mod, string modId)
	{
		ModInfo modInfo = new ModInfo();
		modInfo.structureVersion = globalStructureVersion;
		modInfo.modId = modId;
		modInfo.isValidMod = true;
		modInfo.downloading = false;
		modInfo.fileSizeKB = (float)mod["filesize"];
		modInfo.fileName = (string)mod["filename"];
		modInfo.windowsDownloadLink = (string)mod["download"]["binary_url"];
		modInfo.version = (string)mod["version"];
		return modInfo;
	}

	public static void Make(string modId, string json, string destination, dynamic originalModInfo, bool mature = false)
	{
		ModInfo modInfo = new ModInfo();
		modInfo.structureVersion = globalStructureVersion;
		modInfo.modId = modId;
		modInfo.mature = mature;
		Action<ModInfo> action = delegate
		{
		};
		switch (destination)
		{
		case "menuinfos":
			action = delegate(ModInfo info)
			{
				ModlistMenu._modInfos.Add(info);
			};
			break;
		case "spotlight":
			action = delegate(ModInfo info)
			{
				NetworkerMenuController.spotlightOverride.downloadedInfo = info;
				if ((UnityEngine.Object)(object)NetworkerMenuController.spotlightOverride.cachedThumbnail != null)
				{
					UnityEngine.Object.Destroy((UnityEngine.Object)(object)NetworkerMenuController.spotlightOverride.cachedThumbnail);
				}
				NetworkerMenuController.spotlightOverride.cachedThumbnail = null;
			};
			break;
		case "install_level":
			action = delegate(ModInfo info)
			{
				if (!MainClass.modNumericalsDownloadedDuringLobbySession.Contains(info.numericalId) && !info.IsSubscribed())
				{
					if (MainClass.tempLobbyMods)
					{
						info.temp = true;
					}
					float num2 = modInfo.fileSizeKB / 1000000f;
					float num3 = num2 / 1000f;
					if (num3 < MainClass.levelMaxGb && ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = info,
						notify = true
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(info.numericalId);
					}
				}
			};
			break;
		case "install_spawnable":
			action = delegate(ModInfo info)
			{
				if (!info.IsSubscribed() && !MainClass.modNumericalsDownloadedDuringLobbySession.Contains(info.numericalId))
				{
					if (MainClass.tempLobbyMods)
					{
						info.temp = true;
					}
					float num2 = modInfo.fileSizeKB / 1000000f;
					if (num2 < MainClass.maxAutoDownloadMb && ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = info,
						notify = true
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(info.numericalId);
					}
				}
			};
			break;
		case "install_native":
			action = delegate(ModInfo info)
			{
				ModFileManager.AddToQueue(new DownloadQueueElement
				{
					associatedPlayer = null,
					info = info,
					notify = true
				});
			};
			break;
		default:
		{
			if (!destination.StartsWith("install_avatar"))
			{
				break;
			}
			string s = destination.Split(';')[1];
			byte b = byte.Parse(s);
			PlayerID playerId = PlayerIDManager.GetPlayerID(b);
			if (playerId == null)
			{
				break;
			}
			action = delegate(ModInfo info)
			{
				if (!info.IsSubscribed() && !MainClass.modNumericalsDownloadedDuringLobbySession.Contains(info.numericalId))
				{
					if (MainClass.tempLobbyMods)
					{
						info.temp = true;
					}
					float num2 = modInfo.fileSizeKB / 1000000f;
					if (num2 < MainClass.maxAutoDownloadMb && ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = playerId,
						info = info,
						notify = false
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(info.numericalId);
					}
				}
			};
			break;
		}
		}
		try
		{
			dynamic val = JsonConvert.DeserializeObject<object>(json);
			JArray val2 = (JArray)val["data"];
			int count = ((JContainer)val2).Count;
			if (val2 == null)
			{
				modInfo.isValidMod = false;
				action(modInfo);
				return;
			}
			modInfo.isValidMod = true;
			modInfo.downloading = false;
			dynamic val3 = null;
			dynamic val4 = null;
			string text = "0.0.0";
			string text2 = "0.0.0";
			int num = count - 1;
			while (num >= 0 && !((val3 != null && val4 != null) ? true : false))
			{
				JToken val5 = val2[num];
				dynamic val6 = val5[(object)"platforms"];
				bool flag = false;
				bool flag2 = false;
				foreach (dynamic item in val6)
				{
					bool flag3 = HelperMethods.IsAndroid();
					string text3 = (flag3 ? "android" : "windows");
					string text4 = (flag3 ? "windows" : "android");
					if ((string)item["platform"] == text3)
					{
						flag = true;
					}
					else if ((string)item["platform"] == text4)
					{
						flag = false;
						break;
					}
				}
				foreach (dynamic item2 in val6)
				{
					bool flag4 = HelperMethods.IsAndroid();
					string text5 = (flag4 ? "android" : "windows");
					string text6 = (flag4 ? "windows" : "android");
					if ((string)item2["platform"] == text6)
					{
						flag2 = true;
					}
					else if ((string)item2["platform"] == text5)
					{
						flag2 = false;
						break;
					}
				}
				if (flag || flag2)
				{
					if (flag)
					{
						string text7 = ((string)val5[(object)"version"]) ?? "";
						if (val3 == null)
						{
							val3 = val5;
							text = text7;
						}
					}
					if (flag2)
					{
						string text8 = ((string)val5[(object)"version"]) ?? "";
						if (val4 == null)
						{
							val4 = val5;
							text2 = text8;
						}
					}
				}
				num--;
			}
			if (val3 != null)
			{
				if (originalModInfo != null)
				{
					string text9 = (string)originalModInfo["profile_url"];
					string text10 = ((string)originalModInfo["id"]) ?? "";
					string text11 = (string)originalModInfo["name"];
					string text12 = (string)originalModInfo["summary"];
					string text13 = (string)originalModInfo["logo"]["thumb_640x360"];
					modInfo.modName = text11;
					modInfo.thumbnailLink = text13;
					modInfo.modSummary = text12;
					modInfo.numericalId = text10;
					modInfo.author = (string)originalModInfo["submitted_by"]["username"];
					foreach (dynamic item3 in originalModInfo["tags"])
					{
						modInfo.tags.Add((string)item3["name"]);
					}
				}
				modInfo.fileSizeKB = (float)val3["filesize"];
				if (!HelperMethods.IsAndroid())
				{
					modInfo.windowsDownloadLink = (string)val3["download"]["binary_url"];
					if (val4 != null)
					{
						modInfo.androidDownloadLink = (string)val4["download"]["binary_url"];
					}
				}
				else
				{
					modInfo.androidDownloadLink = (string)val3["download"]["binary_url"];
					if (val4 != null)
					{
						modInfo.windowsDownloadLink = (string)val4["download"]["binary_url"];
					}
				}
				modInfo.fileName = (string)val3["filename"];
				modInfo.version = ((string)val3["version"]) ?? "";
				if (HelperMethods.IsAndroid() && ((val4 != null) ? true : false))
				{
					modInfo.version = ((string)val4["version"]) ?? "";
				}
				if (modInfo.version == null)
				{
					modInfo.version = "0.0.0";
				}
			}
			else
			{
				modInfo.isValidMod = false;
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error((object)ex);
			modInfo.isValidMod = false;
			action(modInfo);
			return;
		}
		action(modInfo);
	}
}
