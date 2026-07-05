using System.Collections.Generic;
using BoneLib;
using Il2CppSLZ.Marrow;
using Il2CppTMPro;
using LabFusion.Entities;
using LabFusion.Player;
using ModioModNetworker.Utilities;
using UnityEngine;

namespace ModioModNetworker.UI;

public class AvatarDownloadBar
{
	private NetworkPlayer rep;

	private GameObject bar;

	private GameObject fill;

	private Animator animator;

	private RigManager manager;

	private TMP_Text modNameText;

	private TMP_Text percentageText;

	private float zeroPosition = 86f;

	private float completePosition = 0f;

	private bool previouslyStarted = false;

	public static Dictionary<PlayerID, AvatarDownloadBar> bars = new Dictionary<PlayerID, AvatarDownloadBar>();

	public AvatarDownloadBar(NetworkPlayer rep)
	{
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = UnityEngine.Object.Instantiate<GameObject>(NetworkerAssets.avatarDownloadBarPrefab);
		UnityEngine.Object.DontDestroyOnLoad((UnityEngine.Object)(object)val);
		((UnityEngine.Object)val).hideFlags = (HideFlags)32;
		bar = val;
		val.SetActive(false);
		if (bars.TryGetValue(rep.PlayerID, out AvatarDownloadBar value))
		{
			GameObject val2 = value.bar;
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)val2);
			bars.Remove(rep.PlayerID);
		}
		bars.Add(rep.PlayerID, this);
		animator = val.GetComponent<Animator>();
		this.rep = rep;
		manager = rep.RigRefs.RigManager;
		modNameText = ((Component)val.transform.Find("Bar").Find("ModName")).GetComponent<TMP_Text>();
		percentageText = ((Component)val.transform.Find("Bar").Find("Percentage")).GetComponent<TMP_Text>();
		fill = ((Component)val.transform.Find("Bar").Find("Mask").Find("Fill")).gameObject;
		fill.transform.localPosition = new Vector3(zeroPosition, 0f, 0f);
	}

	private float GetBarOffset(RigManager rm)
	{
		float num = 0.3f;
		if ((UnityEngine.Object)(object)rm._avatar != null)
		{
			num *= rm._avatar.height;
		}
		return num;
	}

	public void Update()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (manager != null)
		{
			Transform head = ((Rig)manager.physicsRig).m_head;
			bar.transform.position = head.position + Vector3.up * GetBarOffset(manager);
			bar.transform.forward = -(Player.Head.position - bar.transform.position);
		}
	}

	public void SetModName(string name)
	{
		modNameText.text = name;
	}

	public void Finish()
	{
		animator.SetTrigger("completed");
		previouslyStarted = false;
	}

	public void SetPercentage(float percentage)
	{
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!previouslyStarted)
		{
			Show();
		}
		string text = percentage.ToString("0.0");
		percentageText.text = text + "%";
		float num = zeroPosition - percentage / 100f * (zeroPosition - completePosition);
		RectTransform component = fill.GetComponent<RectTransform>();
		((Transform)component).localPosition = new Vector3(num, 0f, 0f);
	}

	public void Show()
	{
		bar.SetActive(true);
		animator.SetTrigger("reset");
		previouslyStarted = true;
	}
}
