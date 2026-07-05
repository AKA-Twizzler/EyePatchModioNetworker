using System;

namespace ThunderstoreModAssistant.Utilities;

public class TimerDelayedAction
{
	public float time;

	public Action onTimeOver;

	public bool completed = false;
}
