using System;
using HarmonyLib;
using Il2CppSLZ.Marrow.Warehouse;
using LabFusion.Data;
using LabFusion.Network;
using LabFusion.Network.Serialization;
using LabFusion.Player;
using LabFusion.Senders;
using ModInfo = ModioModNetworker.Data.ModInfo;
using ModioModNetworker.Data;
using MelonLoader;

namespace ModioModNetworker.Patches;

public class AvatarSwitchPatch
{
	[HarmonyPatch(typeof(PlayerSender), "SendPlayerAvatar")]
	public static class PlayerSenderPatch
	{
		public static void Postfix()
		{
			if (!MainClass.overrideFusionDL)
			{
				MelonLogger.Msg("[OverrideFusionDL] Pass-through — Fusion handles avatar switch");
				return;
			}
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			if (!NetworkInfo.HasServer || !MainClass.confirmedHostHasIt)
			{
				return;
			}
			ModInfo modInfo = null;
			foreach (InstalledModInfo installedModInfo in MainClass.InstalledModInfos)
			{
				if (installedModInfo.palletBarcode == ((Scannable)((Crate)((CrateReferenceT<AvatarCrate>)(object)RigData.Refs.RigManager._avatarCrate).Crate)._pallet)._barcode._id)
				{
					modInfo = installedModInfo.ModInfo;
					break;
				}
			}
			if (!ModlistMessage.avatarMods.ContainsKey(PlayerIDManager.LocalID))
			{
				ModlistMessage.avatarMods.Add(PlayerIDManager.LocalID, modInfo);
			}
			else
			{
				ModlistMessage.avatarMods[PlayerIDManager.LocalID] = modInfo;
			}
			if (modInfo == null)
			{
				return;
			}
			NetWriter val = NetWriter.Create();
			try
			{
				ModlistData modlistData = ModlistData.Create(PlayerIDManager.LocalID, modInfo, ModlistData.ModType.AVATAR);
				modlistData.Serialize((INetSerializer)(object)val);
				NetMessage val2 = NetMessage.ModuleCreate<ModlistMessage>(val, CommonMessageRoutes.ReliableToClients, (byte?)null);
				try
				{
					MelonLogger.Msg("[OverrideFusionDL] Broadcasting avatar change: player=" + PlayerIDManager.LocalID.SmallID + " mod=" + modInfo.modId);
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
