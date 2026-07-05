using System;
using HarmonyLib;
using LabFusion.Downloading;
using LabFusion.Downloading.ModIO;
using LabFusion.Entities;
using LabFusion.Player;
using LabFusion.Scene;
using ModioModNetworker.Data;

namespace ModioModNetworker.Patches;

[HarmonyPatch(typeof(ModIODownloader), "EnqueueDownload", new Type[] { typeof(ModTransaction) })]
public class ModIoDownloaderEnqueuePatch
{
	public static bool Prefix(ModTransaction transaction)
	{
		if (MainClass.overrideFusionDL)
		{
			ModIOFile modFile = transaction.ModFile;
			string item = modFile.ModID.ToString();
			if (!MainClass.modNumericalsDownloadedDuringLobbySession.Contains(item))
			{
				DownloadCallback callback = transaction.Callback;
				bool flag = false;
				if (((Delegate)(object)callback).Method.DeclaringType == typeof(LevelDownloaderManager))
				{
					flag = true;
				}
				string destination = "install_spawnable";
				if (transaction.Reporter != null && transaction.Reporter.GetType() == typeof(RigProgressBar))
				{
					destination = "install_avatar;";
					PlayerID ownerOfProgressBar = GetOwnerOfProgressBar((RigProgressBar)transaction.Reporter);
					destination = ((ownerOfProgressBar == null) ? "install_spawnable" : (destination + ownerOfProgressBar.SmallID));
				}
				if (flag)
				{
					destination = "install_level";
				}
				modFile = transaction.ModFile;
				ModInfo.RequestModInfoNumerical(modFile.ModID.ToString(), destination);
			}
			return false;
		}
		return true;
	}

	private static PlayerID GetOwnerOfProgressBar(RigProgressBar bar)
	{
		NetworkPlayer val = default(NetworkPlayer);
		foreach (PlayerID playerID in PlayerIDManager.PlayerIDs)
		{
			if (NetworkPlayerManager.TryGetPlayer((byte)playerID, out val) && val.AvatarSetter.ProgressBar == bar)
			{
				return playerID;
			}
		}
		return null;
	}
}
