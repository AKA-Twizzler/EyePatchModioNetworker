using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BoneLib;
using BoneLib.BoneMenu;
using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.SceneStreaming;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Downloading.ModIO;
using LabFusion.Entities;
using LabFusion.Marrow;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.SDK.Modules;
using LabFusion.UI.Popups;
using LabFusion.Utilities;
using MelonLoader;
using MelonLoader.Preferences;
using ModioModNetworker.UI;
using ModioModNetworker.Data;
using ModioModNetworker.Queue;
using ModioModNetworker.UI;
using ModioModNetworker.Utilities;
using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace ModioModNetworker;

public class MainClass : MelonMod
{
	private static string MODIO_MODNETWORKER_DIRECTORY = MelonLoader.Utils.MelonEnvironment.GameRootDirectory + "/ModIoModNetworker";

	private static string MODIO_AUTH_TXT_DIRECTORY = MODIO_MODNETWORKER_DIRECTORY + "/auth.txt";

	private static string MODIO_BLACKLIST_TXT_DIRECTORY = MODIO_MODNETWORKER_DIRECTORY + "/blacklist.txt";

	public static List<string> subscribedModIoNumericalIds = new List<string>();

	public static List<string> blacklistedModIoIds = new List<string>();

	private static List<string> toRemoveSubscribedModIoIds = new List<string>();

	public static List<ModInfo> subscribedMods = new List<ModInfo>();

	public static List<ModInfo> installedMods = new List<ModInfo>();

	public static List<InstalledModInfo> InstalledModInfos = new List<InstalledModInfo>();

	private static List<InstalledModInfo> outOfDateModInfos = new List<InstalledModInfo>();

	public static bool warehouseReloadRequested = false;

	public static List<string> warehousePalletReloadTargets = new List<string>();

	public static List<string> warehouseReloadFolders = new List<string>();

	public static bool subsChanged = false;

	public static bool refreshInstalledModsRequested = false;

	public static bool refreshSubscribedModsRequested = false;

	public static bool menuRefreshRequested = false;

	public static string subscriptionThreadString = "";

	public static string trendingThreadString = "";

	public static bool subsRefreshing = false;

	private static int desiredSubs = 0;

	private bool addedCallback = false;

	public static MelonPreferences_Category melonPreferencesCategory;

	private static MelonPreferences_Entry<string> modsDirectory;

	public static MelonPreferences_Entry<bool> autoDownloadAvatarsConfig;

	public static MelonPreferences_Entry<bool> autoDownloadSpawnablesConfig;

	public static MelonPreferences_Entry<bool> autoDownloadLevelsConfig;

	public static MelonPreferences_Entry<bool> downloadMatureContentConfig;

	public static MelonPreferences_Entry<bool> tempLobbyModsConfig;

	public static MelonPreferences_Entry<bool> overrideFusionDLConfig;

	public static MelonPreferences_Entry<float> maxAutoDownloadMbConfig;

	public static MelonPreferences_Entry<float> maxLevelAutoDownloadGbConfig;

	public static float maxAutoDownloadMb = 500f;

	public static bool autoDownloadAvatars = true;

	public static bool autoDownloadSpawnables = true;

	public static bool autoDownloadLevels = false;

	public static float levelMaxGb = 1f;

	public static bool downloadMatureContent = false;

	public static bool tempLobbyMods = false;

	public static bool useRepo = false;

	public static bool overrideFusionDL = false;

	public static List<string> modNumericalsDownloadedDuringLobbySession = new List<string>();

	private static int subsShown = 0;

	private static int subTotal = 0;

	public bool palletLock = false;

	public static bool confirmedHostHasIt = false;

	private static bool loadedInstalled = false;

	public static bool handlingInstalled = false;

	public static bool handlingSubscribed = false;

	private bool assetWarehouseLoaded = false;

	public override void OnInitializeMelon()
	{
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Expected O, but got Unknown
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Expected O, but got Unknown
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Expected O, but got Unknown
		melonPreferencesCategory = MelonPreferences.CreateCategory("ModioModNetworker");
		melonPreferencesCategory.SetFilePath(MelonLoader.Utils.MelonEnvironment.UserDataDirectory + "/ModioModNetworker.cfg");
		modsDirectory = melonPreferencesCategory.CreateEntry<string>("ModDirectoryPath", Application.persistentDataPath + "/Mods", (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		autoDownloadAvatarsConfig = melonPreferencesCategory.CreateEntry<bool>("AutoDownloadAvatars", true, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		autoDownloadSpawnablesConfig = melonPreferencesCategory.CreateEntry<bool>("AutoDownloadSpawnables", true, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		autoDownloadLevelsConfig = melonPreferencesCategory.CreateEntry<bool>("AutoDownloadLevels", true, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		maxLevelAutoDownloadGbConfig = melonPreferencesCategory.CreateEntry<float>("MaxLevelAutoDownloadGb", 1f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		tempLobbyModsConfig = melonPreferencesCategory.CreateEntry<bool>("TemporaryLobbyMods", false, (string)null, "If set to true, lobby mods like (avatars/spawnables/levels) that got auto downloaded will be deleted when you leave the lobby.", false, false, (ValueValidator)null, (string)null);
		maxAutoDownloadMbConfig = melonPreferencesCategory.CreateEntry<float>("MaxAutoDownloadMb", 500f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		downloadMatureContentConfig = melonPreferencesCategory.CreateEntry<bool>("DownloadMatureContent", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		overrideFusionDLConfig = melonPreferencesCategory.CreateEntry<bool>("OverrideFusionDL", true, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		maxAutoDownloadMb = maxAutoDownloadMbConfig.Value;
		autoDownloadAvatars = autoDownloadAvatarsConfig.Value;
		downloadMatureContent = downloadMatureContentConfig.Value;
		autoDownloadSpawnables = autoDownloadSpawnablesConfig.Value;
		autoDownloadLevels = autoDownloadLevelsConfig.Value;
		tempLobbyMods = tempLobbyModsConfig.Value;
		levelMaxGb = maxLevelAutoDownloadGbConfig.Value;
		useRepo = false;
		overrideFusionDL = overrideFusionDLConfig.Value;
		ModFileManager.MOD_FOLDER_PATH = modsDirectory.Value;
		SpotlightOverride.LoadFromRegularURL();
		AssetBundle bundle = (HelperMethods.IsAndroid() ? HelperMethods.LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), "ModioModNetworker.Resources.networkermenu.android.networker") : HelperMethods.LoadEmbeddedAssetBundle(Assembly.GetExecutingAssembly(), "ModioModNetworker.Resources.networkermenu.networker"));
		if (bundle != null)
		{
			NetworkerAssets.LoadAssetsUI(bundle);
		}
		else
		{
			MelonLogger.Error("Failed to load UI asset bundle - modio menu will be unavailable");
		}
		PrepareModFiles();
		string authKey = ReadAuthKey();
		if (!string.IsNullOrEmpty(authKey))
		{
			ModFileManager.OAUTH_KEY = authKey;
			MelonLogger.Msg("Loaded OAUTH key from auth.txt (" + authKey.Length + " chars)");
		}
		else
		{
			MelonLogger.Warning("auth.txt is empty or invalid — will use game/Fusion token if available");
		}
		blacklistedModIoIds = ReadBlacklist();
		MelonLogger.Msg("Loaded blacklist with " + blacklistedModIoIds.Count + " entries.");
		ModIOSettings.LoadToken((Action<string>)OnLoadToken);
		MelonLogger.Msg("Loading internal module...");
		ModuleManager.RegisterModule<ModlistModule>();
		ModFileManager.Initialize();
		ModlistMenu.Initialize();
		// If we already have auth from auth.txt, trigger data population immediately
		// without waiting for Fusion's async LoadToken callback
		if (!string.IsNullOrEmpty(ModFileManager.OAUTH_KEY))
		{
			OnLoadToken(ModFileManager.OAUTH_KEY);
		}
		MultiplayerHooking.OnPlayerJoined += new PlayerUpdate(OnPlayerJoin);
		MultiplayerHooking.OnDisconnected += new ServerEvent(OnDisconnect);
		MultiplayerHooking.OnStartedServer += new ServerEvent(OnStartServer);
		NetworkPlayer.OnNetworkRigCreated += OnPlayerRepCreated;
		AssetWarehouse.OnReady(new System.Action(delegate
		{
			AssetWarehouse instance = AssetWarehouse.Instance;
			instance.OnCrateAdded += new Action<Barcode>(delegate(Barcode s)
			{
				palletLock = false;
				LevelHoldQueue.CheckValid(s._id);
				SpawnableHoldQueue.CheckValid(s._id);
				foreach (NetworkPlayer allNetworkPlayer in NetworkPlayerUtilities.GetAllNetworkPlayers())
				{
					FieldInfo field = ((object)allNetworkPlayer.AvatarSetter).GetType().GetField("_isAvatarDirty", BindingFlags.Instance | BindingFlags.NonPublic);
					field.SetValue(allNetworkPlayer.AvatarSetter, true);
				}
			});

			assetWarehouseLoaded = true;
			DeleteAllTempMods();
		}));
		void OnLoadToken(string loadedToken)
		{
			// Guard: don't re-run if already populated from auth.txt
			if (loadedInstalled)
			{
				return;
			}

			// Remember whether auth came from auth.txt (set before this call) or Fusion
			bool fromAuthTxt = !string.IsNullOrEmpty(ModFileManager.OAUTH_KEY);

			// Only set OAUTH_KEY from Fusion if auth.txt didn't already provide it
			if (!fromAuthTxt)
			{
				ModFileManager.OAUTH_KEY = loadedToken;
			}

			if (!string.IsNullOrEmpty(ModFileManager.OAUTH_KEY))
			{
				MelonLogger.Msg("Populating currently installed mods via this mod. (auth source: " + (fromAuthTxt ? "auth.txt" : "game/Fusion") + ")");
				installedMods.Clear();
				InstalledModInfos.Clear();
				PopulateInstalledMods(ModFileManager.MOD_FOLDER_PATH);
				loadedInstalled = true;
				MelonLogger.Msg("Checking mod.io account subscriptions");
				PopulateSubscriptions();
				ModFileManager.QueueTrending(0);
				MelonLogger.Msg("Registered on mod.io with auth key!");
			}
			else
			{
				MelonLogger.Error("No auth token available from auth.txt or game/Fusion — mod.io features will be unavailable");
			}
		}
	}

	private void OnLobbyCategoryMade(Page category, INetworkLobby lobby)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		string text = default(string);
		if (lobby.TryGetMetadata("modionetworker", out text))
		{
			category.CreateFunction("ModioModNetworker Active On Server", Color.cyan, (Action)delegate
			{
			});
		}
		string text2 = default(string);
		if (!lobby.TryGetMetadata("LevelBarcode", out text2) || CrateFilterer.HasCrate<LevelCrate>(new Barcode(text2)))
		{
			return;
		}
		category.CreateFunction("Download Level", Color.cyan, (Action)delegate
		{
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Expected O, but got Unknown
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Expected O, but got Unknown
			string text3 = default(string);
			if (lobby.TryGetMetadata("networkermap", out text3))
			{
				if (text3 != "null")
				{
					Notifier.Send(new Notification
					{
						Title = new NotificationText("Installing lobby level...", Color.cyan, true),
						ShowPopup = true,
						PopupLength = 1f,
						Message = "Please wait",
						SaveToMenu = false
					});
					ModInfo.RequestModInfoNumerical(text3, "install_native");
				}
				else
				{
					Notifier.Send(new Notification
					{
						Title = new NotificationText("Cannot install this map!", Color.cyan, true),
						ShowPopup = true,
						PopupLength = 1f,
						Message = "Probably not a networked map!",
						SaveToMenu = false
					});
				}
			}
		});
	}

	private void DeleteAllTempMods()
	{
		foreach (ModInfo installedMod in installedMods)
		{
			if (installedMod.temp)
			{
				ModFileManager.UnInstallMainThread(installedMod.numericalId);
			}
		}
	}

	public override void OnUpdate()
	{
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Invalid comparison between Unknown and I4
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_038a: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Expected O, but got Unknown
		foreach (AvatarDownloadBar value3 in AvatarDownloadBar.bars.Values)
		{
			value3.Update();
		}
		ThumbnailThreader.HandleQueue();
		MainThreadManager.HandleQueue();
		LevelHoldQueue.Update();
		if (ModFileManager.activeDownloadQueueElement != null && ModFileManager.activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(ModFileManager.activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value))
		{
			ModInfo activeDownloadModInfo = ModlistMenu.activeDownloadModInfo;
			value.SetModName(activeDownloadModInfo.modId);
			value.SetPercentage((float)activeDownloadModInfo.modDownloadPercentage);
		}
		bool flag = false;
		if (SceneStreamer._session != null && (int)SceneStreamer._session.Status == 1)
		{
			flag = true;
		}
		if (ModFileManager.activeDownloadAction != null && ModFileManager.activeDownloadAction.Check())
		{
			ModFileManager.activeDownloadAction.Handle();
			ModFileManager.activeDownloadAction = null;
		}
		if (subsRefreshing && subscribedModIoNumericalIds.Count >= desiredSubs)
		{
			foreach (string toRemoveSubscribedModIoId in toRemoveSubscribedModIoIds)
			{
				subscribedModIoNumericalIds.Remove(toRemoveSubscribedModIoId);
			}
			toRemoveSubscribedModIoIds.Clear();
			ModlistMenu.Refresh(openMenu: true);
			subsRefreshing = false;
			handlingSubscribed = false;
			Notifier.Send(new Notification
			{
				Title = new NotificationText("Mod.io Subscriptions Refreshed!", Color.cyan, true),
				ShowPopup = true,
				PopupLength = 2f
			});
			MelonLogger.Msg("Finished refreshing mod.io subscriptions!");
			outOfDateModInfos.Clear();
		}
		if (warehouseReloadRequested && assetWarehouseLoaded && AssetWarehouse.Instance._initialLoaded && !palletLock)
		{
			bool flag2 = false;
			if (warehousePalletReloadTargets.Count > 0)
			{
				ModFileManager.DeleteExistingModObjects(warehousePalletReloadTargets[0]);
				PalletManifest val = null;
				var enumerator3 = AssetWarehouse.Instance.palletManifests.GetEnumerator();
				while (enumerator3.MoveNext())
				{
					var current3 = enumerator3.Current;
				if (current3.Key._id == warehousePalletReloadTargets[0])
				{
					val = current3.Value;
						break;
					}
				}
				AssetWarehouse.Instance.LoadAndUpdatePalletManifest(val.Pallet, ModlistMenu.activeDownloadModInfo.ToModListing(), val.PalletPath, val.CatalogPath, (IResourceLocator)null);
				warehousePalletReloadTargets.RemoveAt(0);
				flag2 = true;
			}
			if (warehouseReloadFolders.Count > 0)
			{
				AssetWarehouse.Instance.LoadPalletFromFolderAsync(warehouseReloadFolders[0], true, (string)null, ModlistMenu.activeDownloadModInfo.ToModListing());
				warehouseReloadFolders.RemoveAt(0);
				palletLock = true;
			}
			if (warehouseReloadFolders.Count == 0 && warehousePalletReloadTargets.Count == 0)
			{
				string text = "Downloaded!";
				string text2 = "This mod has been loaded into the game.";
				if (flag2)
				{
					text = "Updated!";
					text2 = "This mod has been updated and reloaded.";
				}
				if (ModFileManager.activeDownloadQueueElement.notify)
				{
					Notifier.Send(new Notification
					{
						Title = new NotificationText(ModlistMenu.activeDownloadModInfo.modId + " " + text, Color.cyan, true),
						ShowPopup = true,
						Message = new NotificationText(text2),
						PopupLength = 3f,
						SaveToMenu = false
					});
				}
				if (ModFileManager.activeDownloadQueueElement != null && ModFileManager.activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(ModFileManager.activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value2))
				{
					value2.Finish();
				}
				palletLock = false;
				warehouseReloadRequested = false;
				ModFileManager.isDownloading = false;
				ModFileManager.activeDownloadWebRequest = null;
				ModlistMenu.activeDownloadModInfo = null;
				ModFileManager.activeDownloadQueueElement = null;
			}
		}
		ModInfo.HandleQueue();
		ModFileManager.CheckQueue();
		TimerManager.Update();
		if (!flag && ModlistMessage.waitAndQueue.Count > 0)
		{
			foreach (ModInfo item in ModlistMessage.waitAndQueue)
			{
				float num = item.fileSizeKB / 1000000f;
				if (num < maxAutoDownloadMb && autoDownloadAvatars)
				{
					if (!downloadMatureContent && item.mature)
					{
						return;
					}
					ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = item,
						notify = false
					});
				}
			}
			ModlistMessage.waitAndQueue.Clear();
		}
		if (subsChanged)
		{
			subsChanged = false;
			if (NetworkInfo.HasServer && NetworkInfo.IsHost)
			{
				SendAllMods();
			}
		}
		if (menuRefreshRequested)
		{
			if (flag)
			{
				return;
			}
			ModlistMenu.Refresh(openMenu: true);
			menuRefreshRequested = false;
		}
		if (refreshSubscribedModsRequested && !handlingInstalled && !handlingSubscribed)
		{
			refreshSubscribedModsRequested = false;
			handlingSubscribed = true;
			subscribedMods.Clear();
			subscribedModIoNumericalIds.Clear();
			subTotal = 0;
			subsShown = 0;
			desiredSubs = 0;
			ModFileManager.QueueSubscriptions(subsShown);
		}
		if (refreshInstalledModsRequested && !handlingSubscribed && !handlingInstalled)
		{
			installedMods.Clear();
			InstalledModInfos.Clear();
			NetworkerMenuController.totalInstalled.Clear();
			ModlistMenu.installPage = 0;
			Thread thread = new Thread((ThreadStart)delegate
			{
				handlingInstalled = true;
				PopulateInstalledMods(ModFileManager.MOD_FOLDER_PATH);
				MainThreadManager.QueueAction(delegate
				{
					if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
					{
						NetworkerMenuController.instance.Refresh();
					}
				});
				handlingInstalled = false;
			});
			thread.Start();
			loadedInstalled = true;
			ModlistMenu.Refresh(openMenu: true);
			refreshInstalledModsRequested = false;
			if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
			{
				NetworkerMenuController.instance.UpdateModPopupButtons();
			}
		}
		if (subscriptionThreadString != "")
		{
			InternalPopulateSubscriptions();
		}
		if (trendingThreadString != "")
		{
			InternalPopulateTrending();
			if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
			{
				NetworkerMenuController.instance.OnNewTrendingRecieved();
			}
		}
	}

	public static void RequestInstallCheck(float delay = 1f)
	{
		TimerManager.DelayAction(delay, delegate
		{
			refreshInstalledModsRequested = true;
		});
	}

	private static void UpdateModInfo(ModInfo subscribed, InstalledModInfo installed)
	{
		try
		{
			ModInfo modInfo = installed.ModInfo;
			modInfo.mature = subscribed.mature;
			modInfo.modSummary = subscribed.modSummary;
			modInfo.modName = subscribed.modName;
			modInfo.thumbnailLink = subscribed.thumbnailLink;
			modInfo.numericalId = subscribed.numericalId;
			modInfo.structureVersion = ModInfo.globalStructureVersion;
			modInfo.windowsDownloadLink = subscribed.windowsDownloadLink;
			modInfo.androidDownloadLink = subscribed.androidDownloadLink;
			if (modInfo.version == null)
			{
				modInfo.version = "0.0.0";
			}
			string path = Path.Combine(Directory.GetParent(installed.palletPath).Name, "modinfo.json");
			File.Delete(path);
			string contents = JsonConvert.SerializeObject((object)modInfo);
			File.WriteAllText(path, contents);
			MelonLogger.Msg($"Updated modinfo.json for {modInfo.modId} to version {modInfo.structureVersion}");
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Skipped updating modinfo.json for " + installed.ModInfo.modId + " because of an error: " + ex);
		}
	}

	public static void ReceiveSubModInfo(ModInfo modInfo, bool ignoreTag = false)
	{
		InstalledModInfo installedModInfo = null;
		if (modInfo.version == null)
		{
			modInfo.version = "0.0.0";
		}
		if (installedModInfo != null)
		{
			outOfDateModInfos.Remove(installedModInfo);
		}
		if (!modInfo.isValidMod)
		{
			toRemoveSubscribedModIoIds.Add(modInfo.numericalId);
		}
		ModFileManager.AddToQueue(new DownloadQueueElement
		{
			associatedPlayer = null,
			info = modInfo
		}, ignoreTag);
		subscribedModIoNumericalIds.Add(modInfo.numericalId);
		subscribedMods.Add(modInfo);
	}

	public static void PopulateSubscriptions()
	{
		refreshSubscribedModsRequested = true;
	}

	private static void InternalPopulateTrending()
	{
		string text = trendingThreadString;
		trendingThreadString = "";
		dynamic val = JsonConvert.DeserializeObject<object>(text);
		foreach (dynamic item in val["data"])
		{
			string text2 = (string)item["profile_url"];
			string numericalId = "" + item["id"];
			string text3 = (string)item["name"];
			string modSummary = (string)item["summary"];
			string thumbnailLink = (string)item["logo"]["thumb_640x360"];
			string text4 = text2.Split('/')[^1];
			bool flag = true;
			int num = 0;
			int num2 = 0;
			try
			{
				foreach (dynamic item2 in item["platforms"])
				{
					if ((string)item2["platform"] == "windows")
					{
						int num3 = (int)item2["modfile_live"];
						num = num3;
						break;
					}
				}
				foreach (dynamic item3 in item["platforms"])
				{
					if ((string)item3["platform"] == "android")
					{
						int num4 = (int)item3["modfile_live"];
						num2 = num4;
						break;
					}
				}
				if (num != 0 && num2 != 0 && num == num2)
				{
					flag = false;
				}
				if ((int)item["status"] == 3)
				{
					flag = false;
				}
				ModInfo modInfo = ModInfo.MakeFromDynamic(item["modfile"], text4);
				modInfo.isValidMod = false;
				modInfo.mature = (int)item["maturity_option"] > 0;
				modInfo.modName = text3;
				modInfo.thumbnailLink = thumbnailLink;
				modInfo.modSummary = modSummary;
				modInfo.numericalId = numericalId;
				foreach (dynamic item4 in item["tags"])
				{
					modInfo.tags.Add((string)item4["name"]);
				}
				if (flag)
				{
					modInfo.androidDownloadLink = $"{ModFileManager.API_PATH}{(string)item["id"]}/files/{num2}/download";
					modInfo.windowsDownloadLink = $"{ModFileManager.API_PATH}{(string)item["id"]}/files/{num}/download";
					modInfo.isValidMod = true;
				}
				if (modInfo.version == null)
				{
					modInfo.version = "0.0.0";
				}
				if (!modInfo.mature || downloadMatureContent)
				{
					NetworkerMenuController.modIoRetrieved.Add(modInfo);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("Failed to parse trending mod " + text3 + ": " + ex);
			}
		}
	}

	private static void InternalPopulateSubscriptions()
	{
		string text = subscriptionThreadString;
		subscriptionThreadString = "";
		dynamic val = JsonConvert.DeserializeObject<object>(text);
		int num = 0;
		int num2 = (int)val["result_total"];
		if (subTotal == 0)
		{
			MelonLogger.Msg("Total subscriptions: " + num2);
			subTotal = num2;
			desiredSubs = 0;
		}
		int num3 = (int)val["result_count"];
		if (num3 == 0)
		{
			MelonLogger.Msg("No subscriptions found!");
			return;
		}
		foreach (dynamic item in val["data"])
		{
			if ((int)item["game_id"] == 3809)
			{
				num++;
			}
		}
		desiredSubs += num;
		ModInfoThreadRequest result;
		while (ModInfo.modInfoThreadRequests.TryDequeue(out result))
		{
		}
		ModInfo.requestSize = num;
		foreach (dynamic item2 in val["data"])
		{
			if ((int)item2["game_id"] != 3809)
			{
				continue;
			}
			string text2 = (string)item2["profile_url"];
			string numericalId = ((string)item2["id"]) ?? "";
			string modName = (string)item2["name"];
			string modSummary = (string)item2["summary"];
			string author = (string)item2["submitted_by"]["username"];
			string thumbnailLink = (string)item2["logo"]["thumb_640x360"];
			string text3 = text2.Split('/')[^1];
			bool flag = true;
			int num4 = 0;
			int num5 = 0;
			foreach (dynamic item3 in item2["platforms"])
			{
				if ((string)item3["platform"] == "windows")
				{
					int num6 = (int)item3["modfile_live"];
					num4 = num6;
					break;
				}
			}
			foreach (dynamic item4 in item2["platforms"])
			{
				if ((string)item4["platform"] == "android")
				{
					int num7 = (int)item4["modfile_live"];
					num5 = num7;
					break;
				}
			}
			if (num4 != 0 && num5 != 0 && num4 == num5)
			{
				flag = false;
			}
			if ((int)item2["status"] == 3)
			{
				flag = false;
			}
			ModInfo modInfo = ModInfo.MakeFromDynamic(item2["modfile"], text3);
			modInfo.isValidMod = false;
			modInfo.mature = (int)item2["maturity_option"] > 0;
			modInfo.modName = modName;
			modInfo.thumbnailLink = thumbnailLink;
			modInfo.modSummary = modSummary;
			modInfo.numericalId = numericalId;
			modInfo.author = author;
			foreach (dynamic item5 in item2["tags"])
			{
				modInfo.tags.Add((string)item5["name"]);
			}
			if (flag)
			{
				modInfo.androidDownloadLink = string.Format("{0}{1}/files/{2}/download", ModFileManager.API_PATH, (object?)item2["id"], num5);
				modInfo.windowsDownloadLink = string.Format("{0}{1}/files/{2}/download", ModFileManager.API_PATH, (object?)item2["id"], num4);
				modInfo.isValidMod = true;
			}
			ReceiveSubModInfo(modInfo);
		}
		subsShown += num3;
		if (subTotal - subsShown > 0)
		{
			ModFileManager.QueueSubscriptions(subsShown);
		}
		if (subsShown >= subTotal)
		{
			subsRefreshing = true;
		}
	}

	public void PopulateInstalledMods(string directory)
	{
		if (string.IsNullOrEmpty(directory))
		{
			MelonLogger.Warning("PopulateInstalledMods: MOD_FOLDER_PATH is empty — no mods directory configured. Set 'ModsFolder' in ModioModNetworker.cfg");
			return;
		}
		if (!Directory.Exists(directory))
		{
			MelonLogger.Warning("PopulateInstalledMods: Directory not found: '" + directory + "' — no installed mods to display");
			return;
		}
		MelonLogger.Msg("PopulateInstalledMods: Scanning " + directory);
		List<DirectoryInfo> list = new List<DirectoryInfo>();
		try
		{
			list = (from f in new DirectoryInfo(directory).GetDirectories()
				orderby f.LastWriteTime descending
				select f).ToList();
		}
		catch (Exception ex)
		{
			MelonLogger.Error("PopulateInstalledMods: Error reading directory listing: " + ex.Message);
		}
		if (1 == 0)
		{
			return;
		}
		string[] files = Directory.GetFiles(directory);
		foreach (string text in files)
		{
			if (!text.EndsWith(".manifest"))
			{
				continue;
			}
			dynamic val = JsonConvert.DeserializeObject<object>(File.ReadAllText(text));
			ModInfo modInfo = new ModInfo();
			try
			{
				string version = (string)val["objects"]["2"]["version"];
				string modId = (string)val["objects"]["2"]["title"];
				string modSummary = (string)val["objects"]["2"]["description"];
				string thumbnailLink = (string)val["objects"]["2"]["thumbnailUrl"];
				int num2 = -1;
				int num3 = -1;
				string text2 = "";
				int value = 0;
				foreach (dynamic item2 in val["objects"]["2"]["targets"])
				{
					string text3 = item2.ToString();
					if (text3.Contains("networker"))
					{
						string[] array = text3.Split("\": {");
						text2 = array[0].Replace("\"", "");
					}
				}
				string windowsDownloadLink = "";
				string androidDownloadLink = "";
				try
				{
					num3 = (int)val["objects"]["2"]["targets"]["android"]["ref"];
				}
				catch (Exception)
				{
				}
				try
				{
					num2 = (int)val["objects"]["2"]["targets"]["pc"]["ref"];
				}
				catch (Exception)
				{
				}
				if (num2 != -1)
				{
					int value2 = (int)val["objects"][num2.ToString()]["modfileId"];
					value = (int)val["objects"][num2.ToString()]["modId"];
					windowsDownloadLink = $"{ModFileManager.API_PATH}{value}/files/{value2}/download";
				}
				if (num3 != -1)
				{
					int value3 = val["objects"][num3.ToString()]["modfileId"];
					value = (int)val["objects"][num3.ToString()]["modId"];
					androidDownloadLink = $"{ModFileManager.API_PATH}{value}/files/{value3}/download";
				}
				modInfo.version = version;
				modInfo.thumbnailLink = thumbnailLink;
				modInfo.modSummary = modSummary;
				modInfo.androidDownloadLink = androidDownloadLink;
				modInfo.windowsDownloadLink = windowsDownloadLink;
				modInfo.modId = modId;
				modInfo.numericalId = value.ToString() ?? "";
				modInfo.structureVersion = ModInfo.globalStructureVersion;
				if (text2 != "")
				{
					modInfo.PopulateFromInfoString(text2);
				}
				NetworkerMenuController.totalInstalled.Add(modInfo);
				installedMods.Add(modInfo);
				InstalledModInfo installedModInfo = new InstalledModInfo();
				installedModInfo.manifestPath = text;
				installedModInfo.palletBarcode = (string)val["objects"]["1"]["palletBarcode"];
				installedModInfo.palletPath = (string)val["objects"]["1"]["palletPath"];
				installedModInfo.catalogPath = (string)val["objects"]["1"]["catalogPath"];
				installedModInfo.ModInfo = modInfo;
				InstalledModInfo item = installedModInfo;
				InstalledModInfos.Add(item);
			}
			catch (Exception)
			{
			}
		}
		MelonLogger.Msg("PopulateInstalledMods: Found " + installedMods.Count + " installed mods in " + directory);
	}

	public void OnStartServer()
	{
		ModlistMenu.Refresh(openMenu: false);
		confirmedHostHasIt = true;
	}

	public void OnDisconnect()
	{
		ModlistMessage.avatarMods.Clear();
		ModlistMenu.Clear();
		confirmedHostHasIt = false;
		modNumericalsDownloadedDuringLobbySession.Clear();
		DeleteAllTempMods();
	}

	public override void OnApplicationQuit()
	{
		DeleteAllTempMods();
	}

	public void OnPlayerJoin(PlayerID playerId)
	{
		if (NetworkInfo.HasServer && NetworkInfo.IsHost)
		{
			SendAllMods();
			SendAllAvatars();
		}
	}

	public void OnPlayerRepCreated(NetworkPlayer networkPlayer, RigManager manager)
	{
		if (!networkPlayer.NetworkEntity.IsOwner)
		{
			new AvatarDownloadBar(networkPlayer);
		}
	}

	private void SendAllAvatars()
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<PlayerID, ModInfo> avatarMod in ModlistMessage.avatarMods)
		{
			ModlistData modlistData = ModlistData.Create(avatarMod.Key, avatarMod.Value, ModlistData.ModType.AVATAR);
			NetWriter val = NetWriter.Create();
			try
			{
				modlistData.Serialize((INetSerializer)(object)val);
				NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
				try
				{
					MessageSender.BroadcastMessageExcept((byte)avatarMod.Key, (NetworkChannel)0, val2, true);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}

	private void SendAllMods()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (ModInfo subscribedMod in subscribedMods)
		{
			bool final = num == subscribedMods.Count - 1;
			ModlistData modlistData = ModlistData.Create(final, subscribedMod);
			NetWriter val = NetWriter.Create();
			try
			{
				modlistData.Serialize((INetSerializer)(object)val);
				NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
				try
				{
					MessageSender.BroadcastMessageExceptSelf((NetworkChannel)0, val2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
			num++;
		}
	}

	private void PrepareModFiles()
	{
		if (!Directory.Exists(MODIO_MODNETWORKER_DIRECTORY))
		{
			Directory.CreateDirectory(MODIO_MODNETWORKER_DIRECTORY);
		}
		if (!File.Exists(MODIO_AUTH_TXT_DIRECTORY))
		{
			CreateDefaultAuthText(MODIO_AUTH_TXT_DIRECTORY);
		}
		if (!File.Exists(MODIO_BLACKLIST_TXT_DIRECTORY))
		{
			CreateDefaultBlacklistText(MODIO_BLACKLIST_TXT_DIRECTORY);
		}
	}

	private void CreateDefaultBlacklistText(string directory)
	{
		using StreamWriter streamWriter = File.CreateText(directory);
		streamWriter.WriteLine("#                       ----- WELCOME TO THE MOD.IO BLACKLIST TXT! -----");
		streamWriter.WriteLine("#");
		streamWriter.WriteLine("# This file is where you put mods that you DO NOT want to download under any circumstances.");
		streamWriter.WriteLine("# If you want to blacklist a mod, simply put the mod ID in this file, and it will not be downloaded.");
		streamWriter.WriteLine("# You can find the mod ID by going to the mod.io page for the mod, and looking at the URL.");
		streamWriter.WriteLine("# The mod ID is the name at the end of the URL.");
		streamWriter.WriteLine("# For example, if the URL is https://mod.io/g/bonelab/m/remove-bodylog-transform-vfx, the mod ID is remove-bodylog-transform-vfx");
		streamWriter.WriteLine("# To blacklist mods, simply put each mod ID on a new line. DO NOT START YOUR LINES WITH #, as this will comment out the line.");
		streamWriter.WriteLine("# Ex. ");
		streamWriter.WriteLine("# remove-bodylog-transform-vfx");
		streamWriter.WriteLine("# my-awesome-replacer");
		streamWriter.WriteLine("# annoying-mod");
	}

	private void CreateDefaultAuthText(string directory)
	{
		using StreamWriter streamWriter = File.CreateText(directory);
		streamWriter.WriteLine("#                       ----- WELCOME TO THE MOD.IO AUTH TXT! -----");
		streamWriter.WriteLine("#");
		streamWriter.WriteLine("# Put your mod.io OAuth token in this file, and it will be used to download mods from the mod.io network.");
		streamWriter.WriteLine("# Your OAuth token can be found here: https://mod.io/me/access");
		streamWriter.WriteLine("# At the bottom, you should see a section called 'OAuth Access'");
		streamWriter.WriteLine("# Create a key, then create a token using the + Icon. call it whatever you'd like, this doesnt matter.");
		streamWriter.WriteLine("# Then create a token, call it whatever you'd like, this doesnt matter.");
		streamWriter.WriteLine("# The token is pretty long, so make sure you copy the entire thing. Make sure you're copying the token, not the key.");
		streamWriter.WriteLine("# Once you've copied the token, paste it in this file, replacing the text labeled REPLACE_THIS_TEXT_WITH_YOUR_TOKEN.");
		streamWriter.WriteLine("AuthToken=REPLACE_THIS_TEXT_WITH_YOUR_TOKEN");
	}

	private List<string> ReadBlacklist()
	{
		string[] array = File.ReadAllLines(MODIO_BLACKLIST_TXT_DIRECTORY);
		List<string> list = new List<string>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!text.StartsWith("#") && text != "")
			{
				list.Add(text.Trim());
			}
		}
		return list;
	}

	public static void WriteLineToBlacklist(string line)
	{
		using StreamWriter streamWriter = new StreamWriter(MODIO_BLACKLIST_TXT_DIRECTORY, append: true);
		streamWriter.WriteLine(line);
	}

	public static void RemoveLineFromBlacklist(string line)
	{
		string tempFileName = Path.GetTempFileName();
		using (StreamReader streamReader = new StreamReader(MODIO_BLACKLIST_TXT_DIRECTORY))
		{
			using StreamWriter streamWriter = new StreamWriter(tempFileName);
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				if (text != line)
				{
					streamWriter.WriteLine(text);
				}
			}
		}
		File.Delete(MODIO_BLACKLIST_TXT_DIRECTORY);
		File.Move(tempFileName, MODIO_BLACKLIST_TXT_DIRECTORY);
	}

	private string ReadAuthKey()
	{
		string[] array = File.ReadAllLines(MODIO_AUTH_TXT_DIRECTORY);
		string text = "";
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (!text2.StartsWith("#"))
			{
				text += text2;
			}
		}
		return text.Replace("AuthToken=", "").Replace("REPLACE_THIS_TEXT_WITH_YOUR_TOKEN", "").Trim();
	}
}
