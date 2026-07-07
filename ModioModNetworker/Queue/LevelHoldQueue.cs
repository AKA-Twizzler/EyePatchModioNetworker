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
			Handle(queueData._data);
			waitingForLevel = true;
			queueData = null;
		}
	}

	public static void Update()
	{
		if (waitingForLevel && SceneStreamer._session != null && (int)SceneStreamer._session.Status == 1)
		{
			waitingForLevelToLoad = true;
			waitingForLevel = false;
		}
		if (waitingForLevelToLoad && SceneStreamer._session != null && (int)SceneStreamer._session.Status != 1)
		{
			waitingForLevelToLoad = false;
			finishedLoadingLevel = true;
		}
		if (finishedLoadingLevel)
		{
			SpawnableHoldQueue.HandleAllSpawnResponseDatas();
			finishedLoadingLevel = false;
		}
		// Intentionally not clearing — cleanup is SyncableCleanupPatch's responsibility.
	}

	private static void Handle(LevelLoadData data)
	{
		FusionSceneManager.SetTargetScene(data.LevelBarcode, data.LoadingScreenBarcode);
		NetworkSceneManager.Purgatory = false;
	}
}
