using System;
using System.Collections.Concurrent;

namespace ModioModNetworker.UI;

public class MainThreadManager
{
	private static ConcurrentQueue<GenericThreadingJob> genericThreadJobs = new ConcurrentQueue<GenericThreadingJob>();

	public static void HandleQueue()
	{
		if (genericThreadJobs.Count > 0 && genericThreadJobs.TryDequeue(out GenericThreadingJob result))
		{
			result.action();
		}
	}

	public static void QueueAction(Action action)
	{
		genericThreadJobs.Enqueue(new GenericThreadingJob
		{
			action = action
		});
	}
}
