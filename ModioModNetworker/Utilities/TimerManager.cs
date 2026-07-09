using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModioModNetworker.Utilities;

public class TimerManager
{
	private static List<TimerDelayedAction> timerDelayedJobs = new List<TimerDelayedAction>();

	public static void Update()
	{
		foreach (TimerDelayedAction timerDelayedJob in timerDelayedJobs)
		{
			timerDelayedJob.time -= Time.deltaTime;
			if (timerDelayedJob.time <= 0f)
			{
				timerDelayedJob.onTimeOver();
				timerDelayedJob.completed = true;
			}
		}
		timerDelayedJobs.RemoveAll((TimerDelayedAction x) => x.completed);
	}

	public static void DelayAction(float time, Action onCompleted)
	{
		timerDelayedJobs.Add(new TimerDelayedAction
		{
			time = time,
			onTimeOver = onCompleted
		});
	}
}
