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
using Il2CppSLZ.Marrow.Forklift.Model;
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
using Newtonsoft.Json.Linq;

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

	// Deferred enrichment queue — processes after AssetWarehouse loads pallets
	public static Queue<ModInfo> deferredEnrichmentQueue = new Queue<ModInfo>();
	public static List<string> deferredEnrichmentBarcodes = new List<string>();
	public static bool isProcessingDeferredEnrichment = false;
	public static bool hasAdvertisedWarehouseNotReady = false;

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
		if (warehouseReloadRequested && (!assetWarehouseLoaded || !AssetWarehouse.Instance._initialLoaded || palletLock))
		{
			if (palletLock) MelonLogger.Warning("[WarehouseReload] Blocked by palletLock=true");
			if (!assetWarehouseLoaded) MelonLogger.Warning("[WarehouseReload] Blocked by warehouse not loaded");
			if (!AssetWarehouse.Instance._initialLoaded) MelonLogger.Warning("[WarehouseReload] Blocked by _initialLoaded=false");
		}
		if (warehouseReloadRequested && assetWarehouseLoaded && AssetWarehouse.Instance._initialLoaded && !palletLock)
		{
			MelonLogger.Msg("[WarehouseReload] Starting warehouse reload — folders: " + warehouseReloadFolders.Count + " updates: " + warehousePalletReloadTargets.Count);
			bool flag2 = false;
			if (warehousePalletReloadTargets.Count > 0)
			{
				MelonLogger.Msg("[WarehouseReload] Processing pallet update for " + warehousePalletReloadTargets[0]);
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
				MelonLogger.Msg("[WarehouseReload] Processing folder " + warehouseReloadFolders[0] + " (" + (warehouseReloadFolders.Count + warehousePalletReloadTargets.Count) + " remaining)");
				AssetWarehouse.Instance.LoadPalletFromFolderAsync(warehouseReloadFolders[0], true, (string)null, ModlistMenu.activeDownloadModInfo.ToModListing());
				warehouseReloadFolders.RemoveAt(0);
				palletLock = true;
			}
			if (warehouseReloadFolders.Count == 0 && warehousePalletReloadTargets.Count == 0)
			{
				MelonLogger.Msg("[WarehouseReload] Complete — notifications sent, state reset");
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
					MelonLogger.Msg("[AutoDownload] Skipping mature avatar " + (item.modName ?? "unknown") + " — mature content disabled");
					continue;
				}
					ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = item,
						notify = false
					});
				}
			}
			MelonLogger.Msg("[AutoDownload] Processed " + modNumericalsDownloadedDuringLobbySession.Count + " waitAndQueue items — clearing");
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
			MelonLogger.Msg("[RefreshInstalledMods] Starting — clearing lists and spawning scan thread");
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
		// Process deferred manifest enrichment queue (runs after warehouse is ready)
		ProcessDeferredEnrichmentQueue();
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
		// Also push the subscription to mod.io server
		if (!string.IsNullOrEmpty(modInfo.numericalId) && modInfo.numericalId != "0")
		{
			MelonLogger.Msg("[Subscription] Pushing subscription to mod.io server for mod " + (modInfo.modName ?? modInfo.modId ?? modInfo.numericalId));
			ModFileManager.Subscribe(modInfo.numericalId);
		}
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
		System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
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
				MelonLogger.Msg("PopulateInstalledMods: Skipping non-manifest file: " + text);
				continue;
			}
			MelonLogger.Msg("PopulateInstalledMods: Found manifest file: " + text);
			dynamic val = JsonConvert.DeserializeObject<object>(File.ReadAllText(text));
			// Log top-level JSON keys to verify manifest structure
			try
			{
				JObject jsonObj = JObject.Parse(File.ReadAllText(text));
				string keys = "";
				foreach (var prop in jsonObj.Properties())
				{
					keys += prop.Name + ", ";
				}
				if (jsonObj["objects"] != null)
				{
					string objKeys = "";
					foreach (var prop in ((JObject)jsonObj["objects"]).Properties())
					{
						objKeys += prop.Name + ", ";
					}
					MelonLogger.Msg("PopulateInstalledMods: Manifest '" + Path.GetFileName(text) + "' top keys: " + keys + " | objects child keys: " + objKeys);
				}
				else
				{
					MelonLogger.Warning("PopulateInstalledMods: Manifest '" + Path.GetFileName(text) + "' has NO 'objects' key at all!");
				}
			}
			catch (Exception ex2)
			{
				MelonLogger.Error("PopulateInstalledMods: Failed to parse manifest JSON structure from " + text + ": " + ex2.Message);
			}
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

				// Check if manifest is missing any required structure (1, 2, 3, 4, or targets/data)
				try
				{
					bool needsEnrichment = false;
					string enrichReason = "";
					
					// Check if objects["2"] targets are empty
					var targets2 = val["objects"]["2"]["targets"];
					bool hasPcTarget = false;
					bool hasAndroidTarget = false;
					try { hasPcTarget = targets2["pc"] != null; } catch { }
					try { hasAndroidTarget = targets2["android"] != null; } catch { }
					
					if (!hasPcTarget && !hasAndroidTarget)
					{
						needsEnrichment = true;
						enrichReason = "targets are empty";
					}
					
					// If targets exist, check the referenced target objects (3, 4)
					if (!needsEnrichment)
					{
						// Check PC target object
						if (hasPcTarget)
						{
							try
							{
								int pcRef = (int)targets2["pc"]["ref"];
								var pcObj = val["objects"][pcRef.ToString()];
								if (pcObj == null) { needsEnrichment = true; enrichReason = "PC target object (ref " + pcRef + ") missing"; }
								else
								{
									bool pcHasModId = false;
									bool pcHasModfileId = false;
									try { pcHasModId = (int)pcObj["modId"] != 0; } catch { }
									try { pcHasModfileId = (int)pcObj["modfileId"] != 0; } catch { }
									if (!pcHasModId || !pcHasModfileId) { needsEnrichment = true; enrichReason = "PC target missing modId/modfileId"; }
								}
							}
							catch { needsEnrichment = true; enrichReason = "PC target ref resolution failed"; }
						}
						
						// Check Android target object
						if (hasAndroidTarget && !needsEnrichment)
						{
							try
							{
								int androidRef = (int)targets2["android"]["ref"];
								var androidObj = val["objects"][androidRef.ToString()];
								if (androidObj == null) { needsEnrichment = true; enrichReason = "Android target object (ref " + androidRef + ") missing"; }
								else
								{
									bool androidHasModId = false;
									bool androidHasModfileId = false;
									try { androidHasModId = (int)androidObj["modId"] != 0; } catch { }
									try { androidHasModfileId = (int)androidObj["modfileId"] != 0; } catch { }
									if (!androidHasModId || !androidHasModfileId) { needsEnrichment = true; enrichReason = "Android target missing modId/modfileId"; }
								}
							}
							catch { needsEnrichment = true; enrichReason = "Android target ref resolution failed"; }
						}
						
						// Even if targets exist, check that objects["3"] and ["4"] are actually present
						if (!needsEnrichment)
						{
							bool hasObj3 = false;
							bool hasObj4 = false;
							try { hasObj3 = val["objects"]["3"] != null; } catch { }
							try { hasObj4 = val["objects"]["4"] != null; } catch { }
							if (!hasObj3) { needsEnrichment = true; enrichReason = "objects[\"3\"] missing"; }
							else if (!hasObj4) { needsEnrichment = true; enrichReason = "objects[\"4\"] missing"; }
						}
					}
					
					if (needsEnrichment)
					{
						MelonLogger.Msg("[ManifestEnrichment] Manifest " + Path.GetFileName(text) + " needs enrichment: " + enrichReason);
						
						string manifestFilePath = text;
						string modFolderPath = manifestFilePath.EndsWith(".manifest")
							? manifestFilePath.Substring(0, manifestFilePath.Length - ".manifest".Length)
							: "";
						string modInfoPath = System.IO.Path.Combine(modFolderPath, "modinfo.json");
						
						if (File.Exists(modInfoPath))
						{
							string modInfoJson2 = File.ReadAllText(modInfoPath);
							ModInfo modInfoFromFile2 = JsonConvert.DeserializeObject<ModInfo>(modInfoJson2, new JsonSerializerSettings
							{
								MissingMemberHandling = MissingMemberHandling.Ignore,
								Error = (sender, args) => args.ErrorContext.Handled = true
							});
							
							if (modInfoFromFile2 != null && !string.IsNullOrEmpty(modInfoFromFile2.numericalId))
							{
								string barcode2 = "";
								if (val != null && val["objects"] != null && val["objects"]["1"] != null)
								{
									barcode2 = (string)val["objects"]["1"]["palletBarcode"];
								}
								deferredEnrichmentQueue.Enqueue(modInfoFromFile2);
								deferredEnrichmentBarcodes.Add(barcode2);
								MelonLogger.Msg("[ManifestEnrichment] Queued " + (modInfoFromFile2.modName ?? barcode2) + " for enrichment (reason: " + enrichReason + ", queue size: " + deferredEnrichmentQueue.Count + ")");
							}
							else
							{
								MelonLogger.Warning("[ManifestEnrichment] Cannot enrich " + Path.GetFileName(text) + " — modinfo.json missing numericalId (reason: " + enrichReason + ")");
							}
						}
						else
						{
							MelonLogger.Warning("[ManifestEnrichment] Cannot enrich " + Path.GetFileName(text) + " — no modinfo.json (reason: " + enrichReason + ")");
						}
					}
				}
				catch (Exception enrichCheckEx)
				{
					MelonLogger.Warning("[ManifestEnrichment] Failed to check manifest structure for " + Path.GetFileName(text) + ": " + enrichCheckEx.Message);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("PopulateInstalledMods: Failed to parse manifest " + text + ": " + ex.Message);

				// === OPTION B: Manifest Enrichment from modinfo.json ===
				// PC manifests lack objects["2"]. Rebuild the mod listing from modinfo.json
				// which was written when the mod was downloaded through the Networker.

				MelonLogger.Msg("[ManifestEnrichment] Manifest " + Path.GetFileName(text) + " has no objects[\"2\"] — attempting enrichment from modinfo.json");

				string manifestFilePath = text;
				string modFolderPath = manifestFilePath.EndsWith(".manifest")
					? manifestFilePath.Substring(0, manifestFilePath.Length - ".manifest".Length)
					: "";

				try
				{
					string barcode = "";
					if (val != null && val["objects"] != null && val["objects"]["1"] != null)
					{
						barcode = (string)val["objects"]["1"]["palletBarcode"];
					}

					if (string.IsNullOrEmpty(modFolderPath) || !Directory.Exists(modFolderPath))
					{
						MelonLogger.Warning("[ManifestEnrichment] Mod folder not found at " + modFolderPath + " — cannot enrich " + Path.GetFileName(text));
					}
					else
					{
						string modInfoPath = System.IO.Path.Combine(modFolderPath, "modinfo.json");
						if (!File.Exists(modInfoPath))
						{
							MelonLogger.Warning("[ManifestEnrichment] No modinfo.json found at " + modInfoPath + " — cannot enrich " + (barcode ?? Path.GetFileName(text)) + " (mod may have been installed manually, not through Networker)");
						}
						else
						{
							MelonLogger.Msg("[ManifestEnrichment] Found modinfo.json at " + modInfoPath + " — attempting enrichment for " + (barcode ?? Path.GetFileName(text)));

							string modInfoJson = File.ReadAllText(modInfoPath);
							ModInfo modInfoFromFile = JsonConvert.DeserializeObject<ModInfo>(modInfoJson, new JsonSerializerSettings
							{
								MissingMemberHandling = MissingMemberHandling.Ignore,
								Error = (sender, args) => args.ErrorContext.Handled = true
							});

							if (modInfoFromFile == null)
							{
								MelonLogger.Warning("[ManifestEnrichment] Failed to deserialize modinfo.json for " + (barcode ?? Path.GetFileName(text)));
							}
							else
							{
								MelonLogger.Msg("[ManifestEnrichment] Loaded modinfo.json: modName=" + (modInfoFromFile.modName ?? "null") + " numericalId=" + (modInfoFromFile.numericalId ?? "null"));

								if (string.IsNullOrEmpty(modInfoFromFile.numericalId))
								{
									MelonLogger.Warning("[ManifestEnrichment] numericalId is missing from modinfo.json for " + (modInfoFromFile.modName ?? "unknown") + " — cannot create ModIOModTarget");
								}
								else
								{
									// Build a rich ModListing from modinfo.json data
									ModListing richModListing = modInfoFromFile.ToModListing();
									MelonLogger.Msg("[ManifestEnrichment] ToModListing() completed for " + (modInfoFromFile.modName ?? barcode) + " — ModListing has targets count: " + ((richModListing?.Targets?.Count ?? 0).ToString()));

									// Queue for deferred enrichment — warehouse may not be ready yet
									deferredEnrichmentQueue.Enqueue(modInfoFromFile);
									deferredEnrichmentBarcodes.Add(barcode);
									MelonLogger.Msg("[ManifestEnrichment] Queued " + (modInfoFromFile.modName ?? barcode) + " for deferred enrichment (queue size: " + deferredEnrichmentQueue.Count + ")");
								}
							}
						}
					}
				}
				catch (Exception ex2)
				{
					MelonLogger.Error("[ManifestEnrichment] Option B failed for " + manifestFilePath + ": " + ex2.Message);
				}
			}
		}
		MelonLogger.Msg("PopulateInstalledMods: Found " + installedMods.Count + " installed mods in " + directory);
		MelonLogger.Msg("PopulateInstalledMods: Total mod files found in directory: " + files.Length + " — " + installedMods.Count + " parsed successfully, " + deferredEnrichmentQueue.Count + " queued for deferred enrichment, " + (files.Length - installedMods.Count - deferredEnrichmentQueue.Count) + " skipped");
		sw.Stop();
		MelonLogger.Msg("[PopulateInstalledMods] " + directory + " scan complete in " + sw.ElapsedMilliseconds + "ms");
	}

	public void ProcessDeferredEnrichmentQueue()
	{
		if (isProcessingDeferredEnrichment || deferredEnrichmentQueue.Count == 0)
			return;

		isProcessingDeferredEnrichment = true;

		try
		{
			int processed = 0;
			int succeeded = 0;
			int failed = 0;

			MelonLogger.Msg("[DeferredEnrichment] Processing deferred enrichment queue — " + deferredEnrichmentQueue.Count + " mods pending");

			while (deferredEnrichmentQueue.Count > 0)
			{
				ModInfo modInfo = deferredEnrichmentQueue.Dequeue();
				string barcode = deferredEnrichmentBarcodes.Count > 0 ? deferredEnrichmentBarcodes[0] : "";
				if (deferredEnrichmentBarcodes.Count > 0)
					deferredEnrichmentBarcodes.RemoveAt(0);

				processed++;

				if (string.IsNullOrEmpty(barcode))
				{
					MelonLogger.Warning("[DeferredEnrichment] Skipping deferred enrichment — no barcode for mod " + (modInfo.modName ?? "unknown"));
					failed++;
					continue;
				}

				// Check if warehouse is ready and pallet is registered
				if (AssetWarehouse.Instance == null || AssetWarehouse.Instance.palletManifests == null)
				{
					MelonLogger.Warning("[DeferredEnrichment] AssetWarehouse not available yet — requeuing " + (modInfo.modName ?? barcode));
					deferredEnrichmentQueue.Enqueue(modInfo);
					deferredEnrichmentBarcodes.Add(barcode);
					isProcessingDeferredEnrichment = false;
					return;
				}

				if (!AssetWarehouse.Instance.palletManifests.ContainsKey(new Barcode(barcode)))
				{
					MelonLogger.Warning("[DeferredEnrichment] Pallet not found yet for " + (modInfo.modName ?? barcode) + " — will retry next frame");
					// Re-queue for retry
					deferredEnrichmentQueue.Enqueue(modInfo);
					deferredEnrichmentBarcodes.Add(barcode);
					// Stop processing for now — let OnUpdate try again later
					isProcessingDeferredEnrichment = false;
					return;
				}

				// Warehouse is ready and pallet found — do the enrichment
				MelonLogger.Msg("[DeferredEnrichment] Processing deferred enrichment for " + (modInfo.modName ?? barcode) + " (#" + processed + " of " + (processed + deferredEnrichmentQueue.Count) + ")");

				try
				{
					ModListing richModListing = modInfo.ToModListing();
					MelonLogger.Msg("[DeferredEnrichment] ToModListing() completed for " + (modInfo.modName ?? barcode) + " — targets count: " + (richModListing?.Targets?.Count ?? 0));

					PalletManifest existingManifest = AssetWarehouse.Instance.palletManifests[new Barcode(barcode)];
					if (existingManifest != null && existingManifest.Pallet != null)
					{
						MelonLogger.Msg("[DeferredEnrichment] Calling LoadAndUpdatePalletManifest for " + barcode + " (pallet=" + (existingManifest.Pallet.name ?? "null") + ")");

						AssetWarehouse.Instance.LoadAndUpdatePalletManifest(
							existingManifest.Pallet,
							richModListing,
							existingManifest.PalletPath,
							existingManifest.CatalogPath,
							(IResourceLocator)null
						);

						MelonLogger.Msg("[DeferredEnrichment] ✅ Successfully enriched manifest for " + (modInfo.modName ?? barcode));

						// Add enriched mod to display lists so it shows in the installed tab
						if (modInfo != null)
						{
							NetworkerMenuController.totalInstalled.Add(modInfo);
							installedMods.Add(modInfo);
							MelonLogger.Msg("[DeferredEnrichment] Added " + (modInfo.modName ?? barcode) + " to installed mods list (now " + NetworkerMenuController.totalInstalled.Count + " total installed)");
						}

						succeeded++;
					}
					else
					{
						MelonLogger.Warning("[DeferredEnrichment] Manifest or Pallet was null for " + barcode + " — cannot enrich");
						failed++;
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Error("[DeferredEnrichment] Failed to enrich " + (modInfo.modName ?? barcode) + ": " + ex.Message);
					MelonLogger.Error("[DeferredEnrichment] Stack trace: " + ex.StackTrace);
					failed++;
				}
			}

			MelonLogger.Msg("[DeferredEnrichment] Enrichment complete — " + succeeded + " succeeded, " + failed + " failed out of " + processed + " processed");

			// Refresh UI since new mods were enriched
			if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
			{
				NetworkerMenuController.instance.Refresh();
				MelonLogger.Msg("[DeferredEnrichment] Refreshed NetworkerMenuController UI");
			}
			else
			{
				MelonLogger.Warning("[DeferredEnrichment] NetworkerMenuController.instance is null — UI refresh skipped");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("[DeferredEnrichment] Queue processing failed: " + ex.Message);
		}
		finally
		{
			isProcessingDeferredEnrichment = false;
		}
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
