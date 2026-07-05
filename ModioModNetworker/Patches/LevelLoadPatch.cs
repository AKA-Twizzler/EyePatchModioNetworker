using System;
using HarmonyLib;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Marrow;
using LabFusion.Network;
using LabFusion.Scene;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Scene;
using LabFusion.Senders;
using ModioModNetworker.Data;
using ModioModNetworker.Queue;
using ModioModNetworker.Utilities;

namespace ModioModNetworker.Patches;

public class LevelLoadPatch
{
	[HarmonyPatch(typeof(LevelLoadMessage), "OnHandleMessage", new Type[] { typeof(ReceivedMessage) })]
	public static class PatchClass
	{
		public static bool Prefix(ReceivedMessage received)
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected O, but got Unknown
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			if (!NetworkInfo.IsHost && !received.IsServerHandled && MainClass.autoDownloadLevels)
			{
				LevelLoadData val = received.ReadData<LevelLoadData>();
				if (!MainClass.overrideFusionDL)
				{
					return true;
				}
				LevelHoldQueue.ClearQueue();
				if (!CrateFilterer.HasCrate<LevelCrate>(new Barcode(val.LevelBarcode)))
				{
					LevelHoldQueue.SetQueue(new LevelHoldQueue.LevelHoldQueueData
					{
						missingBarcode = val.LevelBarcode,
						_data = val
					});
					if (!MainClass.confirmedHostHasIt)
					{
						LevelDownloaderManager.DownloadLevel(new LevelDownloaderManager.LevelDownloadInfo
						{
							LevelBarcode = val.LevelBarcode,
							LevelHost = 0
						});
					}
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(LoadSender), "SendLevelLoad", new Type[]
	{
		typeof(string),
		typeof(string),
		typeof(ulong)
	})]
	private static class SendLevelPatchClass
	{
		public static void Prefix(string barcode, string loadBarcode, ulong userId)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			if (!NetworkInfo.IsHost)
			{
				return;
			}
			ModInfo modInfoForLevelBarcode = ModInfoUtilities.GetModInfoForLevelBarcode(barcode);
			if (modInfoForLevelBarcode == null)
			{
				return;
			}
			NetWriter val = NetWriter.Create();
			try
			{
				ModlistData modlistData = ModlistData.Create(PlayerIDManager.LocalID, modInfoForLevelBarcode, ModlistData.ModType.LEVEL);
				modlistData.Serialize((INetSerializer)(object)val);
				NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
				try
				{
					MessageSender.SendFromServer(userId, (NetworkChannel)0, val2);
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

	[HarmonyPatch(typeof(LoadSender), "SendLevelLoad", new Type[]
	{
		typeof(string),
		typeof(string)
	})]
	private static class SendLevelPatchClassGeneric
	{
		public static void Prefix(string barcode, string loadBarcode)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			if (!NetworkInfo.IsHost)
			{
				return;
			}
			ModInfo modInfoForLevelBarcode = ModInfoUtilities.GetModInfoForLevelBarcode(barcode);
			if (modInfoForLevelBarcode == null)
			{
				return;
			}
			NetWriter val = NetWriter.Create();
			try
			{
				ModlistData modlistData = ModlistData.Create(PlayerIDManager.LocalID, modInfoForLevelBarcode, ModlistData.ModType.LEVEL);
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
