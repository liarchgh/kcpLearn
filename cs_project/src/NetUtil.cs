public partial class NetUtil
{
	// TODO KCPConnectKey是不是不应该传出来
	public delegate void ServerDataHandle(KCPConnectKey iPEndPoint, byte[] bytes);
	public static void StartServerThreads(int localPort, ServerDataHandle action)
	{
		UDPUtil.Init(localPort);

		UDPUtil.AddListen(KCPUtil.Input);

		ThreadUtil.GenerateServiceThread("kcp update", (begin) =>
		{
			KCPUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if (KCPUtil.TryReceive(out var connectKey, out var bs))
			{
				action(connectKey, bs);
			}
		}).Start();
	}
	protected static Queue<(PCK_TYPE, byte[])> _pcksToSend = new Queue<(PCK_TYPE, byte[])>();
	public static void SendText(KCPConnectKey connectKey, string text)
	{
		var bs = System.Text.Encoding.UTF8.GetBytes(text);
		_sendBytes(connectKey, PCK_TYPE.TEXT, bs);
	}
	public static void SendFile(KCPConnectKey connectKey, string filePath)
	{
		var bs = File.ReadAllBytes(filePath);
		_sendBytes(connectKey, PCK_TYPE.FILE, bs);
	}
	protected static void _sendBytes(KCPConnectKey connectKey, PCK_TYPE dataType, byte[] bs)
	{
		if(!KCPUtil.TryGetKCPData(connectKey, out var kcpData)) return;
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
	protected static byte[] GenKCPPck(PCK_TYPE dataType, byte[] bs)
	{
		var bsToSend = new byte[bs.Length+1];
		bsToSend[0] = (byte)dataType;
		bs.CopyTo(bsToSend, 1);
		return bsToSend;
	}
	protected enum PCK_TYPE
	{
		MULS =	0x00,
		MULE =	0x01,
		TEXT =	0x02,
		FILE =	0x03,
	}
	private delegate void KCPPackageHandler(KCPConnectKey connectKey, byte[] bs);
	private static Dictionary<PCK_TYPE, KCPPackageHandler> PacketHandlers = new Dictionary<PCK_TYPE, KCPPackageHandler>()
	{
		// DEBUG
		{PCK_TYPE.TEXT, (connectKey, bs) => { LogUtil.Info($"{connectKey}:{System.Text.Encoding.UTF8.GetString(bs)}"); }},
		{PCK_TYPE.FILE, (connectKey, bs) =>
			{
				var filePath = "test.bin";
				LogUtil.Info($"receive file from:{connectKey}, to file:{filePath}");
				File.WriteAllBytes(filePath, bs);
			}
		},
	};
	private static List<byte> _kcpPckReceiveCache = new List<byte>();
	public static void OnPckBytes(KCPConnectKey connectKey, byte[] bs)
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
				OnPckBytes(connectKey, pckBs);
			}
		}
		else if(PacketHandlers.TryGetValue(dataType, out var packetHandler))
		{
			packetHandler.Invoke(connectKey, bs[1..]);
		}
		else
		{
			LogUtil.Error($"unknown packet type:{dataType}, bytes base64:{System.Convert.ToBase64String(bs)}");
		}
	}
}