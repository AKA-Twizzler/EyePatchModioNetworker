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

	// Cache file for subscription data to persist across restarts
	private static readonly string subDataCachePath = Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ModioModNetworker", "subscription_cache.json");

	public static bool warehouseReloadRequested = false;

	public static List<string> warehousePalletReloadTargets = new List<string>();

	public static List<string> warehouseReloadFolders = new List<string>();

	public static bool subsChanged = false;

	public static bool refreshInstalledModsRequested = false;

	public static bool refreshSubscribedModsRequested = false;

	public static bool menuRefreshRequested = false;

	public static volatile string subscriptionThreadString = "";

	public static volatile string trendingThreadString = "";

	public static bool subsRefreshing = false;

	private static bool _processingSubscriptionData = false;

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

	private static int diagUpdateCount = 0;

	public override void OnInitializeMelon()
	{
		MelonLogger.Msg("DIAG: MOD INIT START");
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
		NetworkerAssets.LoadAssetsUI(bundle);
		PrepareModFiles();
		// Delete stale Networker manifest files to prevent AssetWarehouse crash during pallet loading
		try
		{
			string modFolder = ModFileManager.MOD_FOLDER_PATH;
			if (Directory.Exists(modFolder))
			{
				string[] rootManifests = Directory.GetFiles(modFolder, "*.manifest");
				foreach (string manifest in rootManifests)
				{
					string name = Path.GetFileNameWithoutExtension(manifest);
					if (!name.StartsWith("SLZ."))
					{
						File.Delete(manifest);
					}
				}
				
				// One-time purge: delete any leftover subfolder manifests (legacy cleanup)
				string[] modDirs = Directory.GetDirectories(modFolder);
				foreach (string dir in modDirs)
				{
					string[] subManifests = Directory.GetFiles(dir, "*.manifest");
					foreach (string subManifest in subManifests)
					{
						File.Delete(subManifest);
					}
				}
				
				MelonLogger.Msg("Pre-init: Cleaned stale mod manifests (NRE prevention)");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Pre-init manifest cleanup failed: " + ex.Message);
		}
		// Immediately backfill manifests so AssetWarehouse finds them during init
		try
		{
			BackfillManifests();
			MelonLogger.Msg("OnInitializeMelon: Pre-initialization manifest backfill complete");
		}
		catch (Exception ex)
		{
			MelonLogger.Error("OnInitializeMelon: Pre-init BackfillManifests failed: " + ex.Message);
		}
		string text = ReadAuthKey();
		if (!string.IsNullOrEmpty(text))
		{
			MelonLogger.Msg("Found auth.txt token - bypassing Fusion 5-digit code login");
			OnLoadToken(text);
		}
		blacklistedModIoIds = ReadBlacklist();
		MelonLogger.Msg("Loaded blacklist with " + blacklistedModIoIds.Count + " entries.");
		ModIOSettings.LoadToken((Action<string>)OnLoadToken);
		MelonLogger.Msg("Loading internal module...");
		ModuleManager.RegisterModule<ModlistModule>();
		ModFileManager.Initialize();
		ModlistMenu.Initialize();
		MultiplayerHooking.OnPlayerJoined += new PlayerUpdate(OnPlayerJoin);
		MultiplayerHooking.OnDisconnected += new ServerEvent(OnDisconnect);
		MultiplayerHooking.OnStartedServer += new ServerEvent(OnStartServer);
		NetworkPlayer.OnNetworkRigCreated += OnPlayerRepCreated;
		MelonLogger.Msg("DIAG: Waiting for AssetWarehouse init...");
		AssetWarehouse.OnReady(new System.Action(delegate
		{
			MelonLogger.Msg("DIAG: OnReady FIRED");
			AssetWarehouse instance = AssetWarehouse.Instance;
			instance.OnCrateAdded += new Action<Barcode>(delegate(Barcode s)
			{
				palletLock = false;
				LevelHoldQueue.CheckValid(s._id);
				SpawnableHoldQueue.CheckValid(s._id);
				// Only force avatar refresh for local player (safely - may not be initialized during AssetWarehouse startup)
				try
				{
					if (PlayerIDManager.LocalID != null)
					{
						if (NetworkPlayerManager.TryGetPlayer((byte)PlayerIDManager.LocalID, out var localPlayer))
						{
							var field = localPlayer.AvatarSetter.GetType().GetField("_isAvatarDirty", BindingFlags.Instance | BindingFlags.NonPublic);
							if (field != null)
								field.SetValue(localPlayer.AvatarSetter, true);
						}
					}
				}
				catch (Exception ex)
				{
					MelonLogger.Warning("_isAvatarDirty: " + ex.Message);
				}
			});

			assetWarehouseLoaded = true;
			MelonLogger.Msg("DIAG: assetWarehouseLoaded = true");
			DeleteAllTempMods();
		}));
		MelonLogger.Msg("DIAG: OnReady registered");
		void OnLoadToken(string loadedToken)
		{
			ModFileManager.OAUTH_KEY = loadedToken;
			MelonLogger.Msg("Populating currently installed mods via this mod.");
			installedMods.Clear();
			InstalledModInfos.Clear();
			NetworkerMenuController.totalInstalled.Clear();
			BackfillManifests();
			PopulateInstalledMods(ModFileManager.MOD_FOLDER_PATH);
			loadedInstalled = true;
			MelonLogger.Msg("Checking mod.io account subscriptions");
			PopulateSubscriptions();
			ModFileManager.QueueTrending(0);
			MelonLogger.Msg("Registered on mod.io with auth key!");
		}
	}

	private void OnLobbyCategoryMade(Page category, INetworkLobby lobby)
	{
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
		MelonLogger.Msg("DIAG: DeleteAllTempMods start");
		foreach (ModInfo installedMod in installedMods)
		{
			if (installedMod.temp)
			{
				ModFileManager.UnInstallMainThread(installedMod.numericalId);
			}
		}
		MelonLogger.Msg("DIAG: DeleteAllTempMods end");
	}

	public override void OnUpdate()
	{
		if (diagUpdateCount < 5) { MelonLogger.Msg("DIAG: OnUpdate tick " + (diagUpdateCount + 1)); }
		diagUpdateCount++;
		
		// Snapshot to avoid collection modification exceptions
		try
		{
			List<AvatarDownloadBar> barsSnapshot = new List<AvatarDownloadBar>(AvatarDownloadBar.bars.Values);
			foreach (AvatarDownloadBar value3 in barsSnapshot)
			{
				value3.Update();
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("OnUpdate: AvatarDownloadBar error: " + ex.Message);
		}
		try
		{
			ThumbnailThreader.HandleQueue();
			MainThreadManager.HandleQueue();
			LevelHoldQueue.Update();
		}
		catch (Exception ex2)
		{
			MelonLogger.Error("OnUpdate: Pre-subscription error: " + ex2.Message);
		}
		if (ModFileManager.activeDownloadQueueElement != null && ModFileManager.activeDownloadQueueElement.associatedPlayer != null && AvatarDownloadBar.bars.TryGetValue(ModFileManager.activeDownloadQueueElement.associatedPlayer, out AvatarDownloadBar value))
		{
			ModInfo activeDownloadModInfo = ModlistMenu.activeDownloadModInfo;
			if (activeDownloadModInfo != null)
			{
				value.SetModName(activeDownloadModInfo.modId);
				value.SetPercentage((float)activeDownloadModInfo.modDownloadPercentage);
			}
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
				if (val == null)
			{
				MelonLogger.Error("OnUpdate: Pallet manifest not found for " + warehousePalletReloadTargets[0] + ", skipping reload");
				warehousePalletReloadTargets.RemoveAt(0);
				flag2 = true;
			}
			else
			{
				if (ModlistMenu.activeDownloadModInfo != null)
				{
					AssetWarehouse.Instance.LoadAndUpdatePalletManifest(val.Pallet, ModlistMenu.activeDownloadModInfo.ToModListing(), val.PalletPath, val.CatalogPath, (IResourceLocator)null);
				}
				warehousePalletReloadTargets.RemoveAt(0);
				flag2 = true;
			}
			}
			if (warehouseReloadFolders.Count > 0)
			{
				if (ModlistMenu.activeDownloadModInfo != null)
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
				if (ModFileManager.activeDownloadQueueElement != null && ModFileManager.activeDownloadQueueElement.notify && ModlistMenu.activeDownloadModInfo != null)
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
			handlingInstalled = true;
			Thread thread = new Thread((ThreadStart)delegate
			{
				BackfillManifests();
				PopulateInstalledMods(ModFileManager.MOD_FOLDER_PATH);
			MainThreadManager.QueueAction(delegate
			{
				try
				{
					if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
					{
						NetworkerMenuController.instance.Refresh();
					}
					// Reset flags before cross-reference to avoid stale data
					foreach (ModInfo resetMod in installedMods)
					{
						resetMod.isSubscribed = false;
					}
					// Re-apply cross-reference data to newly scanned mods
					CrossReferenceInstalledMods();
					// Update modinfo.json on disk with fresh subscription data for matched mods
					foreach (InstalledModInfo installedInfo in InstalledModInfos)
					{
						if (installedInfo?.ModInfo == null) continue;
						string installedNumericalId = installedInfo.ModInfo.numericalId;
						if (string.IsNullOrEmpty(installedNumericalId) || installedNumericalId == "0") continue;
						foreach (ModInfo sub in subscribedMods)
						{
							if (sub.numericalId == installedNumericalId)
							{
								UpdateModInfo(sub, installedInfo);
								break;
							}
						}
					}
				}
				finally
				{
					handlingInstalled = false;
				}
			});
			});
			thread.Start();
			loadedInstalled = true;
			refreshInstalledModsRequested = false;
			if ((UnityEngine.Object)(object)NetworkerMenuController.instance != null)
			{
				NetworkerMenuController.instance.UpdateModPopupButtons();
			}
		}
		if (subscriptionThreadString != "" && !_processingSubscriptionData)
		{
			_processingSubscriptionData = true;
			try
			{
				InternalPopulateSubscriptions();
			}
			finally
			{
				_processingSubscriptionData = false;
			}
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
			string path = Path.Combine(Directory.GetParent(installed.palletPath).FullName, "modinfo.json");
			string tempPath = path + ".tmp";
			string contents = JsonConvert.SerializeObject((object)modInfo);
			File.WriteAllText(tempPath, contents);
			File.Delete(path);
			File.Move(tempPath, path);
			MelonLogger.Msg($"Updated modinfo.json for {modInfo.modId} to version {modInfo.structureVersion}");
		}
		catch (Exception ex)
		{
			MelonLogger.Error("Skipped updating modinfo.json for " + installed.ModInfo.modId + " because of an error: " + ex);
		}
	}

	public static void ReceiveSubModInfo(ModInfo modInfo, bool ignoreTag = false)
	{
		if (modInfo.version == null)
		{
			modInfo.version = "0.0.0";
		}
		if (!modInfo.isValidMod)
		{
			toRemoveSubscribedModIoIds.Add(modInfo.numericalId);
		}

		// Check if already installed - skip download if up to date
		bool needsDownload = true;
		if (!string.IsNullOrEmpty(modInfo.numericalId) && modInfo.numericalId != "0")
		{
			foreach (ModInfo installed in installedMods)
			{
				if (installed.numericalId == modInfo.numericalId)
				{
					// Found installed mod with same numericalId
					string installedVer = installed.version ?? "0.0.0";
					string subVer = modInfo.version ?? "0.0.0";
					if (installedVer == subVer)
					{
						// Versions match - already up to date, skip download
						needsDownload = false;
					}
					else
					{
						// Version mismatch - needs update
						MelonLogger.Msg("ReceiveSubModInfo: Version mismatch for " + modInfo.modId
							+ " (local: " + installedVer + ", remote: " + subVer + ") - queuing update");
					}
					break;
				}
			}
		}

		if (needsDownload)
		{
			ModFileManager.AddToQueue(new DownloadQueueElement
			{
				associatedPlayer = null,
				info = modInfo
			}, ignoreTag);
		}

		subscribedModIoNumericalIds.Add(modInfo.numericalId);
		subscribedMods.Add(modInfo);
	}

	public static void LoadSubscriptionCache()
	{
		try
		{
			if (!File.Exists(subDataCachePath))
			{
				MelonLoader.MelonLogger.Msg("[Diag] Cache: No cache file found at " + subDataCachePath);
				return;
			}

			string json = File.ReadAllText(subDataCachePath);
			var cacheList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
			if (cacheList == null || cacheList.Count == 0)
			{
					return;
			}

			// Populate ModInfo objects from cache and add to totalInstalled
			int count = 0;
			foreach (var entry in cacheList)
			{
				if (entry.TryGetValue("modId", out string modId) && !string.IsNullOrEmpty(modId))
				{
					ModInfo cachedMod = new ModInfo();
					cachedMod.modId = modId;
					cachedMod.numericalId = entry.GetValueOrDefault("numericalId", null);
					cachedMod.modName = entry.GetValueOrDefault("modName", null);
					cachedMod.thumbnailLink = entry.GetValueOrDefault("thumbnailLink", null);
					if (float.TryParse(entry.GetValueOrDefault("fileSizeKB", "0"), out float fs))
						cachedMod.fileSizeKB = fs;

					// Try to match with installed mods
					foreach (ModInfo installedMod in NetworkerMenuController.totalInstalled)
					{
						if (installedMod.modId == modId)
						{
							bool updated = false;
							if (string.IsNullOrEmpty(installedMod.numericalId) && !string.IsNullOrEmpty(cachedMod.numericalId))
							{ installedMod.numericalId = cachedMod.numericalId; updated = true; }
							if (string.IsNullOrEmpty(installedMod.modName) && !string.IsNullOrEmpty(cachedMod.modName))
							{ installedMod.modName = cachedMod.modName; updated = true; }
							if (string.IsNullOrEmpty(installedMod.thumbnailLink) && !string.IsNullOrEmpty(cachedMod.thumbnailLink))
							{ installedMod.thumbnailLink = cachedMod.thumbnailLink; updated = true; }
							if (installedMod.fileSizeKB == 0f && cachedMod.fileSizeKB > 0f)
							{ installedMod.fileSizeKB = cachedMod.fileSizeKB; updated = true; }
							if (updated) count++;
							break;
						}
					}
				}
			}

		}
		catch (Exception ex)
		{
			MelonLoader.MelonLogger.Error($"[Diag] Cache: Failed to load: {ex.Message}");
		}
	}

	public static void CrossReferenceInstalledMods()
	{
		int count = 0;
		foreach (ModInfo installedMod in NetworkerMenuController.totalInstalled)
		{
			foreach (ModInfo subMod in subscribedMods)
			{
				bool matches = false;

				// Strategy 1: Match by numericalId (most reliable)
				if (!string.IsNullOrEmpty(installedMod.numericalId) &&
					!string.IsNullOrEmpty(subMod.numericalId) &&
					installedMod.numericalId == subMod.numericalId)
				{
					matches = true;
				}

				// Strategy 2: Match by display title (installed modId is the title from manifest)
				if (!matches && !string.IsNullOrEmpty(installedMod.modId) && 
				    !string.IsNullOrEmpty(subMod.modName))
				{
					// Check if installed modId (title) contains subscription modName or vice versa
					string installedTitle = installedMod.modId.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
					string subName = subMod.modName.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
					if (installedTitle.Contains(subName) || subName.Contains(installedTitle))
					{
						matches = true;
					}
				}

				// Strategy 3: Match by fileName (installed dl filename vs subscription filename)
				if (!matches && !string.IsNullOrEmpty(installedMod.fileName) &&
					!string.IsNullOrEmpty(subMod.fileName) &&
					installedMod.fileName == subMod.fileName)
				{
					matches = true;
				}

				// Strategy 4: Match by display name (installed modId is the manifest title)
				if (!matches && !string.IsNullOrEmpty(installedMod.modId) &&
					!string.IsNullOrEmpty(subMod.modName) &&
					installedMod.modId == subMod.modName)
				{
					matches = true;
				}

				// Strategy 5: Try to match installed mod's fileName (zip) against subscription patterns
				if (!matches && !string.IsNullOrEmpty(installedMod.fileName))
				{
					string fileNameLower = installedMod.fileName.ToLowerInvariant();
					string subModIdLower = subMod.modId.ToLowerInvariant();
					if (fileNameLower.Contains(subModIdLower) || subModIdLower.Contains(fileNameLower))
					{
						matches = true;
					}
				}

				if (matches)
				{
					bool updated = false;
					if (string.IsNullOrEmpty(installedMod.numericalId) && !string.IsNullOrEmpty(subMod.numericalId))
					{
						installedMod.numericalId = subMod.numericalId;
						updated = true;
					}
				if (!string.IsNullOrEmpty(subMod.modName))
				{
					if (installedMod.modName != subMod.modName)
					{
						installedMod.modName = subMod.modName;
						updated = true;
					}
				}
					if (string.IsNullOrEmpty(installedMod.thumbnailLink) && !string.IsNullOrEmpty(subMod.thumbnailLink))
					{
						installedMod.thumbnailLink = subMod.thumbnailLink;
						updated = true;
					}
					if (installedMod.fileSizeKB == 0f && subMod.fileSizeKB > 0f)
					{
						installedMod.fileSizeKB = subMod.fileSizeKB;
						updated = true;
					}
					installedMod.isSubscribed = true;
					if (updated) count++;
					break;
				}
			}
		}
		if (count > 0)
			MelonLogger.Msg($"[Diag] Cross-reference: Updated {count} installed mods");
	}

	public static void SaveSubscriptionCache()
	{
		try
		{
			string dirPath = Path.GetDirectoryName(subDataCachePath);
			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);

			// Build a simple JSON with just the fields we need for cross-reference
			var cacheList = new List<Dictionary<string, string>>();
			foreach (ModInfo mod in subscribedMods)
			{
				var entry = new Dictionary<string, string>();
				entry["modId"] = mod.modId ?? "";
				entry["numericalId"] = mod.numericalId ?? "";
				entry["modName"] = mod.modName ?? "";
				entry["thumbnailLink"] = mod.thumbnailLink ?? "";
				entry["fileSizeKB"] = mod.fileSizeKB.ToString();
				cacheList.Add(entry);
			}

			string json = Newtonsoft.Json.JsonConvert.SerializeObject(cacheList, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText(subDataCachePath, json);

		}
		catch (Exception ex)
		{
			MelonLoader.MelonLogger.Error($"[Diag] Cache: Failed to save: {ex.Message}");
		}
	}

	public static void PopulateSubscriptions()
	{
		if (!_processingSubscriptionData)
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
						int num3 = SafeInt(item2, "modfile_live");
						num = num3;
						break;
					}
				}
				foreach (dynamic item3 in item["platforms"])
				{
					if ((string)item3["platform"] == "android")
					{
						int num4 = SafeInt(item3, "modfile_live");
						num2 = num4;
						break;
					}
				}
				if (num != 0 && num2 != 0 && num == num2)
				{
					flag = false;
				}
				if (SafeInt(item, "status") == 3)
				{
					flag = false;
				}
				ModInfo modInfo = ModInfo.MakeFromDynamic(item["modfile"], text4);
				modInfo.isValidMod = false;
				modInfo.mature = SafeInt(item, "maturity_option") > 0;
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
		try
		{
			dynamic val = JsonConvert.DeserializeObject<object>(text);
		int num = 0;
		int num2 = SafeInt(val, "result_total");
		if (subTotal == 0)
		{
			MelonLogger.Msg("Total subscriptions: " + num2);
			subTotal = num2;
			desiredSubs = 0;
		}
		int num3 = SafeInt(val, "result_count");
		if (num3 == 0)
		{
			MelonLogger.Msg("No subscriptions found!");
			return;
		}
		if (val["data"] != null)
		{
			foreach (dynamic item in val["data"])
			{
				if (SafeInt(item, "game_id") == 3809)
				{
					num++;
				}
			}
		}
		desiredSubs += num;
		ModInfoThreadRequest result;
		while (ModInfo.modInfoThreadRequests.TryDequeue(out result))
		{
		}
		ModInfo.requestSize = num;
        if (val["data"] != null)
        {
            foreach (dynamic item2 in val["data"])
            {
                try
                {
                    if (SafeInt(item2, "game_id") != 3809)
                    {
                        continue;
                    }
                    string text2 = (string)item2["profile_url"];
                    string numericalId = ((string)item2["id"]) ?? "";
                    string modName = (string)item2["name"];
                    string modSummary = (string)item2["summary"];
                    var submittedBy = item2["submitted_by"];
                    string author = submittedBy != null ? (string)submittedBy["username"] : "";
                    var logo = item2["logo"];
                    string thumbnailLink = logo != null ? (string)logo["thumb_640x360"] : "";
                    string text3 = !string.IsNullOrEmpty(text2) ? text2.Split('/')[^1] : "";
                bool flag = true;
                int num4 = 0;
                int num5 = 0;
                var platforms = item2["platforms"];
                if (platforms != null)
                {
                    foreach (dynamic item3 in platforms)
                    {
                        if ((string)item3["platform"] == "windows")
                        {
                            int num6 = SafeInt(item3, "modfile_live");
                            num4 = num6;
                            break;
                        }
                    }
                    foreach (dynamic item4 in platforms)
                    {
                        if ((string)item4["platform"] == "android")
                        {
                            int num7 = SafeInt(item4, "modfile_live");
                            num5 = num7;
                            break;
                        }
                    }
                }
                if (num4 != 0 && num5 != 0 && num4 == num5)
                {
                    flag = false;
                }
                if (SafeInt(item2, "status") == 3)
                {
                    flag = false;
                }
                ModInfo modInfo = ModInfo.MakeFromDynamic(item2["modfile"], text3);
                modInfo.isValidMod = false;
                modInfo.mature = SafeInt(item2, "maturity_option") > 0;
                modInfo.modName = modName;
                modInfo.thumbnailLink = thumbnailLink;
                modInfo.modSummary = modSummary;
                modInfo.numericalId = numericalId;
                modInfo.author = author;
                var tags = item2["tags"];
                if (tags != null)
                {
                    foreach (dynamic item5 in tags)
                    {
                        modInfo.tags.Add((string)item5["name"]);
                    }
                }
                if (flag)
                {
                    modInfo.androidDownloadLink = string.Format("{0}{1}/files/{2}/download", ModFileManager.API_PATH, (object?)item2["id"], num5);
                    modInfo.windowsDownloadLink = string.Format("{0}{1}/files/{2}/download", ModFileManager.API_PATH, (object?)item2["id"], num4);
                    modInfo.isValidMod = true;
                }
                ReceiveSubModInfo(modInfo);
                NetworkerMenuController.modIoRetrieved.Add(modInfo);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to parse subscription mod: {ex.Message}");
                continue;
            }
        }
		subsShown += num3;
		if (subTotal - subsShown > 0)
		{
			ModFileManager.QueueSubscriptions(subsShown);
		}
		if (subsShown >= subTotal)
		{
			subsRefreshing = true;
			
			// Only cross-reference with COMPLETE subscription data
			// Use snapshot to avoid concurrent modification with background thread
			try
			{
				List<ModInfo> resetSnapshot = new List<ModInfo>(NetworkerMenuController.totalInstalled);
				foreach (ModInfo resetMod in resetSnapshot)
				{
					resetMod.isSubscribed = false;
				}
			}
			catch (Exception snapEx)
			{
				MelonLogger.Error("Failed to snapshot totalInstalled for flag reset: " + snapEx.Message);
			}
			CrossReferenceInstalledMods();
			SaveSubscriptionCache();
			if (NetworkerMenuController.instance != null)
				NetworkerMenuController.instance.Refresh();
		}
		}
		}
		catch (Exception e)
		{
			MelonLogger.Error("Failed to process subscriptions: " + e);
		}
	}

	public void PopulateInstalledMods(string directory)
	{
		MelonLogger.Msg("DIAG: PopulateInstalledMods start");
		// Load cached subscription data before scanning installed mods
		LoadSubscriptionCache();

		string[] files = Directory.GetFiles(directory, "*.manifest", SearchOption.AllDirectories);
		foreach (string text in files)
		{
			if (!text.EndsWith(".manifest"))
			{
				continue;
			}
			dynamic val = null;
			ModInfo modInfo = new ModInfo();
			try
			{
				val = JsonConvert.DeserializeObject<object>(File.ReadAllText(text));
				if (val == null) throw new Exception("Failed to parse manifest JSON");
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
					var prop = item2 as JProperty;
					if (prop != null && prop.Name.Contains("networker"))
					{
						text2 = prop.Name;
					}
					else if (prop == null)
					{
						// Fallback: try the old toString approach
						string text3 = item2.ToString();
						if (text3.Contains("networker"))
						{
							string[] array = text3.Split("\": {");
							if (array.Length > 0)
								text2 = array[0].Replace("\"", "");
						}
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
				// If pc/android refs failed, try to get numericalId from other sources
				if (value == 0)
				{
					// Try from info string if available
					if (text2 != "")
					{
						try {
							// The numericalId might be in a target ref value
							// Check if we have any target with a valid modId
							foreach (dynamic item2 in val["objects"]["2"]["targets"])
							{
								try
								{
									int refNum = (int)item2["ref"];
									if (refNum != 0)
									{
										// Found a valid ref
										// modId can be read from objects[refNum]["modId"]
										int tryModId = (int)val["objects"][refNum.ToString()]["modId"];
										if (tryModId != 0)
										{
											value = tryModId;
											break;
										}
									}
								}
								catch { }
							}
						}
						catch { }
					}
				}
				if (value != 0)
					modInfo.numericalId = value.ToString();
				// If value is 0, numericalId stays null until PopulateFromInfoString or other source sets it
				modInfo.structureVersion = ModInfo.globalStructureVersion;
				if (text2 != "")
				{
					modInfo.PopulateFromInfoString(text2);
				}
				// Fallback: if modName wasn't set from info string, use modId (title from manifest)
				if (string.IsNullOrEmpty(modInfo.modName))
				{
					modInfo.modName = modId;
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
		MelonLogger.Msg($"[Diag] Scanned {NetworkerMenuController.totalInstalled.Count} installed mods");
		// Re-apply cache data now that totalInstalled is populated
		CrossReferenceInstalledMods();
	}

	
	public static void BackfillManifests()
	{
		MelonLogger.Msg("BackfillManifests: Scanning installed mods for missing .manifest files...");
		if (!Directory.Exists(ModFileManager.MOD_FOLDER_PATH))
			return;
		string[] modDirectories = Directory.GetDirectories(ModFileManager.MOD_FOLDER_PATH);
		int backfilled = 0;
		int skipped = 0;
		int queuedUpdate = 0;
		bool hasSubscriptions = subscribedMods != null && subscribedMods.Count > 0;
		if (hasSubscriptions)
		{
			MelonLogger.Msg("BackfillManifests: " + subscribedMods.Count + " subscriptions available - will check for updates");
		}
		else
		{
			MelonLogger.Msg("BackfillManifests: No subscription data - backfilling manifests only");
		}
		foreach (string modDir in modDirectories)
		{
			try
			{
				string modInfoPath = Path.Combine(modDir, "modinfo.json");
				if (!File.Exists(modInfoPath))
				{
					skipped++;
					continue;
				}
				string modInfoJson = File.ReadAllText(modInfoPath);
				JObject modInfoObj = JObject.Parse(modInfoJson);
				string palletPath = ModFileManager.FindFile(modDir, "pallet.json");
				if (string.IsNullOrEmpty(palletPath))
				{
					skipped++;
					continue;
				}
				string palletJson = File.ReadAllText(palletPath);
				JObject palletObj = JObject.Parse(palletJson);
				string barcode = (string)palletObj["objects"]?["1"]?["barcode"];
				if (string.IsNullOrEmpty(barcode))
				{
					skipped++;
					continue;
				}
				string numericalId = (string)modInfoObj["numericalId"] ?? (string)modInfoObj["id"] ?? "0";
				string installedVersion = (string)modInfoObj["version"] ?? "0.0.0";
				string installedModId = (string)modInfoObj["modId"] ?? "";
				bool needsUpdate = true;
				// STEP 1: Check if subscription exists and needs update
				if (hasSubscriptions && !string.IsNullOrEmpty(numericalId) && numericalId != "0")
				{
					ModInfo matchingSub = null;
					foreach (ModInfo sub in subscribedMods)
					{
						if (sub.numericalId == numericalId)
						{
							matchingSub = sub;
							break;
						}
					}
					if (matchingSub == null && !string.IsNullOrEmpty(installedModId))
					{
						foreach (ModInfo sub in subscribedMods)
						{
							if (sub.modId == installedModId)
							{
								matchingSub = sub;
								break;
							}
						}
					}
					if (matchingSub != null)
					{
						string subVersion = matchingSub.version ?? "0.0.0";
						if (installedVersion != subVersion)
						{
							MelonLogger.Msg("BackfillManifests: Version mismatch for " + installedModId + " (local: " + installedVersion + ", remote: " + subVersion + ") - queuing update");
							ModFileManager.AddToQueue(new DownloadQueueElement
							{
								associatedPlayer = null,
								info = matchingSub,
								notify = false
							});
							queuedUpdate++;
							needsUpdate = false; // Skip manifest write - download will create fresh manifest
						}
						else
						{
							// Version matches - update modinfo.json with fresh subscription data
							MelonLogger.Msg("BackfillManifests: Version match for " + installedModId + " - updating modinfo.json with fresh metadata");
							string freshJson = JsonConvert.SerializeObject((object)matchingSub);
							File.WriteAllText(modInfoPath, freshJson);
							modInfoObj = JObject.Parse(freshJson);
						}
					}
				}
				// STEP 2: Always rewrite manifest unconditionally
				string expectedManifestPath = Path.Combine(modDir, barcode + ".manifest");
				string windowsLink = (string)modInfoObj["windowsDownloadLink"] ?? "";
				string androidLink = (string)modInfoObj["androidDownloadLink"] ?? "";
				string version = (string)modInfoObj["version"] ?? "0.0.0";
				string modId = (string)modInfoObj["modId"] ?? "";
				string modSummary = (string)modInfoObj["modSummary"] ?? "";
				string thumbnailLink = (string)modInfoObj["thumbnailLink"] ?? "";
				string mature = modInfoObj["mature"]?.ToString() ?? "False";
				string temp = modInfoObj["temp"]?.ToString() ?? "False";
				string fileSizeKB = modInfoObj["fileSizeKB"]?.ToString() ?? "0";
				string fileName = modInfoObj["fileName"]?.ToString() ?? "";
				string structureVersion = modInfoObj["structureVersion"]?.ToString() ?? "0";
				string modName = modInfoObj["modName"]?.ToString() ?? "";
				string modNameSafe = modName.Replace(";", "");
				string catalogPath = ModFileManager.FindFile(modDir, "catalog.json");
				long pcModfileId = 0L;
				long androidModfileId = 0L;
				try { if (!string.IsNullOrEmpty(windowsLink) && windowsLink.Contains("/files/")) pcModfileId = long.Parse(windowsLink.Split("/files/")[1].Replace("/download", "")); } catch { }
				try { if (!string.IsNullOrEmpty(androidLink) && androidLink.Contains("/files/")) androidModfileId = long.Parse(androidLink.Split("/files/")[1].Replace("/download", "")); } catch { }
				long numericalIdLong = long.TryParse(numericalId, out long parsedId) ? parsedId : 0L;
				JObject manifest = new JObject();
				JObject objects = new JObject();
            JObject obj1 = new JObject();
            obj1["palletBarcode"] = barcode;
            obj1["palletPath"] = palletPath;
            obj1["catalogPath"] = !string.IsNullOrEmpty(catalogPath) ? catalogPath : "";
            obj1["version"] = "1.0.0";
            obj1["installedDate"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            obj1["updateDate"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            obj1["active"] = true;
            obj1["isa"] = new JObject();
            ((JObject)obj1["isa"])["type"] = "pallet-manifest#0";
            objects["1"] = obj1;
				JObject obj2 = new JObject();
				obj2["barcode"] = barcode;
				obj2["version"] = version ?? "0.0.0";
				obj2["title"] = modId ?? "";
				obj2["description"] = modSummary ?? "";
				obj2["thumbnailUrl"] = thumbnailLink ?? "";
				obj2["author"] = "ModIoModNetworker";
				JObject targets = new JObject();
				JObject pcTarget = new JObject();
				pcTarget["ref"] = "3";
				pcTarget["type"] = "mod-target-modio#0";
				targets["pc"] = pcTarget;
				if (androidModfileId > 0L)
				{
					JObject androidTarget = new JObject();
					androidTarget["ref"] = "4";
					androidTarget["type"] = "mod-target-modio#0";
					targets["android"] = androidTarget;
				}
				else
				{
					JObject androidTarget = new JObject();
					androidTarget["ref"] = "3";
					androidTarget["type"] = "mod-target-modio#0";
					targets["android"] = androidTarget;
				}
				string infoString = "networker;" + mature + ";" + temp + ";" + fileSizeKB + ";" + fileName + ";" + structureVersion + ";" + modNameSafe + ";" + (modInfoObj["tags"] is JArray tagsArray ? tagsArray.Count.ToString() : "0");
				JObject infoTarget = new JObject();
				infoTarget["ref"] = "3";
				infoTarget["type"] = "mod-target-modio#0";
				targets[infoString] = infoTarget;
				obj2["targets"] = targets;
				objects["2"] = obj2;
				JObject obj3 = new JObject();
				obj3["gameId"] = 3809L;
				obj3["modId"] = numericalIdLong;
				obj3["modfileId"] = pcModfileId;
				JObject isa3 = new JObject();
				isa3["type"] = "mod-target-modio#0";
				obj3["isa"] = isa3;
				objects["3"] = obj3;
				if (androidModfileId > 0L && androidModfileId != pcModfileId)
				{
					JObject obj4 = new JObject();
					obj4["gameId"] = 3809L;
					obj4["modId"] = numericalIdLong;
					obj4["modfileId"] = androidModfileId;
					JObject isa4 = new JObject();
					isa4["type"] = "mod-target-modio#0";
					obj4["isa"] = isa4;
					objects["4"] = obj4;
				}
            manifest["version"] = 2;
            manifest["root"] = new JObject();
            manifest["root"]["ref"] = "1";
            manifest["root"]["type"] = "pallet-manifest#0";
            manifest["objects"] = objects;
				string manifestContent = manifest.ToString(Formatting.Indented);
				// Also overwrite top-level manifest (was likely Format A/truncated)
				string topLevelManifestPath = Path.Combine(ModFileManager.MOD_FOLDER_PATH, barcode + ".manifest");
				if (topLevelManifestPath != expectedManifestPath)
				{
					AtomicWriteFile(topLevelManifestPath, manifestContent);
				}
				MelonLogger.Msg("BackfillManifests: Wrote manifest for " + barcode + " (" + modId + ")");
				backfilled++;
			}
			catch (Exception ex)
			{
				MelonLogger.Error("BackfillManifests: Error processing " + modDir + ": " + ex.Message);
				skipped++;
			}
		}
		MelonLogger.Msg("BackfillManifests: Done. Backfilled: " + backfilled + ", Queued update: " + queuedUpdate + ", Skipped/OK: " + skipped);
		// Trigger warehouse reload so fresh manifests are picked up
		if (!string.IsNullOrEmpty(ModFileManager.MOD_FOLDER_PATH))
		{
			warehouseReloadRequested = true;
			if (!warehouseReloadFolders.Contains(ModFileManager.MOD_FOLDER_PATH))
				warehouseReloadFolders.Add(ModFileManager.MOD_FOLDER_PATH);
			MelonLogger.Msg("BackfillManifests: Triggered warehouse reload for " + backfilled + " mods");
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

	private static int SafeInt(dynamic obj, string key, int defaultValue = 0)
	{
		if (obj == null) return defaultValue;
		try { return (int)obj[key]; }
		catch { return defaultValue; }
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
	private static void AtomicWriteFile(string path, string content)
	{
		string tempPath = path + ".tmp";
		File.WriteAllText(tempPath, content);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		File.Move(tempPath, path);
	}

}
