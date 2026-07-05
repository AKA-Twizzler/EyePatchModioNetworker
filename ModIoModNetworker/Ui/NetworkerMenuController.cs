using System;
using System.Collections.Generic;
using BoneLib.BoneMenu;
using Il2CppTMPro;
using LabFusion.Network;
using MelonLoader;
using ModioModNetworker;
using ModioModNetworker.Data;
using ModioModNetworker.UI;
using ModioModNetworker.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModIoModNetworker.Ui;

[RegisterTypeInIl2Cpp]
public class NetworkerMenuController : MonoBehaviour
{
	public NetworkerMenuController(IntPtr intPtr) : base(intPtr) { }
	public enum Panels
	{
		MODIO,
		FILES,
		SETTINGS,
		MULTIPLAYER,
		NONE
	}

	public enum InstalledSort
	{
		INSTALLED,
		SUBSCRIBED,
		BLACKLIST
	}

	private Panels selectedPanel = Panels.NONE;

	private InstalledSort chosenSort = InstalledSort.INSTALLED;

	private GameObject modIoTab;

	private GameObject filesTab;

	private GameObject settingsTab;

	private GameObject multiplayerTab;

	private GameObject modProgressDisplay;

	private GameObject keyboardPopup;

	private Button upArrowButton;

	private Button downArrowButton;

	private TMP_Text typeBarText;

	private GameObject typeBarTextObject;

	private GameObject typeBarEmptyTextObject;

	private GameObject typeBarObject;

	private Transform selector;

	private Transform desired;

	private float speed = 10f;

	private Button modIoTabButton;

	private Button filesTabButton;

	private Button settingsTabButton;

	private Button multiplayerTabButton;

	private GameObject modInfoPopup;

	private int pageNumber = 0;

	private int maxPages = 0;

	private int maxDisplayPerPage = 8;

	private int trendingOffset = 0;

	private bool searching = false;

	private Animator rootAnimator;

	public string lastDownloadedTitle = "nothing";

	public static List<ModInfo> totalInstalled = new List<ModInfo>();

	public static List<ModInfo> modIoRetrieved = new List<ModInfo>();

	public static List<ModInfo> host = new List<ModInfo>();

	private static List<GenericSetting> settings = new List<GenericSetting>();

	private static List<StalledAction> stalledActions = new List<StalledAction>();

	private ModInfo viewedInfo;

	public static NetworkerMenuController instance;

	public static SpotlightOverride spotlightOverride = new SpotlightOverride();

	private void Awake()
	{
		instance = this;
		selector = ((Component)this).transform.Find("Selector");
		modIoTab = ((Component)((Component)this).transform.Find("ModIoTab")).gameObject;
		filesTab = ((Component)((Component)this).transform.Find("FilesTab")).gameObject;
		settingsTab = ((Component)((Component)this).transform.Find("SettingsTab")).gameObject;
		multiplayerTab = ((Component)((Component)this).transform.Find("MultiplayerTab")).gameObject;
		Button component = ((Component)((Component)this).transform.Find("BackButton")).GetComponent<Button>();
		component.onClick.AddListener(new System.Action(() => Menu.OpenPage(Page.Root)));
		Button component2 = ((Component)filesTab.transform.Find("Confirmer").Find("Confirm").Find("Button")).GetComponent<Button>();
		component2.onClick.AddListener(new System.Action(() => {
			foreach (ModInfo item in totalInstalled)
			{
				if (!item.IsSubscribed() && item.IsTracked())
				{
					ModFileManager.UnInstallMainThread(item.numericalId);
				}
			}
		}));
		Button component3 = ((Component)multiplayerTab.transform.Find("Confirmer").Find("Confirm").Find("Button")).GetComponent<Button>();
		component3.onClick.AddListener(new System.Action(() => {
			foreach (ModInfo item2 in host)
			{
				ModFileManager.AddToQueue(new DownloadQueueElement
				{
					associatedPlayer = null,
					info = item2
				});
			}
		}));
		Button component4 = ((Component)modIoTab.transform.Find("RefreshSubscribedButton").Find("Button")).GetComponent<Button>();
		component4.onClick.AddListener(new System.Action(() => MainClass.PopulateSubscriptions()));
		Button component5 = ((Component)modIoTab.transform.Find("TrendingFirstPage").Find("BigBanner").Find("ThumbnailMaskMovedSlight")
			.Find("Button")).GetComponent<Button>();
		component5.onClick.AddListener(new System.Action(() => {
			if (spotlightOverride.manualDisplayId != null)
			{
				TriggerModInfoPopup(show: true, spotlightOverride.downloadedInfo);
			}
			else
			{
				TriggerModInfoPopup(show: true, modIoRetrieved[0]);
			}
		}));
		Button component6 = ((Component)modIoTab.transform.Find("SearchIcon")).GetComponent<Button>();
		component6.onClick.AddListener(new System.Action(() => PopupKeyboard()));
		upArrowButton = ((Component)((Component)this).transform.Find("UpArrow").Find("Button")).GetComponent<Button>();
		downArrowButton = ((Component)((Component)this).transform.Find("DownArrow").Find("Button")).GetComponent<Button>();
		upArrowButton.onClick.AddListener(new System.Action(() => OnArrowPress(up: true)));
		downArrowButton.onClick.AddListener(new System.Action(() => OnArrowPress(up: false)));
		modIoTabButton = ((Component)((Component)this).transform.Find("SelectableTabs").Find("ModIoTab")).GetComponentInChildren<Button>();
		filesTabButton = ((Component)((Component)this).transform.Find("SelectableTabs").Find("FileManagementTab")).GetComponentInChildren<Button>();
		settingsTabButton = ((Component)((Component)this).transform.Find("SelectableTabs").Find("SettingsTab")).GetComponentInChildren<Button>();
		multiplayerTabButton = ((Component)((Component)this).transform.Find("SelectableTabs").Find("MultiplayerTab")).GetComponentInChildren<Button>();
		((Component)modIoTab.transform.Find("BackToTrending").Find("BackArrow").Find("Button")).gameObject.GetComponent<Button>().onClick.AddListener(new System.Action(() => ReturnToTrending()));
		modIoTabButton.onClick.AddListener(new System.Action(() => ChangePanel(Panels.MODIO)));
		filesTabButton.onClick.AddListener(new System.Action(() => ChangePanel(Panels.FILES)));
		settingsTabButton.onClick.AddListener(new System.Action(() => ChangePanel(Panels.SETTINGS)));
		multiplayerTabButton.onClick.AddListener(new System.Action(() => ChangePanel(Panels.MULTIPLAYER)));
		modInfoPopup = ((Component)((Component)this).transform.parent.Find("ModInfoOverlay").Find("ModInfoPopup")).gameObject;
		modProgressDisplay = ((Component)((Component)this).transform.parent.Find("ModInstallingDisplay")).gameObject;
		keyboardPopup = ((Component)((Component)this).transform.parent.Find("KeyboardOverlay")).gameObject;
		typeBarObject = ((Component)keyboardPopup.transform.Find("TypeBar")).gameObject;
		typeBarTextObject = ((Component)typeBarObject.transform.Find("TypedOutText")).gameObject;
		typeBarEmptyTextObject = ((Component)typeBarObject.transform.Find("EmptyTextDisplay")).gameObject;
		typeBarText = typeBarTextObject.GetComponent<TMP_Text>();
		rootAnimator = ((Component)this).GetComponentInParent<Animator>();
		Button componentInChildren = ((Component)modInfoPopup.transform.Find("SubscribeUnselected")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren2 = ((Component)modInfoPopup.transform.Find("SubscribeSelected")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren3 = ((Component)modInfoPopup.transform.Find("BlacklistParted")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren4 = ((Component)modInfoPopup.transform.Find("BlacklistPartedSelected")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren5 = ((Component)modInfoPopup.transform.Find("UninstallParted")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren6 = ((Component)modInfoPopup.transform.Find("UninstallPartedSelected")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren7 = ((Component)modInfoPopup.transform.Find("BlacklistFull")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren8 = ((Component)modInfoPopup.transform.Find("BlacklistSelectedFull")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren9 = ((Component)modInfoPopup.transform.Find("ExitCatch")).gameObject.GetComponentInChildren<Button>();
		Button componentInChildren10 = ((Component)modInfoPopup.transform.Find("ExitCatch (1)")).gameObject.GetComponentInChildren<Button>();
		Button component7 = ((Component)filesTab.transform.Find("InstalledText")).GetComponent<Button>();
		Button component8 = ((Component)filesTab.transform.Find("SubscribedText")).GetComponent<Button>();
		Button component9 = ((Component)filesTab.transform.Find("BlacklistText")).GetComponent<Button>();
		RegisterWholeKeyboard();
		component7.onClick.AddListener(new System.Action(() => SetFilterMode(InstalledSort.INSTALLED)));
		component8.onClick.AddListener(new System.Action(() => SetFilterMode(InstalledSort.SUBSCRIBED)));
		component9.onClick.AddListener(new System.Action(() => SetFilterMode(InstalledSort.BLACKLIST)));
		componentInChildren9.onClick.AddListener(new System.Action(() => TriggerModInfoPopup(show: false, null)));
		componentInChildren10.onClick.AddListener(new System.Action(() => TriggerModInfoPopup(show: false, null)));
		componentInChildren.onClick.AddListener(new System.Action(() => OnSubscribeButtonPressed(selected: false)));
		componentInChildren2.onClick.AddListener(new System.Action(() => OnSubscribeButtonPressed(selected: true)));
		componentInChildren3.onClick.AddListener(new System.Action(() => OnBlacklistButtonPressed(selected: false)));
		componentInChildren4.onClick.AddListener(new System.Action(() => OnBlacklistButtonPressed(selected: true)));
		componentInChildren5.onClick.AddListener(new System.Action(() => OnInstallButtonPressed(selected: false)));
		componentInChildren6.onClick.AddListener(new System.Action(() => OnInstallButtonPressed(selected: true)));
		componentInChildren7.onClick.AddListener(new System.Action(() => OnBlacklistButtonPressed(selected: false)));
		componentInChildren8.onClick.AddListener(new System.Action(() => OnBlacklistButtonPressed(selected: true)));
		ChangePanel(Panels.FILES);
	}

	public void Refresh()
	{
		if (selectedPanel == Panels.FILES)
		{
			SetFilterMode(chosenSort);
		}
	}

	public static void SetHostSubscribedMods(List<ModInfo> modInfos)
	{
		host.Clear();
		host.AddRange(modInfos);
		MainClass.confirmedHostHasIt = true;
	}

	public static void AddCheckboxSetting(string title, bool startingValue, Action<bool> onModified)
	{
		settings.Add(new CheckboxSetting(title, startingValue, onModified));
	}

	public static void AddNumericalSetting(string title, int startingValue, int minValue, int maxValue, int increment, Action<int> onModified)
	{
		settings.Add(new NumericalSetting(title, startingValue, minValue, maxValue, increment, onModified));
	}

	public void OnSubscribeButtonPressed(bool selected)
	{
		if (selected)
		{
			ModFileManager.UnSubscribe(viewedInfo.numericalId);
			if (viewedInfo.IsInstalled())
			{
				ModFileManager.UnInstall(viewedInfo.numericalId);
			}
			MainClass.subscribedModIoNumericalIds.Remove(viewedInfo.numericalId);
			return;
		}
		if (viewedInfo.windowsDownloadLink != "nothing" || viewedInfo.androidDownloadLink != "nothing")
		{
			MainClass.ReceiveSubModInfo(viewedInfo, ignoreTag: true);
		}
		else
		{
			MainClass.PopulateSubscriptions();
		}
		if (!MainClass.subscribedModIoNumericalIds.Contains(viewedInfo.numericalId))
		{
			MainClass.subscribedModIoNumericalIds.Add(viewedInfo.numericalId);
		}
		ModFileManager.Subscribe(viewedInfo.numericalId);
	}

	private void Search(string query)
	{
		searching = true;
		modIoRetrieved.Clear();
		PopulateModIoTab(0);
		ResetPageNumber();
		trendingOffset = 0;
		UpdateArrowDisplays();
		ModFileManager.QueueTrending(0, query);
		rootAnimator.SetTrigger("keyboardpopup");
		SetMainCanvasColliderState(enabled: true);
		((Component)modIoTab.transform.Find("BackToTrending")).gameObject.SetActive(true);
		((Component)modIoTab.transform.Find("BonelabIcon")).gameObject.SetActive(false);
		((Component)modIoTab.transform.Find("SectionText")).GetComponent<TMP_Text>().text = "\"" + query + "\"";
	}

	private void ReturnToTrending()
	{
		searching = false;
		modIoRetrieved.Clear();
		PopulateModIoTab(0);
		ResetPageNumber();
		UpdateArrowDisplays();
		ModFileManager.QueueTrending(0);
		((Component)modIoTab.transform.Find("BackToTrending")).gameObject.SetActive(false);
		((Component)modIoTab.transform.Find("BonelabIcon")).gameObject.SetActive(true);
		((Component)modIoTab.transform.Find("SectionText")).GetComponent<TMP_Text>().text = "TRENDING";
	}

	public void OnBlacklistButtonPressed(bool selected)
	{
		if (selected)
		{
			if (viewedInfo.modId != null)
			{
				MainClass.blacklistedModIoIds.Remove(viewedInfo.modId);
				MainClass.RemoveLineFromBlacklist(viewedInfo.modId);
			}
			MainClass.blacklistedModIoIds.Remove(viewedInfo.numericalId);
			MainClass.RemoveLineFromBlacklist(viewedInfo.numericalId);
		}
		else
		{
			if (viewedInfo.modId != null)
			{
				MainClass.blacklistedModIoIds.Add(viewedInfo.modId);
				MainClass.WriteLineToBlacklist(viewedInfo.modId);
			}
			else
			{
				MainClass.blacklistedModIoIds.Add(viewedInfo.numericalId);
				MainClass.WriteLineToBlacklist(viewedInfo.numericalId);
			}
			if (viewedInfo.IsSubscribed())
			{
				ModFileManager.UnSubscribe(viewedInfo.numericalId);
				if (viewedInfo.IsInstalled())
				{
					ModFileManager.UnInstall(viewedInfo.numericalId);
				}
				MainClass.subscribedModIoNumericalIds.Remove(viewedInfo.numericalId);
			}
		}
		UpdateModPopupButtons();
	}

	public void OnInstallButtonPressed(bool selected)
	{
		if (selected)
		{
			ModFileManager.UnInstall(viewedInfo.numericalId);
			if (viewedInfo.IsSubscribed())
			{
				ModFileManager.UnSubscribe(viewedInfo.numericalId);
			}
			MainClass.subscribedModIoNumericalIds.Remove(viewedInfo.numericalId);
		}
		else if (viewedInfo.windowsDownloadLink != null)
		{
			ModFileManager.AddToQueue(new DownloadQueueElement
			{
				info = viewedInfo,
				associatedPlayer = null,
				notify = true
			});
		}
		else
		{
			ModInfo.RequestModInfoNumerical(viewedInfo.numericalId, "install_native");
		}
	}

	private void ResetPageNumber()
	{
		pageNumber = 0;
		maxDisplayPerPage = 8;
		maxPages = 0;
	}

	public void TriggerModInfoPopup(bool show, ModInfo modInfo)
	{
		rootAnimator = ((Component)this).GetComponentInParent<Animator>();
		rootAnimator.SetTrigger("triggerpopup");
		if (show)
		{
			SetMainCanvasColliderState(enabled: false);
			TMP_Text component = ((Component)modInfoPopup.transform.Find("ModTitle")).GetComponent<TMP_Text>();
			TMP_Text component2 = ((Component)modInfoPopup.transform.Find("Description")).GetComponent<TMP_Text>();
			TMP_Text component3 = ((Component)modInfoPopup.transform.Find("FileSizeDisplay")).GetComponent<TMP_Text>();
			RawImage thumbnail = ((Component)modInfoPopup.transform.Find("Thumbnail")).GetComponent<RawImage>();
			component.text = modInfo.modName;
			component2.text = modInfo.modSummary;
			float fileSizeKB = modInfo.fileSizeKB;
			float num = fileSizeKB / 1000000f;
			float num2 = num / 1000f;
			string value = "KB";
			float num3 = fileSizeKB;
			if (num > 1f)
			{
				num3 = num;
				value = "MB";
			}
			if (num2 > 1f)
			{
				num3 = num2;
				value = "GB";
			}
			num3 = Mathf.Round(num3 * 100f) / 100f;
			component3.text = $"({num3} {value})";
			ThumbnailThreader.DownloadThumbnail(modInfo.thumbnailLink, delegate(Texture texture)
			{
				if (thumbnail != null)
				{
					thumbnail.texture = texture;
				}
			});
			viewedInfo = modInfo;
			UpdateModPopupButtons();
		}
		else
		{
			SetMainCanvasColliderState(enabled: true);
		}
	}

	private void SetMainCanvasColliderState(bool enabled)
	{
		foreach (BoxCollider componentsInChild in ((Component)this).GetComponentsInChildren<BoxCollider>())
		{
			((Collider)componentsInChild).enabled = enabled;
		}
	}

	public void UpdateModPopupButtons()
	{
		if (viewedInfo == null)
		{
			return;
		}
		ModInfo modInfo = viewedInfo;
		GameObject gameObject = ((Component)modInfoPopup.transform.Find("SubscribeUnselected")).gameObject;
		GameObject gameObject2 = ((Component)modInfoPopup.transform.Find("SubscribeSelected")).gameObject;
		GameObject gameObject3 = ((Component)modInfoPopup.transform.Find("BlacklistParted")).gameObject;
		GameObject gameObject4 = ((Component)modInfoPopup.transform.Find("BlacklistPartedSelected")).gameObject;
		GameObject gameObject5 = ((Component)modInfoPopup.transform.Find("UninstallParted")).gameObject;
		GameObject gameObject6 = ((Component)modInfoPopup.transform.Find("UninstallPartedSelected")).gameObject;
		GameObject gameObject7 = ((Component)modInfoPopup.transform.Find("BlacklistFull")).gameObject;
		GameObject gameObject8 = ((Component)modInfoPopup.transform.Find("BlacklistSelectedFull")).gameObject;
		gameObject.SetActive(false);
		gameObject2.SetActive(false);
		gameObject3.SetActive(false);
		gameObject4.SetActive(false);
		gameObject5.SetActive(false);
		gameObject6.SetActive(false);
		gameObject7.SetActive(false);
		gameObject8.SetActive(false);
		bool flag = modInfo.IsInstalled();
		if (modInfo.IsSubscribed())
		{
			gameObject.SetActive(false);
			gameObject2.SetActive(true);
		}
		else
		{
			gameObject.SetActive(true);
			gameObject2.SetActive(false);
		}
		bool flag2 = false;
		if (flag)
		{
			flag2 = true;
			gameObject6.SetActive(true);
			gameObject5.SetActive(false);
		}
		if (flag2)
		{
			gameObject8.SetActive(false);
			gameObject7.SetActive(false);
			if (modInfo.IsBlacklisted())
			{
				gameObject4.SetActive(true);
				gameObject3.SetActive(false);
			}
			else
			{
				gameObject4.SetActive(false);
				gameObject3.SetActive(true);
			}
		}
		else if (modInfo.IsBlacklisted())
		{
			gameObject8.SetActive(true);
			gameObject7.SetActive(false);
		}
		else
		{
			gameObject8.SetActive(false);
			gameObject7.SetActive(true);
		}
	}

	public void SetFilterMode(InstalledSort installedSort)
	{
		chosenSort = installedSort;
		ResetPageNumber();
		switch (installedSort)
		{
		case InstalledSort.INSTALLED:
			((Component)filesTab.transform.Find("GridLayout")).gameObject.SetActive(true);
			((Component)filesTab.transform.Find("ListLayout")).gameObject.SetActive(false);
			((Component)filesTab.transform.Find("UninstallUnsubscribedModsButton")).gameObject.SetActive(true);
			maxPages = (int)Math.Ceiling((double)totalInstalled.Count / (double)maxDisplayPerPage);
			UpdateArrowDisplays();
			PopulateFiles(pageNumber);
			break;
		case InstalledSort.SUBSCRIBED:
		{
			((Component)filesTab.transform.Find("GridLayout")).gameObject.SetActive(true);
			((Component)filesTab.transform.Find("ListLayout")).gameObject.SetActive(false);
			((Component)filesTab.transform.Find("UninstallUnsubscribedModsButton")).gameObject.SetActive(false);
			List<ModInfo> list = new List<ModInfo>();
			foreach (ModInfo item in totalInstalled)
			{
				if (item.IsSubscribed())
				{
					list.Add(item);
				}
			}
			maxPages = (int)Math.Ceiling((double)list.Count / (double)maxDisplayPerPage);
			UpdateArrowDisplays();
			PopulateFiles(pageNumber);
			break;
		}
		case InstalledSort.BLACKLIST:
			((Component)filesTab.transform.Find("GridLayout")).gameObject.SetActive(false);
			((Component)filesTab.transform.Find("ListLayout")).gameObject.SetActive(true);
			((Component)filesTab.transform.Find("UninstallUnsubscribedModsButton")).gameObject.SetActive(false);
			maxPages = (int)Math.Ceiling((double)MainClass.blacklistedModIoIds.Count / 4.0);
			UpdateArrowDisplays();
			PopulateBlacklist(pageNumber);
			break;
		}
	}

	public void PopulateBlacklist(int page)
	{
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		GameObject gameObject = ((Component)filesTab.transform.Find("ListLayout")).gameObject;
		int childCount = gameObject.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = gameObject.transform.GetChild(i);
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)child).gameObject);
		}
		int num = 4 * page;
		for (int j = 0; j < 4; j++)
		{
			if (MainClass.blacklistedModIoIds.Count > num + j)
			{
				string original = MainClass.blacklistedModIoIds[num + j];
				string text = original;
				if (int.TryParse(text, out var result))
				{
					text = "Numeric Listing: " + result;
				}
				GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.blacklistDisplayPrefab);
				TMP_Text component = ((Component)val.transform.Find("BlacklistedMod")).gameObject.GetComponent<TMP_Text>();
				component.text = text;
				Button component2 = ((Component)val.transform.Find("XButton").Find("Button")).gameObject.GetComponent<Button>();
				component2.onClick.AddListener(new System.Action(() => {
					MainClass.blacklistedModIoIds.Remove(original);
					maxPages = (int)Math.Ceiling((double)MainClass.blacklistedModIoIds.Count / 4.0);
					PopulateBlacklist(pageNumber);
					MainClass.RemoveLineFromBlacklist(original);
				}));
				val.transform.parent = gameObject.transform;
				val.transform.localPosition = Vector3.forward;
				val.transform.localRotation = Quaternion.identity;
				val.transform.localScale = Vector3.one;
			}
		}
	}

	public void OnNewTrendingRecieved()
	{
		if (selectedPanel == Panels.MODIO)
		{
			if (maxPages >= 1)
			{
				PopulateModIoTab(maxPages - 1);
			}
			else
			{
				PopulateModIoTab(0);
			}
			maxPages = (int)Math.Ceiling((double)modIoRetrieved.Count / (double)maxDisplayPerPage);
			UpdateArrowDisplays();
			SetMainCanvasColliderState(enabled: true);
		}
	}

	private void PopulateModIoTab(int page)
	{
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		if (page > 0 || searching)
		{
			GameObject gameObject = ((Component)modIoTab.transform.Find("GridLayout")).gameObject;
			GameObject gameObject2 = ((Component)modIoTab.transform.Find("TrendingFirstPage")).gameObject;
			gameObject2.SetActive(false);
			gameObject.SetActive(true);
			ClearAllChildren(gameObject.transform);
			int num = maxDisplayPerPage * page;
			if (!searching)
			{
				if (num > 0)
				{
					num -= 2;
				}
				if (spotlightOverride.manualDisplayId != null)
				{
					num--;
				}
			}
			for (int i = 0; i < maxDisplayPerPage; i++)
			{
				if (modIoRetrieved.Count > num + i)
				{
					ModInfo modInfo = modIoRetrieved[num + i];
					MakeModInfoObject(gameObject.transform, modInfo);
				}
			}
			return;
		}
		GameObject gameObject3 = ((Component)modIoTab.transform.Find("GridLayout")).gameObject;
		Transform val = modIoTab.transform.Find("TrendingFirstPage");
		GameObject gameObject4 = ((Component)val).gameObject;
		Transform val2 = val.Find("BigBanner");
		gameObject4.SetActive(true);
		gameObject3.SetActive(false);
		Transform parent = val.Find("GridLayoutTrending");
		Transform val3 = val.Find("ModInfoSpawnPoint");
		ClearAllChildren(parent);
		ClearAllChildren(val3);
		int num2 = 1;
		if (spotlightOverride.manualDisplayId != null)
		{
			num2--;
		}
		if (modIoRetrieved.Count == 0)
		{
			((Component)val2).gameObject.SetActive(false);
			return;
		}
		((Component)val2).gameObject.SetActive(true);
		GameObject val4 = MakeModInfoObject(val3, modIoRetrieved[num2]);
		RectTransform component = val4.GetComponent<RectTransform>();
		RectTransform component2 = ((Component)val3).GetComponent<RectTransform>();
		((Transform)component).position = ((Transform)component2).position;
		num2++;
		for (int j = 0; j < 4; j++)
		{
			if (modIoRetrieved.Count > num2 + j)
			{
				ModInfo modInfo2 = modIoRetrieved[num2 + j];
				MakeModInfoObject(parent, modInfo2);
			}
		}
		TMP_Text component3 = ((Component)val2.Find("ModTitleText")).GetComponent<TMP_Text>();
		TMP_Text component4 = ((Component)val2.Find("Description")).GetComponent<TMP_Text>();
		Transform val5 = val2.Find("ThumbnailMaskMovedSlight");
		RawImage thumbNail = ((Component)val5.Find("LargeThumbnail")).GetComponent<RawImage>();
		ModInfo modInfo3 = modIoRetrieved[0];
		if (spotlightOverride.downloadedInfo != null)
		{
			modInfo3 = spotlightOverride.downloadedInfo;
		}
		ThumbnailThreader.DownloadThumbnail(modInfo3.thumbnailLink, delegate(Texture texture)
		{
			if (thumbNail != null)
			{
				thumbNail.texture = texture;
			}
			spotlightOverride.cachedThumbnail = texture;
		});
		GameObject gameObject5 = ((Component)((Component)val5).transform.Find("BottomBar")).gameObject;
		if (spotlightOverride.subTitle != null)
		{
			gameObject5.SetActive(true);
			TMP_Text component5 = ((Component)gameObject5.transform.Find("OptionalTitle")).GetComponent<TMP_Text>();
			component5.text = spotlightOverride.subTitle;
		}
		else
		{
			gameObject5.SetActive(false);
		}
		if (spotlightOverride.titleOverride != null)
		{
			component3.text = spotlightOverride.titleOverride;
		}
		else
		{
			component3.text = modInfo3.modName;
		}
		if (spotlightOverride.descriptionOverride != null)
		{
			component4.text = spotlightOverride.descriptionOverride;
		}
		else
		{
			component4.text = modInfo3.modSummary;
		}
	}

	private void ClearAllChildren(Transform parent)
	{
		int childCount = parent.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = parent.GetChild(i);
			ModInfoDisplay componentInChildren = ((Component)child).GetComponentInChildren<ModInfoDisplay>();
			if (componentInChildren != null)
			{
				componentInChildren.DestroyThumbnail();
			}
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)child).gameObject);
		}
	}

	private GameObject MakeModInfoObject(Transform parent, ModInfo modInfo, bool zeroPosition = true)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.modInfoDisplay);
		ModInfoDisplay modInfoDisplay = val.AddComponent<ModInfoDisplay>();
		modInfoDisplay.SetModInfo(modInfo);
		modInfoDisplay.controller = this;
		val.transform.parent = ((Component)parent).transform;
		if (zeroPosition)
		{
			val.transform.localPosition = Vector3.forward;
			val.transform.localRotation = Quaternion.identity;
		}
		val.transform.localScale = Vector3.one;
		return val;
	}

	private void PopulateFiles(int page)
	{
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		GameObject gameObject = ((Component)filesTab.transform.Find("GridLayout")).gameObject;
		int childCount = gameObject.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = gameObject.transform.GetChild(i);
			ModInfoDisplay componentInChildren = ((Component)child).GetComponentInChildren<ModInfoDisplay>();
			if (componentInChildren != null)
			{
				componentInChildren.DestroyThumbnail();
			}
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)child).gameObject);
		}
		int num = 0;
		int num2 = 0;
		int num3 = page * maxDisplayPerPage;
		foreach (ModInfo item in totalInstalled)
		{
			num2++;
			if (num2 >= num3 && (chosenSort != InstalledSort.SUBSCRIBED || item.IsSubscribed()))
			{
				GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.modInfoDisplay);
				ModInfoDisplay modInfoDisplay = val.AddComponent<ModInfoDisplay>();
				modInfoDisplay.SetModInfo(item);
				modInfoDisplay.controller = this;
				val.transform.parent = gameObject.transform;
				val.transform.localPosition = Vector3.forward;
				val.transform.localRotation = Quaternion.identity;
				val.transform.localScale = Vector3.one;
				num++;
				if (num >= maxDisplayPerPage)
				{
					break;
				}
			}
		}
	}

	private void PopulateHostMods(int page)
	{
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		GameObject gameObject = ((Component)multiplayerTab.transform.Find("GridLayout")).gameObject;
		int childCount = gameObject.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = gameObject.transform.GetChild(i);
			ModInfoDisplay componentInChildren = ((Component)child).GetComponentInChildren<ModInfoDisplay>();
			if (componentInChildren != null)
			{
				componentInChildren.DestroyThumbnail();
			}
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)child).gameObject);
		}
		int num = 0;
		int num2 = 0;
		int num3 = page * maxDisplayPerPage;
		foreach (ModInfo item in host)
		{
			num2++;
			if (num2 >= num3)
			{
				GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.modInfoDisplay);
				ModInfoDisplay modInfoDisplay = val.AddComponent<ModInfoDisplay>();
				modInfoDisplay.SetModInfo(item);
				modInfoDisplay.controller = this;
				val.transform.parent = gameObject.transform;
				val.transform.localPosition = Vector3.forward;
				val.transform.localRotation = Quaternion.identity;
				val.transform.localScale = Vector3.one;
				num++;
				if (num >= maxDisplayPerPage)
				{
					break;
				}
			}
		}
	}

	private void PopulateSettings(int page)
	{
		GameObject gameObject = ((Component)settingsTab.transform.Find("SettingsHolder")).gameObject;
		int childCount = gameObject.transform.childCount;
		for (int i = 0; i < childCount; i++)
		{
			Transform child = gameObject.transform.GetChild(i);
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)child).gameObject);
		}
		int num = 3 * page;
		for (int j = 0; j < 3; j++)
		{
			if (settings.Count > num + j)
			{
				GenericSetting genericSetting = settings[num + j];
				genericSetting.SpawnPrefab(gameObject.transform);
			}
		}
	}

	private void ChangePanel(Panels panel)
	{
		ResetPageNumber();
		selectedPanel = panel;
		switch (panel)
		{
		case Panels.FILES:
			SetSelectorDesired(((Component)filesTabButton).transform.parent.Find("SelectorDesiredPos"));
			SetFilterMode(chosenSort);
			break;
		case Panels.MODIO:
			maxPages = (int)Math.Ceiling((double)modIoRetrieved.Count / (double)maxDisplayPerPage);
			UpdateArrowDisplays();
			SetSelectorDesired(((Component)modIoTabButton).transform.parent.Find("SelectorDesiredPos"));
			PopulateModIoTab(pageNumber);
			break;
		case Panels.SETTINGS:
			maxPages = (int)Math.Ceiling((double)settings.Count / 3.0);
			UpdateArrowDisplays();
			SetSelectorDesired(((Component)settingsTabButton).transform.parent.Find("SelectorDesiredPos"));
			PopulateSettings(pageNumber);
			break;
		case Panels.MULTIPLAYER:
		{
			maxPages = (int)Math.Ceiling((double)host.Count / (double)maxDisplayPerPage);
			UpdateArrowDisplays();
			SetSelectorDesired(((Component)multiplayerTabButton).transform.parent.Find("SelectorDesiredPos"));
			PopulateHostMods(pageNumber);
			TMP_Text component = ((Component)multiplayerTab.transform.Find("InstallAllHostModsButton").Find("Text (TMP)")).GetComponent<TMP_Text>();
			float num = 0f;
			int num2 = 0;
			foreach (ModInfo item in host)
			{
				if (!item.IsInstalled())
				{
					num2++;
					num += item.fileSizeKB;
				}
			}
			float num3 = num;
			float num4 = num3 / 1000000f;
			float num5 = num4 / 1000f;
			string value = "KB";
			float num6 = num3;
			if (num4 > 1f)
			{
				num6 = num4;
				value = "MB";
			}
			if (num5 > 1f)
			{
				num6 = num5;
				value = "GB";
			}
			num6 = Mathf.Round(num6 * 100f) / 100f;
			component.text = $"Install Host Mods ({num2}) ({num6} {value})";
			break;
		}
		}
	}

	private void UpdateArrowDisplays()
	{
		GameObject gameObject = ((Component)((Component)upArrowButton).transform.parent).gameObject;
		GameObject gameObject2 = ((Component)((Component)downArrowButton).transform.parent).gameObject;
		gameObject.SetActive(pageNumber > 0);
		gameObject2.SetActive(pageNumber < maxPages - 1);
		if (selectedPanel == Panels.MODIO && modIoRetrieved.Count > 80)
		{
			gameObject2.SetActive(true);
		}
	}

	private void OnArrowPress(bool up)
	{
		if (!up)
		{
			pageNumber++;
		}
		else
		{
			pageNumber--;
		}
		if (pageNumber < 0)
		{
			pageNumber = 0;
		}
		if (selectedPanel == Panels.MODIO && pageNumber == maxPages - 1 && modIoRetrieved.Count > 80)
		{
			trendingOffset++;
			if (searching)
			{
				ModFileManager.QueueTrending(trendingOffset * 100, KeyboardManager.typed);
			}
			else
			{
				ModFileManager.QueueTrending(trendingOffset * 100);
			}
		}
		if (pageNumber > maxPages)
		{
			pageNumber = maxPages;
		}
		if (selectedPanel == Panels.MULTIPLAYER)
		{
			PopulateHostMods(pageNumber);
		}
		if (selectedPanel == Panels.FILES)
		{
			if (chosenSort != InstalledSort.BLACKLIST)
			{
				PopulateFiles(pageNumber);
			}
			else
			{
				PopulateBlacklist(pageNumber);
			}
		}
		if (selectedPanel == Panels.MODIO)
		{
			PopulateModIoTab(pageNumber);
		}
		if (selectedPanel == Panels.SETTINGS)
		{
			PopulateSettings(pageNumber);
		}
		UpdateArrowDisplays();
	}

	private void RegisterWholeKeyboard()
	{
		RegisterKey("Q");
		RegisterKey("W");
		RegisterKey("E");
		RegisterKey("R");
		RegisterKey("T");
		RegisterKey("Y");
		RegisterKey("U");
		RegisterKey("I");
		RegisterKey("O");
		RegisterKey("P");
		RegisterKey("A");
		RegisterKey("S");
		RegisterKey("D");
		RegisterKey("F");
		RegisterKey("G");
		RegisterKey("H");
		RegisterKey("J");
		RegisterKey("K");
		RegisterKey("L");
		RegisterKey("Z");
		RegisterKey("X");
		RegisterKey("C");
		RegisterKey("V");
		RegisterKey("B");
		RegisterKey("N");
		RegisterKey("M");
		RegisterKey("1");
		RegisterKey("2");
		RegisterKey("3");
		RegisterKey("4");
		RegisterKey("5");
		RegisterKey("6");
		RegisterKey("7");
		RegisterKey("8");
		RegisterKey("9");
		RegisterKey("0");
		RegisterKey(".");
		RegisterKey(",");
		RegisterKey("'");
		RegisterKey("-");
		RegisterKey("=");
		SetKeyAction("Backspace", delegate
		{
			KeyboardManager.Backspace();
		});
		SetKeyAction("Space", delegate
		{
			KeyboardManager.Append(" ");
		});
		SetKeyAction("Enter", delegate
		{
			Search(KeyboardManager.typed);
		});
		SetKeyAction("Exit", delegate
		{
			rootAnimator.SetTrigger("keyboardpopup");
			SetMainCanvasColliderState(enabled: true);
		});
	}

	public void Reset()
	{
		((Component)((Component)this).transform.parent.Find("ModInfoOverlay")).gameObject.SetActive(false);
		keyboardPopup.gameObject.SetActive(false);
		((Component)this).GetComponent<CanvasGroup>().interactable = true;
		SetMainCanvasColliderState(enabled: true);
	}

	private void RegisterKey(string keyName)
	{
		SetKeyAction(keyName, delegate
		{
			KeyboardManager.Append(keyName);
		});
	}

	private void PopupKeyboard()
	{
		rootAnimator.SetTrigger("keyboardpopup");
		SetMainCanvasColliderState(enabled: false);
	}

	private void SetKeyAction(string keyName, Action action)
	{
		GameObject gameObject = ((Component)keyboardPopup.transform.Find("Keyboard").Find(keyName)).gameObject;
		Button component = gameObject.GetComponent<Button>();
		component.onClick.AddListener(new System.Action(() => action()));
	}

	private void Update()
	{
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		List<StalledAction> toRemove = new List<StalledAction>();
		foreach (StalledAction stalledAction in stalledActions)
		{
			stalledAction.frameCount--;
			if (stalledAction.frameCount == 0)
			{
				stalledAction.action();
				toRemove.Add(stalledAction);
			}
		}
		stalledActions.RemoveAll((StalledAction stall) => toRemove.Contains(stall));
		if ((UnityEngine.Object)(object)desired != (UnityEngine.Object)null && (UnityEngine.Object)(object)selector != (UnityEngine.Object)null)
		{
			selector.position = Vector3.Lerp(selector.position, desired.position, speed * Time.deltaTime);
		}
		if (ModFileManager.activeDownloadQueueElement != null)
		{
			modProgressDisplay.SetActive(true);
			RawImage thumbnail = ((Component)modProgressDisplay.transform.Find("Thumbnail")).gameObject.GetComponent<RawImage>();
			if (lastDownloadedTitle != ModFileManager.activeDownloadQueueElement.info.modName)
			{
				lastDownloadedTitle = ModFileManager.activeDownloadQueueElement.info.modName;
				ThumbnailThreader.DownloadThumbnail(ModFileManager.activeDownloadQueueElement.info.thumbnailLink, delegate(Texture thumb)
				{
					thumbnail.texture = thumb;
				});
			}
			TMP_Text component = ((Component)modProgressDisplay.transform.Find("Title")).gameObject.GetComponent<TMP_Text>();
			component.text = ModFileManager.activeDownloadQueueElement.info.modName;
			TMP_Text component2 = ((Component)modProgressDisplay.transform.Find("Percentage")).gameObject.GetComponent<TMP_Text>();
			component2.text = (int)Math.Round(ModlistMenu.activeDownloadModInfo.modDownloadPercentage) + "%";
		}
		else
		{
			lastDownloadedTitle = "nothing";
			modProgressDisplay.SetActive(false);
		}
		if (multiplayerTabButton != null)
		{
			((Component)((Component)multiplayerTabButton).transform.parent).gameObject.SetActive(NetworkInfo.HasServer);
		}
		if (keyboardPopup != null)
		{
			typeBarText.text = KeyboardManager.typed;
			if (KeyboardManager.typed == "")
			{
				typeBarTextObject.SetActive(false);
				typeBarEmptyTextObject.SetActive(true);
			}
			else
			{
				typeBarTextObject.SetActive(true);
				typeBarEmptyTextObject.SetActive(false);
			}
		}
	}

	public void SetSelectorDesired(Transform transform)
	{
		desired = transform;
	}
}
