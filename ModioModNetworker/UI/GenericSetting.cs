using UnityEngine;

namespace ModioModNetworker.UI;

public abstract class GenericSetting
{
	public GameObject prefabObject;

	public GameObject spawnedObject;

	public string title;

	public abstract void SpawnPrefab(Transform parent);
}
