using System;
using Il2CppTMPro;
using ModioModNetworker.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModioModNetworker.UI;

public class CheckboxSetting : GenericSetting
{
	private Button checkBox;

	public bool value;

	public Action<bool> onChecked;

	public CheckboxSetting(string title, bool startingValue, Action<bool> onChecked = null)
	{
		prefabObject = NetworkerAssets.checkboxSettingPrefab;
		base.title = title;
		value = startingValue;
		this.onChecked = onChecked;
	}

	public override void SpawnPrefab(Transform parent)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = UnityEngine.Object.Instantiate<GameObject>(prefabObject);
		checkBox = ((Component)val.transform.Find("Button")).GetComponent<Button>();
		checkBox.onClick.AddListener(new System.Action(() => OnCheckMarkClicked()));
		TMP_Text component = ((Component)val.transform.Find("Title")).GetComponent<TMP_Text>();
		component.text = title;
		val.transform.parent = parent;
		val.transform.localPosition = Vector3.forward;
		val.transform.localRotation = Quaternion.identity;
		val.transform.localScale = Vector3.one;
		spawnedObject = val;
		UpdateDisplay();
	}

	private void OnCheckMarkClicked()
	{
		value = !value;
		UpdateDisplay();
		if (onChecked != null)
		{
			onChecked(value);
		}
	}

	private void UpdateDisplay()
	{
		GameObject gameObject = ((Component)spawnedObject.transform.Find("UnCheckedImage")).gameObject;
		GameObject gameObject2 = ((Component)spawnedObject.transform.Find("CheckedImage")).gameObject;
		if (value)
		{
			gameObject.SetActive(false);
			gameObject2.SetActive(true);
		}
		else
		{
			gameObject.SetActive(true);
			gameObject2.SetActive(false);
		}
	}
}
