using System.Runtime.InteropServices;
using USER_TYPE = int;

public class Program
{
	public static int ikcp_output(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user) {
		// from https://developer.aliyun.com/article/943678
		// 回调的话得用IntPtr，不能直接用byte[]，然后自己转bytes[]
		var bytes = new byte[len];
		Marshal.Copy(buf, bytes, 0, len);
		LogUtil.Info($"ikcp_output, len={len}, user={user}, conv={kcp.conv}, buf:{System.Text.Encoding.UTF8.GetString(bytes)}");
		pcks.Enqueue(bytes);
		return 0;
	}
	public static void ikcp_writelog(string log, ref IKCPCB kcp, USER_TYPE user)
	{
		LogUtil.Info($"ikcp_writelog, log={log}, user={user}, conv={kcp.conv}");
	}
	public static void Main(string[] args)
	{
		var netThread = new Thread(KCPRun);
		var testInThread = new Thread(KCPTestIn);
		var testOutThread = new Thread(KCPTestOut);
		var testInPckThread = new Thread(KCPTestInPck);
		netThread.Start();
		testInThread.Start();
		testOutThread.Start();
		testInPckThread.Start();

		// See https://aka.ms/new-console-template for more information
		Console.WriteLine("Main done");
	}
	public static int millisecondsTimeout = 500;
	public static void KCPRun()
	{
		KCPUtil.Create(1, 1101);
		var kcpData = KCPUtil.GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		KCPUtil.SetKCPData(kcpData);
		while (true) {
			try
			{
				var begin = TimeUtil.GetTimeStamp();
				KCPUtil.Update((uint)begin);
				var end = TimeUtil.GetTimeStamp();
				var cost = end - begin;
				var sleep = millisecondsTimeout - cost;
				if(sleep > 0)
				{
					LogUtil.Info($"sleep: {sleep}");
					Thread.Sleep((int)sleep);
				}
				else
				{
					LogUtil.Error($"sleep time too long: {sleep}, cost: {cost}");
				}
			}
			catch (Exception e)
			{
				LogUtil.Error($"kcp exception, {e}");
				break;
			}
		}
		KCPUtil.Release();
	}


	public static Queue<byte[]> pcks = new Queue<byte[]>();
	public static void KCPTestIn()
	{
		while(!KCPUtil.IsReady())
		{
			Thread.Sleep(1000);
		}
		var ss = "asdf";
		var bs = System.Text.Encoding.UTF8.GetBytes(ss);
		var os = System.Text.Encoding.UTF8.GetString(bs);
		KCPUtil.Send(bs);
		KCPUtil.Flush();
		LogUtil.Info($"KCPTestSend:{ss}, bytes:{os}");
	}
	public static void KCPTestInPck()
	{
		while(!KCPUtil.IsReady())
		{
			Thread.Sleep(1000);
		}
		while(true)
		{
			if(!pcks.TryDequeue(out var pck))
			{
				Thread.Sleep(1000);
				continue;
			}
			KCPUtil.Input(pck);
			LogUtil.Info($"KCPTestInPck:{System.Text.Encoding.UTF8.GetString(pck)}");
		}
	}
	public static void KCPTestOut()
	{
		while(!KCPUtil.IsReady())
		{
			Thread.Sleep(1000);
		}
		while(true)
		{
			var bs = new byte[128];
			KCPUtil.Receive(bs);
			var ss = System.Text.Encoding.UTF8.GetString(bs);
			LogUtil.Info($"KCPTestOut:{ss}");
			Thread.Sleep(5000);
		}
	}
}

