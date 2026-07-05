using UnityEngine;

namespace ModioModNetworker.Utilities;

public class NetworkerAssets
{
	public static GameObject avatarDownloadBarPrefab;

	public static GameObject uiMenuPrefab;

	public static GameObject modInfoDisplay;

	public static GameObject blacklistDisplayPrefab;

	public static GameObject checkboxSettingPrefab;

	public static GameObject numericalSettingPrefab;

	public static void LoadAssetsUI(AssetBundle bundle)
	{
		avatarDownloadBarPrefab = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/avatarprogressbar.prefab");
		uiMenuPrefab = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/rootmenu.prefab");
		modInfoDisplay = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/modinfodisplay.prefab");
		blacklistDisplayPrefab = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/blacklistelement.prefab");
		checkboxSettingPrefab = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/checkboxelement.prefab");
		numericalSettingPrefab = bundle.LoadPersistentAsset<GameObject>("assets/networkerassets/numberelement.prefab");
	}
}
