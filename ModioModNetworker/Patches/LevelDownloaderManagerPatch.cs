using HarmonyLib;
using LabFusion.Scene;

namespace ModioModNetworker.Patches;

[HarmonyPatch(typeof(LevelDownloaderManager), "LoadWaitingScene")]
public class LevelDownloaderManagerPatch
{
	public static bool Prefix()
	{
		if (MainClass.overrideFusionDL)
		{
			return false;
		}
		return true;
	}
}
