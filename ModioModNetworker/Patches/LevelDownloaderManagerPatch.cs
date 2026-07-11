using HarmonyLib;
using LabFusion.Scene;
using MelonLoader;

namespace ModioModNetworker.Patches;

[HarmonyPatch(typeof(LevelDownloaderManager), "LoadWaitingScene")]
public class LevelDownloaderManagerPatch
{
	public static bool Prefix()
	{
		if (MainClass.overrideFusionDL)
		{
			MelonLogger.Msg("[OverrideFusionDL] Blocked LevelDownloaderManager.LoadWaitingScene — Networker controls download UI");
			return false;
		}
		MelonLogger.Msg("[OverrideFusionDL] Pass-through — Fusion handles LoadWaitingScene");
		return true;
	}
}
