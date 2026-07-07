using HarmonyLib;
using LabFusion.Entities;
using ModioModNetworker.Queue;

namespace ModioModNetworker.Patches;

public class SyncableCleanupPatch
{
	[HarmonyPatch(typeof(NetworkEntityManager), "OnCleanupEntities")]
	private static class CleanupPatchClass
	{
		public static void Prefix()
		{
			SpawnableHoldQueue.ClearQueue();
			SpawnableHoldQueue.ClearSpawnResponseDatas();
		}
	}
}
