using HarmonyLib;
using LabFusion.Entities;

namespace ModioModNetworker.Patches;

// DISABLED: Harmony target "LabFusion.Entities.RigProgressBar.set_Visible" 
// is undefined in the current LabFusion build
//[HarmonyPatch(typeof(LabFusion.Entities.RigProgressBar), "set_Visible")]
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
