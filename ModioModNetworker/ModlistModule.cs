using System;
using LabFusion.SDK.Modules;
using MelonLoader;

namespace ModioModNetworker;

public class ModlistModule : Module
{
	public override string Name => "ModIoModNetworkerModule";

	public override string Author => "notnotnotswipez";

	public override Version Version => new Version("2.8.4");

	public override ConsoleColor Color => ConsoleColor.Cyan;

	protected override void OnModuleRegistered()
	{
		MelonLogger.Msg("Registered internal module!");
	}
}
