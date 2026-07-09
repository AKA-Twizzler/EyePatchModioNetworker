using System;
using Il2CppTMPro;
using MelonLoader;
using ModioModNetworker.Data;
using ModioModNetworker.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Il2CppInterop.Runtime.Attributes;

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
		title.text = modInfo.modName;
		if (!modInfo.IsSubscribed())
		{
			subscriptionButton.SetActive(false);
		}
		else
		{
			subscriptionButton.SetActive(true);
		}
		ThumbnailThreader.DownloadThumbnail(modInfo.thumbnailLink, delegate(Texture texture)
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
