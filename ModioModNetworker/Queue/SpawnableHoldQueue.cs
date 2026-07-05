using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow.Data;
using Il2CppSLZ.Marrow.Pool;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Marrow.Pool;
using LabFusion.Network;

namespace ModioModNetworker.Queue;

public class SpawnableHoldQueue
{
	private static List<SpawnableHoldQueueData> queueDatas = new List<SpawnableHoldQueueData>();

	private static List<SpawnResponseData> spawnResponseDatas = new List<SpawnResponseData>();

	public static void ClearSpawnResponseDatas()
	{
		spawnResponseDatas.Clear();
	}

	public static void HandleAllSpawnResponseDatas()
	{
		foreach (SpawnResponseData spawnResponseData in spawnResponseDatas)
		{
			Handle(spawnResponseData);
		}
		spawnResponseDatas.Clear();
	}

	public static void AddToQueue(SpawnResponseData data)
	{
		spawnResponseDatas.Add(data);
	}

	public static void ClearQueue()
	{
		queueDatas.Clear();
	}

	public static void AddToQueue(SpawnableHoldQueueData data)
	{
		queueDatas.Add(data);
	}

	public static void CheckValid(string barcode)
	{
		List<SpawnableHoldQueueData> toRemove = new List<SpawnableHoldQueueData>();
		foreach (SpawnableHoldQueueData queueData in queueDatas)
		{
			if (queueData.missingBarcode == barcode)
			{
				Handle(queueData._data);
				toRemove.Add(queueData);
			}
		}
		queueDatas.RemoveAll((SpawnableHoldQueueData data) => toRemove.Contains(data));
	}

	private static void Handle(SpawnResponseData data)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		SpawnableCrateReference crateRef = new SpawnableCrateReference(data.SpawnData.Barcode);
		Spawnable val = new Spawnable
		{
			crateRef = crateRef,
			policyData = null
		};
		LocalAssetSpawner.Register(val);
		LocalAssetSpawner.Spawn(val, data.SpawnData.SerializedTransform.position, data.SpawnData.SerializedTransform.rotation, (Action<Poolee>)delegate(Poolee go)
		{
			SpawnResponseMessage.OnSpawnFinished(data, go);
		});
	}
}
