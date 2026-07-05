using System.Collections.Generic;
using BoneLib.BoneMenu;
using BoneLib.BoneMenu.UI;
using HarmonyLib;
using ModioModNetworker.UI;
using ModioModNetworker.Data;
using ModioModNetworker.Utilities;
using UnityEngine;

namespace ModioModNetworker;

public class ModlistMenu
{
	[HarmonyPatch(typeof(GUIMenu), "OnPageOpened")]
	public class CategoryUpdatePatch
	{
		public static void Postfix(GUIMenu __instance, Page page)
		{
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			if (page == mainCategory)
			{
				if (!customMenuObject != null)
				{
					GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.uiMenuPrefab);
					((Component)val.transform.Find("MenuBase")).gameObject.AddComponent<NetworkerMenuController>();
					val.transform.parent = ((Component)__instance).gameObject.transform;
					val.transform.localPosition = Vector3.forward;
					val.transform.localRotation = Quaternion.identity;
					val.transform.localScale = Vector3.one;
					customMenuObject = val;
				}
				else
				{
					customMenuObject.SetActive(true);
				}
			}
			else if (page == Page.Root && customMenuObject != null)
			{
				customMenuObject.SetActive(false);
				NetworkerMenuController.instance.Reset();
			}
		}
	}

	public static Page mainCategory;

	private static string lastSelectedCategory;

	public static List<ModInfo> _modInfos = new List<ModInfo>();

	public static int installPage = 0;

	public static int hostPage = 0;

	private static int installPageCount = 0;

	private static int hostPageCount = 0;

	private static int modsPerPage = 4;

	public static ModInfo activeDownloadModInfo;

	public static GameObject customMenuObject;

	public static void Initialize()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		mainCategory = Page.Root.CreatePage("ModIo Mod Networker", Color.cyan, 0, true);
		NetworkerMenuController.AddCheckboxSetting("Override Fusion DL", MainClass.overrideFusionDL, delegate(bool b)
		{
			MainClass.overrideFusionDL = b;
			MainClass.overrideFusionDLConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddCheckboxSetting("Auto Delete Lobby Mods", MainClass.tempLobbyMods, delegate(bool b)
		{
			MainClass.tempLobbyMods = b;
			MainClass.tempLobbyModsConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddCheckboxSetting("Auto Download Avatars", MainClass.autoDownloadAvatars, delegate(bool b)
		{
			MainClass.autoDownloadAvatars = b;
			MainClass.autoDownloadAvatarsConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddCheckboxSetting("Auto Download Spawnables", MainClass.autoDownloadSpawnables, delegate(bool b)
		{
			MainClass.autoDownloadSpawnables = b;
			MainClass.autoDownloadSpawnablesConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddCheckboxSetting("Auto Download Levels", MainClass.autoDownloadLevels, delegate(bool b)
		{
			MainClass.autoDownloadLevels = b;
			MainClass.autoDownloadLevelsConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddNumericalSetting("(Spawnable/Avatar) Auto Download Max MB", (int)MainClass.maxAutoDownloadMb, 200, 2000, 100, delegate(int num)
		{
			MainClass.maxAutoDownloadMb = num;
			MainClass.maxAutoDownloadMbConfig.Value = num;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddNumericalSetting("(Level) Auto Download Max GB", (int)MainClass.levelMaxGb, 1, 10, 1, delegate(int num)
		{
			MainClass.levelMaxGb = num;
			MainClass.maxLevelAutoDownloadGbConfig.Value = num;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		NetworkerMenuController.AddCheckboxSetting("Download Mature Content", MainClass.downloadMatureContent, delegate(bool b)
		{
			MainClass.downloadMatureContent = b;
			MainClass.downloadMatureContentConfig.Value = b;
			MainClass.melonPreferencesCategory.SaveToFile(true);
		});
		MainClass.menuRefreshRequested = true;
	}

	public static void PopulateModInfos(List<ModInfo> modInfos)
	{
		hostPage = 0;
		_modInfos.Clear();
		_modInfos.AddRange(modInfos);
		MainClass.confirmedHostHasIt = true;
		Refresh(openMenu: false);
	}

	public static void Clear()
	{
		_modInfos.Clear();
		Refresh(openMenu: false);
	}

	public static void Refresh(bool openMenu)
	{
		installPageCount = Mathf.CeilToInt((float)MainClass.installedMods.Count / (float)modsPerPage);
		hostPageCount = Mathf.CeilToInt((float)_modInfos.Count / (float)modsPerPage);
	}

	private static void CreateSettingsSection()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Color green = Color.green;
		if (!MainClass.autoDownloadAvatars)
		{
			green = Color.yellow;
		}
		Color green2 = Color.green;
		if (!MainClass.autoDownloadSpawnables)
		{
			green2 = Color.yellow;
		}
		Color green3 = Color.green;
		if (!MainClass.autoDownloadLevels)
		{
			green3 = Color.yellow;
		}
	}
}
