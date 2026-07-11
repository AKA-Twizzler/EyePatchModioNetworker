using System;
using System.Collections.Generic;
using MelonLoader;
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
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Invalid comparison between Unknown and I4
		ModlistData modlistData = message.ReadData<ModlistData>();
		MelonLogger.Msg("[ModlistMessage] Received " + modlistData.modType + " from player " + modlistData.playerId + " — mod=" + (modlistData.serializedModInfo?.modInfo != null ? (modlistData.serializedModInfo.modInfo.modName ?? modlistData.serializedModInfo.modInfo.modId ?? "unknown") : "null"));
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
				string modName = modInfo.modName ?? modInfo.modId ?? "unknown";
				bool sizeRejected = num4 >= MainClass.maxAutoDownloadMb;
				bool matureRejected = modInfo.mature && !MainClass.downloadMatureContent;
				bool alreadyDownloaded = MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId);
				bool alreadySubscribed = modInfo.IsSubscribed();
				if (sizeRejected) MelonLogger.Msg("[ModlistMessage] AVATAR " + modName + " too large — size=" + num4 + "MB limit=" + MainClass.maxAutoDownloadMb + "MB");
				if (matureRejected) MelonLogger.Msg("[ModlistMessage] AVATAR " + modName + " mature — downloadMatureContent=false");
				if (alreadyDownloaded) MelonLogger.Msg("[ModlistMessage] AVATAR " + modName + " already downloaded this session");
				if (alreadySubscribed) MelonLogger.Msg("[ModlistMessage] AVATAR " + modName + " already subscribed — skipping auto-download");
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
				string modName2 = modInfo.modName ?? modInfo.modId ?? "unknown";
				bool sizeRejected2 = num3 >= MainClass.maxAutoDownloadMb;
				bool matureRejected2 = modInfo.mature && !MainClass.downloadMatureContent;
				bool alreadySubscribed2 = modInfo.IsSubscribed();
				bool alreadyDownloaded2 = MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId);
				if (sizeRejected2) MelonLogger.Msg("[ModlistMessage] SPAWNABLE " + modName2 + " too large — size=" + num3 + "MB limit=" + MainClass.maxAutoDownloadMb + "MB");
				if (matureRejected2) MelonLogger.Msg("[ModlistMessage] SPAWNABLE " + modName2 + " mature — downloadMatureContent=false");
				if (alreadySubscribed2) MelonLogger.Msg("[ModlistMessage] SPAWNABLE " + modName2 + " already subscribed — skipping auto-download");
				if (alreadyDownloaded2) MelonLogger.Msg("[ModlistMessage] SPAWNABLE " + modName2 + " already downloaded this session");
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
				string modName3 = modInfo.modName ?? modInfo.modId ?? "unknown";
				bool sizeRejected3 = num2 >= MainClass.levelMaxGb;
				bool matureRejected3 = modInfo.mature && !MainClass.downloadMatureContent;
				bool alreadyDownloaded3 = MainClass.modNumericalsDownloadedDuringLobbySession.Contains(modInfo.numericalId);
				if (sizeRejected3) MelonLogger.Msg("[ModlistMessage] LEVEL " + modName3 + " too large — size=" + num2.ToString("F2") + "GB limit=" + MainClass.levelMaxGb + "GB");
				if (matureRejected3) MelonLogger.Msg("[ModlistMessage] LEVEL " + modName3 + " mature — downloadMatureContent=false");
				if (alreadyDownloaded3) MelonLogger.Msg("[ModlistMessage] LEVEL " + modName3 + " already downloaded this session");
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
			}
		}
	}
}
