using System;

namespace ModioModNetworker.Data;

public class ModInfoThreadRequest
{
	public string modId;

	public string json;

	public string destination;

	public Action<ModInfo> onInfo;

	public bool mature = false;

	public dynamic originalModInfo;
}
