using System;

namespace ModioModNetworker.Utilities;

public class TimerDelayedAction
{
	public float time;

	public Action onTimeOver;

	public bool completed = false;
}
