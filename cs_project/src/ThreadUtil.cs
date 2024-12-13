public class ThreadUtil
{
	private static int millisecondsTimeout = 100;
	public delegate void ThreadRun(long timestamp);
	public static Thread GenerateServiceThread(string name, ThreadRun action)
	{
		return GenerateServiceThreadFull(name, action, millisecondsTimeout);
	}
	public static Thread GenerateServiceThreadNoSleep(string name, ThreadRun action)
	{
		return GenerateServiceThreadFull(name, action, 0);
	}
	private static Thread GenerateServiceThreadFull(string name, ThreadRun action, int callInternal)
	{
		void run()
		{
			while (true) {
				try
				{
					var begin = TimeUtil.GetTimeStamp();
					action(begin);
					var end = TimeUtil.GetTimeStamp();
					var cost = end - begin;
					if(callInternal <= 0)
					{
						continue;
					}
					var sleep = callInternal - cost;
					if(sleep <= 0)
					{
						LogUtil.Error($"sleep time too long, name:{name}, sleep:{sleep}, cost: {cost}, begin:{begin}, end:{end}");
						continue;
					}
					Thread.Sleep((int)sleep);
				}
				catch (Exception e)
				{
					LogUtil.Error($"service exception, {e}, name:{name}");
					break;
				}
			}
		}
		return new Thread(run);
	}
}
