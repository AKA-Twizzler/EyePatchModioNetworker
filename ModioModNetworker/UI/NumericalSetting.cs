using System;
using Il2CppTMPro;
using ModioModNetworker.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModioModNetworker.UI;

public class NumericalSetting : GenericSetting
{
	private Button increaseButton;

	private Button decreaseButton;

	public int value;

	public Action<int> onModified;

	public int minValue;

	public int maxValue;

	public int increment;

	public NumericalSetting(string title, int startingValue, int minValue, int maxValue, int increment, Action<int> onModified = null)
	{
		prefabObject = NetworkerAssets.numericalSettingPrefab;
		base.title = title;
		value = startingValue;
		this.onModified = onModified;
		this.minValue = minValue;
		this.maxValue = maxValue;
		this.increment = increment;
	}

	public override void SpawnPrefab(Transform parent)
	{
		GameObject val = UnityEngine.Object.Instantiate<GameObject>(prefabObject);
		increaseButton = ((Component)val.transform.Find("IncreaseArrow").Find("Button")).GetComponent<Button>();
		decreaseButton = ((Component)val.transform.Find("DecreaseArrow").Find("Button")).GetComponent<Button>();
		increaseButton.onClick.AddListener(new System.Action(() => ModifyValue(increase: true)));
		decreaseButton.onClick.AddListener(new System.Action(() => ModifyValue(increase: false)));
		TMP_Text component = ((Component)val.transform.Find("Title")).GetComponent<TMP_Text>();
		component.text = title;
		val.transform.parent = parent;
		val.transform.localPosition = Vector3.forward;
		val.transform.localRotation = Quaternion.identity;
		val.transform.localScale = Vector3.one;
		spawnedObject = val;
		UpdateDisplay();
	}

	private void ModifyValue(bool increase)
	{
		if (increase)
		{
			value += increment;
		}
		else
		{
			value -= increment;
		}
		if (value < minValue)
		{
			value = minValue;
		}
		if (value > maxValue)
		{
			value = maxValue;
		}
		UpdateDisplay();
		if (onModified != null)
		{
			onModified(value);
		}
	}

	private void UpdateDisplay()
	{
		TMP_Text component = ((Component)spawnedObject.transform.Find("NumericalDisplay")).GetComponent<TMP_Text>();
		component.text = value.ToString();
	}
}
