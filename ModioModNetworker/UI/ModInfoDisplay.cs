using System;
using Il2CppInterop.Runtime.Attributes;
using Il2CppTMPro;
using MelonLoader;
using ModioModNetworker.Data;
using ModioModNetworker.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModioModNetworker.UI;

[RegisterTypeInIl2Cpp]
public class ModInfoDisplay : MonoBehaviour
{
	public ModInfoDisplay(IntPtr intPtr) : base(intPtr) { }
	public TMP_Text title;

	private RawImage thumbnailImage;

	public RawImage borderImage;

	public ModInfo modInfo;

	public Button button;

	public NetworkerMenuController controller;

	public GameObject subscriptionButton;

	private bool hasAddedThumbnail = false;

	public void Awake()
	{
		thumbnailImage = ((Component)((Component)this).transform.Find("Thumbnail")).GetComponent<RawImage>();
		title = ((Component)((Component)this).transform.Find("Text (TMP)")).GetComponent<TMP_Text>();
		borderImage = ((Component)((Component)this).transform.Find("BaseOverlay")).GetComponent<RawImage>();
		button = ((Component)((Component)this).transform.Find("Button")).GetComponent<Button>();
		subscriptionButton = ((Component)((Component)this).transform.Find("SubscribedIndicator")).gameObject;
		button.onClick.AddListener(new System.Action(() => OnModInfoPressed()));
	}

	public void OnModInfoPressed()
	{
		controller.TriggerModInfoPopup(show: true, modInfo);
	}

	[HideFromIl2Cpp]
	public void SetModInfo(ModInfo modInfo)
	{
		this.modInfo = modInfo;
		MelonLogger.Msg($"[Diag] SetModInfo called - modInfo is NULL? {modInfo == null}");
		if (modInfo != null)
		{
			MelonLogger.Msg($"[Diag] SetModInfo - modName={modInfo.modName ?? "null"} fileSizeKB={modInfo.fileSizeKB} numericalId={modInfo.numericalId ?? "null"} thumbnailLink={(modInfo.thumbnailLink != null && modInfo.thumbnailLink.Length > 0 ? "SET" : "null")} subscribed={modInfo.IsSubscribed()}");
		}
		if (modInfo == null || modInfo.modName == null)
		{
			MelonLogger.Error($"[Diag] SetModInfo: modInfo or modName is NULL! Display will be broken!");
			if (modInfo != null && modInfo.modId != null)
			{
				MelonLogger.Msg($"[Diag] SetModInfo: falling back to modId={modInfo.modId}");
			}
		}
		title.text = modInfo?.modName ?? modInfo?.modId ?? "Unknown";
		if (modInfo == null || !modInfo.IsSubscribed())
		{
			subscriptionButton.SetActive(false);
		}
		else
		{
			subscriptionButton.SetActive(true);
		}
		ThumbnailThreader.DownloadThumbnail(modInfo?.thumbnailLink, delegate(Texture texture)
		{
			if (thumbnailImage != null)
			{
				thumbnailImage.texture = texture;
				hasAddedThumbnail = true;
			}
		});
	}

	public void DestroyThumbnail()
	{
		if (hasAddedThumbnail)
		{
			UnityEngine.Object.DestroyImmediate((UnityEngine.Object)(object)thumbnailImage.texture);
		}
	}
}
