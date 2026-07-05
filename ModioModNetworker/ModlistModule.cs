using System;
using LabFusion.SDK.Modules;

namespace ModioModNetworker;

public class ModlistModule : Module
{
	public override string Name => "ModIoModNetworkerModule";

	public override string Author => "notnotnotswipez";

	public override Version Version => new Version("2.8.1");

	public override ConsoleColor Color => ConsoleColor.Cyan;

	protected override void OnModuleRegistered()
	{
		ModuleMessageManager.RegisterHandler<ModlistMessage>();
	}

	protected override void OnModuleUnregistered()
	{
	}
}
