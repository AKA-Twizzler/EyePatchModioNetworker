using MelonLoader;
using Il2CppSLZ.Marrow.SceneStreaming;
using LabFusion.Network;
using LabFusion.Scene;
using LabFusion.UI.Popups;

namespace ModioModNetworker.Queue;

public class LevelHoldQueue
{
	public class LevelHoldQueueData
	{
		public string missingBarcode;

		public LevelLoadData _data;
	}

	private static LevelHoldQueueData queueData;

	public static bool waitingForLevel;

	public static bool waitingForLevelToLoad;

	public static bool finishedLoadingLevel;

	public static void ClearQueue()
	{
		MelonLogger.Msg("[LevelHoldQueue] ClearQueue called");
		queueData = null;
		waitingForLevel = false;
		waitingForLevelToLoad = false;
		finishedLoadingLevel = false;
	}

	public static bool LevelInQueue()
	{
		return queueData != null || waitingForLevel || waitingForLevelToLoad || finishedLoadingLevel;
	}

	public static void SetQueue(LevelHoldQueueData data)
	{
		MelonLogger.Msg("[LevelHoldQueue] SetQueue — barcode=" + (data.missingBarcode ?? "null"));
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		Notifier.Send(new Notification
		{
			Title = new NotificationText("The host tried loading a level you dont have. \"" + data.missingBarcode + "\""),
			Message = new NotificationText("Wait a bit, it may start downloading!"),
			PopupLength = 3f,
			SaveToMenu = false,
			ShowPopup = true
		});
		queueData = data;
	}

	public static void CheckValid(string barcode)
	{
		if (queueData != null && queueData.missingBarcode == barcode)
		{
			MelonLogger.Msg("[LevelHoldQueue] CheckValid — MATCH for barcode=" + barcode + " calling Handle");
			Handle(queueData._data);
			waitingForLevel = true;
			queueData = null;
		}
	}

	public static void Update()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Invalid comparison between Unknown and I4
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Invalid comparison between Unknown and I4
		if (waitingForLevel && SceneStreamer._session != null && (int)SceneStreamer._session.Status == 1)
		{
			MelonLogger.Msg("[LevelHoldQueue] Update — level loading started: " + (queueData != null ? (queueData.missingBarcode ?? "unknown") : "null"));
			waitingForLevelToLoad = true;
			waitingForLevel = false;
		}
		if (waitingForLevelToLoad && SceneStreamer._session != null && (int)SceneStreamer._session.Status != 1)
		{
			MelonLogger.Msg("[LevelHoldQueue] Update — level load complete: " + (queueData != null ? (queueData.missingBarcode ?? "unknown") : "null"));
			waitingForLevelToLoad = false;
			finishedLoadingLevel = true;
		}
		if (finishedLoadingLevel)
		{
			SpawnableHoldQueue.HandleAllSpawnResponseDatas();
			finishedLoadingLevel = false;
		}
		if (SceneStreamer._session != null && !LevelInQueue() && (int)SceneStreamer._session.Status == 2)
		{
			SpawnableHoldQueue.ClearSpawnResponseDatas();
		}
	}

	private static void Handle(LevelLoadData data)
	{
		MelonLogger.Msg("[LevelHoldQueue] Handle — calling FusionSceneManager.SetTargetScene barcode=" + data.LevelBarcode);
		FusionSceneManager.SetTargetScene(data.LevelBarcode, data.LoadingScreenBarcode);
		NetworkSceneManager.Purgatory = false;
	}
}
