using System;
using System.Collections.Generic;
using Il2CppSLZ.Marrow.SceneStreaming;
using LabFusion.Network;
using LabFusion.Player;
using LabFusion.SDK.Modules;
using ModioModNetworker.UI;
using ModioModNetworker.Data;

namespace ModioModNetworker;

public class ModlistMessage : ModuleMessageHandler
{
	private static List<ModInfo> modlist = new List<ModInfo>();

	public static Dictionary<PlayerID, ModInfo> avatarMods = new Dictionary<PlayerID, ModInfo>();

	public static List<ModInfo> waitAndQueue = new List<ModInfo>();

	protected override void OnHandleMessage(ReceivedMessage message)
	{
		ModlistData modlistData = message.ReadData<ModlistData>();
		if (NetworkInfo.IsHost && message.IsServerHandled)
		{
			if (modlistData.modType == ModlistData.ModType.LIST)
			{
				return;
			}
			NetMessage val = NetMessage.ModuleCreate<ModlistMessage>(message.Bytes, CommonMessageRoutes.ReliableToClients, (byte?)null);
			try
			{
				MessageSender.BroadcastMessageExcept((byte)modlistData.playerId, (NetworkChannel)0, val, true);
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		if (modlistData == null)
		{
			return;
		}
		ModInfo modInfo = modlistData.serializedModInfo.modInfo;
		if (modlistData.modType != ModlistData.ModType.LIST)
		{
			bool flag = true;
			switch (modlistData.modType)
			{
			case ModlistData.ModType.AVATAR:
			{
				if (!MainClass.overrideFusionDL)
				{
					break;
				}
				if (!avatarMods.ContainsKey(modlistData.playerId))
				{
					avatarMods.Add(modlistData.playerId, modInfo);
				}
				else
				{
					avatarMods[modlistData.playerId] = modInfo;
				}
				if (SceneStreamer._session != null && (int)SceneStreamer._session.Status == 1)
				{
					waitAndQueue.Add(modInfo);
					break;
				}
				float num4 = modInfo.fileSizeKB / 1000000f;
				if (num4 < MainClass.maxAutoDownloadMb && MainClass.autoDownloadAvatars && (MainClass.downloadMatureContent || !modInfo.mature) && !MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId) && !modInfo.IsSubscribed())
				{
					if (MainClass.tempLobbyMods)
					{
						modInfo.temp = true;
					}
					if (ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = modlistData.playerId,
						info = modInfo,
						notify = true
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(modInfo.numericalId);
					}
				}
				break;
			}
			case ModlistData.ModType.SPAWNABLE:
			{
				if (!MainClass.overrideFusionDL)
				{
					break;
				}
				float num3 = modInfo.fileSizeKB / 1000000f;
				if (MainClass.autoDownloadSpawnables && num3 < MainClass.maxAutoDownloadMb && (MainClass.downloadMatureContent || !modInfo.mature) && !modInfo.IsSubscribed() && !MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId))
				{
					if (MainClass.tempLobbyMods)
					{
						modInfo.temp = true;
					}
					if (ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = modInfo,
						notify = true
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(modInfo.numericalId);
					}
				}
				break;
			}
			case ModlistData.ModType.LEVEL:
			{
				if (!MainClass.overrideFusionDL || MainClass.useRepo)
				{
					break;
				}
				float num = modInfo.fileSizeKB / 1000000f;
				float num2 = num / 1000f;
				if (MainClass.autoDownloadLevels && num2 < MainClass.levelMaxGb && (MainClass.downloadMatureContent || !modInfo.mature) && !MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId))
				{
					if (MainClass.tempLobbyMods)
					{
						modInfo.temp = true;
					}
					if (ModFileManager.AddToQueue(new DownloadQueueElement
					{
						associatedPlayer = null,
						info = modInfo,
						notify = true
					}))
					{
						MainClass.modNumericalsDownloadedDuringLobbySession.Add(modInfo.numericalId);
					}
				}
				break;
			}
			}
		}
		else
		{
			if (modInfo.isValidMod)
			{
				modlist.Add(modInfo);
			}
			if (modlistData.isFinal)
			{
				NetworkerMenuController.SetHostSubscribedMods(modlist);
				modlist.Clear();
				MainClass.confirmedHostHasIt = true;
			}
		}
	}
}
