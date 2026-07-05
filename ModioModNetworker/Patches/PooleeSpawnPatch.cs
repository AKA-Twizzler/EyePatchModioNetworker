using System;
using HarmonyLib;
using Il2CppSLZ.Marrow.Pool;
using LabFusion.Data;
using LabFusion.Entities;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using ModInfo = ModioModNetworker.Data.ModInfo;
using ModioModNetworker.Utilities;

namespace ModioModNetworker.Patches;

public class PooleeSpawnPatch
{
	[HarmonyPatch(typeof(Poolee), "OnSpawn")]
	private static class SpawnPatchClass
	{
		public static void Prefix(Poolee __instance)
		{
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if (!MainClass.confirmedHostHasIt || !NetworkInfo.HasServer)
			{
				return;
			}
			try
			{
				ModInfo modInfoForPoolee = ModInfoUtilities.GetModInfoForPoolee(__instance);
				if (modInfoForPoolee == null)
				{
					return;
				}
				NetWriter val = NetWriter.Create();
				try
				{
					ModlistData modlistData = ModlistData.Create(PlayerIDManager.LocalID, modInfoForPoolee, ModlistData.ModType.SPAWNABLE);
					modlistData.Serialize((INetSerializer)(object)val);
					NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
					try
					{
						MessageSender.BroadcastMessageExceptSelf((NetworkChannel)0, val2);
					}
					finally
					{
						((IDisposable)val2)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch (Exception)
			{
			}
		}
	}

	[HarmonyPatch(typeof(SpawnSender), "SendCatchupSpawn", new Type[]
	{
		typeof(byte),
		typeof(string),
		typeof(ushort),
		typeof(SerializedTransform),
		typeof(byte),
		typeof(EntitySource)
	})]
	private static class CatchupSpawnPatch
	{
		public static void Prefix(byte ownerID, string barcode, ushort entityID, SerializedTransform serializedTransform, byte playerID, EntitySource source)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			if (!NetworkInfo.IsHost)
			{
				return;
			}
			ModInfo modInfoForSpawnableBarcode = ModInfoUtilities.GetModInfoForSpawnableBarcode(barcode);
			if (modInfoForSpawnableBarcode == null)
			{
				return;
			}
			NetWriter val = NetWriter.Create();
			try
			{
				ModlistData modlistData = ModlistData.Create(PlayerIDManager.LocalID, modInfoForSpawnableBarcode, ModlistData.ModType.SPAWNABLE);
				modlistData.Serialize((INetSerializer)(object)val);
				NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
				try
				{
					MessageSender.BroadcastMessageExceptSelf((NetworkChannel)0, val2);
				}
				finally
				{
					((IDisposable)val2)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
	}
}
