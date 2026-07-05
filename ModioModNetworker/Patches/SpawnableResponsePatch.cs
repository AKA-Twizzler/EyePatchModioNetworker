using System;
using HarmonyLib;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Marrow;
using LabFusion.Network;
using ModioModNetworker.Queue;

namespace ModioModNetworker.Patches;

public class SpawnableResponsePatch
{
	[HarmonyPatch(typeof(SpawnResponseMessage), "OnHandleMessage", new Type[] { typeof(ReceivedMessage) })]
	public static class PatchClass
	{
		public static bool Prefix(ReceivedMessage received)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
			if (!received.IsServerHandled && MainClass.autoDownloadSpawnables)
			{
				SpawnResponseData val = received.ReadData<SpawnResponseData>();
				if (!MainClass.overrideFusionDL)
				{
					return true;
				}
				if (!CrateFilterer.HasCrate<GameObjectCrate>(new Barcode(val.SpawnData.Barcode)))
				{
					SpawnableHoldQueue.AddToQueue(new SpawnableHoldQueueData
					{
						missingBarcode = val.SpawnData.Barcode,
						_data = val
					});
					return false;
				}
				if (LevelHoldQueue.LevelInQueue())
				{
					SpawnableHoldQueue.AddToQueue(val);
					return false;
				}
			}
			return true;
		}
	}
}
