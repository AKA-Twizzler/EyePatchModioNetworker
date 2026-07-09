using HarmonyLib;
using LabFusion.Entities;

namespace ModioModNetworker.Patches;

[HarmonyPatch(typeof(RigProgressBar), "set_Visible")]
public class RigProgressBarPatchReport
{
	public static void Prefix(RigProgressBar __instance, ref bool value)
	{
		if (MainClass.overrideFusionDL)
		{
			value = false;
		}
	}
}
