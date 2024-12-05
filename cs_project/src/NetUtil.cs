using System.Runtime.InteropServices;
using USER_TYPE = int;
class NetUtil
{
	public static void StartThreads()
	{
		UDPUtil.StartThreads();

		KCPUtil.Create(1, 1101);
		var kcpData = KCPUtil.GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		KCPUtil.SetKCPData(kcpData);

		UDPUtil.AddListen(KCPUtil.Input);

		GenerateServiceThread((begin) => KCPUtil.Update((uint)begin)).Start();
		GenerateServiceThread(
			(time) =>
			{
				if (KCPUtil.TryReceive(out var bs))
				{
					var ss = System.Text.Encoding.UTF8.GetString(bs);
					LogUtil.Info($"KCPOut:{ss}");
				}

			}
		).Start();
		GenerateServiceThread(
			(time) =>
			{
				if(pcksToSend.TryDequeue(out var bs))
				{
					KCPUtil.Send(bs);
				}
			}
		).Start();
	}
	public static int ikcp_output(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user) {
		// from https://developer.aliyun.com/article/943678
		// 回调的话得用IntPtr，不能直接用byte[]，然后自己转bytes[]
		var bytes = new byte[len];
		Marshal.Copy(buf, bytes, 0, len);
		LogUtil.Debug($"ikcp_output, len={len}, user={user}, conv={kcp.conv}, buf:{System.Text.Encoding.UTF8.GetString(bytes)}");
		UDPUtil.SendBytes(bytes);
		return len;
	}
	public static void ikcp_writelog(string log, ref IKCPCB kcp, USER_TYPE user)
	{
		LogUtil.Info($"ikcp_writelog, log={log}, user={user}, conv={kcp.conv}");
	}
	public static int millisecondsTimeout = 500;
	public delegate void ThreadRun(long timestamp);
	public static Thread GenerateServiceThread(ThreadRun action)
	{
		void run()
		{
			while (true) {
				try
				{
					if(KCPUtil.IsReady())
					{
						var begin = TimeUtil.GetTimeStamp();
						action(begin);
						var end = TimeUtil.GetTimeStamp();
						var cost = end - begin;
						var sleep = millisecondsTimeout - cost;
						if(sleep <= 0)
						{
							LogUtil.Error($"sleep time too long: {sleep}, cost: {cost}");
							continue;
						}
						Thread.Sleep((int)sleep);
					}
				}
				catch (Exception e)
				{
					LogUtil.Error($"kcp exception, {e}");
					break;
				}
			}
		}
		return new Thread(run);
	}


	public static Queue<byte[]> pcksToSend = new Queue<byte[]>();
	public static void SendBytes(byte[] bs)
	{
		pcksToSend.Enqueue(bs);
	}
}