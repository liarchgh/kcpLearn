using System.Runtime.InteropServices;
using System.Security.Cryptography;
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

		ThreadUtil.GenerateServiceThread("kcp update", (begin) =>
		{
			KCPUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if(_pcksToSend.TryDequeue(out var bs))
			{
				KCPUtil.Send(GenKCPPck(bs.Item1, bs.Item2));
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

		ThreadUtil.GenerateServiceThread("kcp update", (begin) =>
		{
			KCPUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if (KCPUtil.TryReceive(out var bs))
			{
				action(bs);
			}
		}).Start();
	}
	// from https://chenqinghe.com/?p=25
	//KCP帧头8字节对齐，KCP空包大小为24字节。

	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |             conv              |  cmd  |  frg  |      wnd      |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |               ts              |               sn              |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |               una             |               len             |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |                                                               |
	// *                              data                             *
	// |                                                               |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	private static int ikcp_output(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user) {
		// from https://developer.aliyun.com/article/943678
		// 回调的话得用IntPtr，不能直接用byte[]，然后自己转bytes[]
		var bytes = new byte[len];
		Marshal.Copy(buf, bytes, 0, len);
		var cmd = bytes[4];
		var frg = bytes[5];
		LogUtil.Debug($"ikcp_output, len={len}, frg:{frg}, cmd:{cmd}, user={user}, stream:{kcp.stream}, mtu:{kcp.mtu}, mss:{kcp.mss}, state:{kcp.state}, conv={kcp.conv}");
		// , buf:{System.Text.Encoding.UTF8.GetString(bytes)}
		UDPUtil.SendByets(bytes);
		return 0;
	}
	public static void ikcp_writelog(string log, ref IKCPCB kcp, USER_TYPE user)
	{
		LogUtil.Info($"ikcp_writelog, log={log}, user={user}, conv={kcp.conv}");
	}
	private static Queue<(PCK_TYPE, byte[])> _pcksToSend = new Queue<(PCK_TYPE, byte[])>();
	public static void SendText(string text)
	{
		var bs = System.Text.Encoding.UTF8.GetBytes(text);
		_sendBytes(PCK_TYPE.TEXT, bs);
	}
	public static void SendFile(string filePath)
	{
		var bs = File.ReadAllBytes(filePath);
		_sendBytes(PCK_TYPE.FILE, bs);
	}
	private static void _sendBytes(PCK_TYPE dataType, byte[] bs)
	{
		var kcpData = KCPUtil.GetKCPData();
		var singlePckMaxSize = (int)kcpData.mss * (KCPUtil.FRG_MAX-1)-1;
		var multPckMaxSize = singlePckMaxSize-1;
		if(bs.Length >= singlePckMaxSize)
		{
			var pckBytes = GenKCPPck(dataType, bs);
			var mulCount = Math.Ceiling((float)pckBytes.Length / multPckMaxSize);
			for(int i = 0; i < mulCount; i++)
			{
				var startIdx = i * multPckMaxSize;
				var endIdx = Math.Min(startIdx + multPckMaxSize, pckBytes.Length);
				var mulPck = pckBytes[startIdx..endIdx];
				var mulDataType = i == mulCount-1?PCK_TYPE.MULE:PCK_TYPE.MULS;
				_pcksToSend.Enqueue((mulDataType, mulPck));
			}
		}
		else
		{
			_pcksToSend.Enqueue((dataType, bs));
		}
	}
	private static byte[] GenKCPPck(PCK_TYPE dataType, byte[] bs)
	{
		var bsToSend = new byte[bs.Length+1];
		bsToSend[0] = (byte)dataType;
		bs.CopyTo(bsToSend, 1);
		return bsToSend;
	}
	private enum PCK_TYPE
	{
		MULS =	0x00,
		MULE =	0x01,
		TEXT =	0x02,
		FILE =	0x03,
	}
	private static Dictionary<PCK_TYPE, Action<byte[]>> PacketHandlers = new Dictionary<PCK_TYPE, Action<byte[]>>()
	{
		// DEBUG
		{PCK_TYPE.TEXT, (bs) => { LogUtil.Info(System.Text.Encoding.UTF8.GetString(bs)); }},
		{PCK_TYPE.FILE, (bs) => { File.WriteAllBytes("test.bin", bs); }},
	};
	private static List<byte> _kcpPckReceiveCache = new List<byte>();
	public static void OnPckBytes(byte[] bs)
	{
		var dataType = (PCK_TYPE)bs[0];
		if(dataType == PCK_TYPE.MULS
			|| dataType == PCK_TYPE.MULE)
		{
			_kcpPckReceiveCache.AddRange(bs[1..]);
			if(dataType == PCK_TYPE.MULE)
			{
				var pckBs = _kcpPckReceiveCache.ToArray();
				_kcpPckReceiveCache.Clear();
				OnPckBytes(pckBs);
			}
		}
		else if(PacketHandlers.TryGetValue(dataType, out var packetHandler))
		{
			packetHandler.Invoke(bs[1..]);
		}
	}
}