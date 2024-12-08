using System.Runtime.InteropServices;
using USER_TYPE = int;
partial class NetUtil
{
	public static void StartClientThreads(int localPort, IPPort iPPort)
	{
		UDPUtil.Init(localPort, iPPort);

		KCPUtil.Create(1, 1101);
		var kcpData = KCPUtil.GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		KCPUtil.SetKCPData(kcpData);

		UDPUtil.AddListen(KCPUtil.Input);

		GenerateServiceThread("kcp update", (begin) =>
		{
			KCPUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if(_pcksToSend.TryDequeue(out var bs))
			{
				var bsToSend = new byte[bs.Item2.Length+1];
				bsToSend[0] = (byte)bs.Item1;
				bs.Item2.CopyTo(bsToSend, 1);
				KCPUtil.Send(bsToSend);
			}
		}).Start();
	}
	public static void StartServerThreads(int localPort, IPPort iPPort, Action<byte[]> action)
	{
		UDPUtil.Init(localPort, iPPort);

		KCPUtil.Create(1, 1101);
		var kcpData = KCPUtil.GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		KCPUtil.SetKCPData(kcpData);

		UDPUtil.AddListen(KCPUtil.Input);

		GenerateServiceThread("kcp update", (begin) =>
		{
			KCPUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if (KCPUtil.TryReceive(out var bs))
			{
				action(bs);
			}
		}).Start();
	}
	private static int ikcp_output(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user) {
		// from https://developer.aliyun.com/article/943678
		// 回调的话得用IntPtr，不能直接用byte[]，然后自己转bytes[]
		var bytes = new byte[len];
		Marshal.Copy(buf, bytes, 0, len);
		LogUtil.Debug($"ikcp_output, len={len}, user={user}, buffer:{kcp.buffer}, incr:{kcp.incr}, cwnd:{kcp.cwnd}, mss:{kcp.mss}, state:{kcp.state}, conv={kcp.conv}, buf:{System.Text.Encoding.UTF8.GetString(bytes)}");
		UDPUtil.SendByets(bytes);
		return 0;
	}
	public static void ikcp_writelog(string log, ref IKCPCB kcp, USER_TYPE user)
	{
		LogUtil.Info($"ikcp_writelog, log={log}, user={user}, conv={kcp.conv}");
	}
	public static int millisecondsTimeout = 100;
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
					if(KCPUtil.IsReady())
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


	public static Queue<(DATA_TYPE, byte[])> _pcksToSend = new Queue<(DATA_TYPE, byte[])>();
	public static void SendText(string text)
	{
		var bs = System.Text.Encoding.UTF8.GetBytes(text);
		_sendBytes(DATA_TYPE.TEXT, bs);
	}
	public static void SendFile(string filePath)
	{
		var bs = File.ReadAllBytes(filePath);
		_sendBytes(DATA_TYPE.FILE, bs);
	}
	private static void _sendBytes(DATA_TYPE dataType, byte[] bs)
	{
		_pcksToSend.Enqueue((dataType, bs));
	}
	public enum DATA_TYPE
	{
		TEXT =	0b01,
		FILE =	0b10,
	}
	public static Dictionary<DATA_TYPE, Action<byte[]>> PacketHandlers = new Dictionary<DATA_TYPE, Action<byte[]>>()
	{
		{DATA_TYPE.TEXT, (bs) => { LogUtil.Info(System.Text.Encoding.UTF8.GetString(bs)); }},
		{DATA_TYPE.FILE, (bs) => { File.WriteAllBytes("test.bin", bs); }},
	};
}