using System.Net;
using System.Net.Sockets;
class UDPUtil
{
	public static Queue<byte[]> pcks = new Queue<byte[]>();
	public static void SendBytes(byte[] bytes)
	{
		pcks.Enqueue(bytes);
	}
	public delegate void PacketHandler(byte[] bytes);
	public static List<PacketHandler> packetHandlers = new List<PacketHandler>();
	public static void AddListen(PacketHandler packetHandler)
	{
		packetHandlers.Add(packetHandler);
	}
	public static void StartThreads()
	{
		var thread = new Thread(() =>
		{
			while (true)
			{
				if (pcks.TryDequeue(out var pck))
				{
					foreach (var handler in packetHandlers)
					{
						handler(pck);
					}
				}
				Thread.Sleep(2000);
			}
		});
		thread.Start();
	}
}
// from https://www.cnblogs.com/chxl800/p/12072751.html
partial class UDPClientUtil
{
	private static UdpClient _udpcSend;
	private static IPEndPoint iPEndPointLocal;
	private static IPEndPoint iPEndPointRemote;
	private static Queue<byte[]> _udpPacketsToSend = new Queue<byte[]>();
	public static void StartClientThreads(int localPort, int remotePort)
	{
		iPEndPointLocal = new IPEndPoint(IPAddress.Loopback, localPort);
		iPEndPointRemote = new IPEndPoint(IPAddress.Loopback, remotePort);
		_udpcSend = new UdpClient(iPEndPointLocal);
		NetUtil.GenerateServiceThread("udp send bytes", TickSendMsg).Start();
	}
	private static void TickSendMsg(long timestamp)
	{
		// TODO 可以直接发，UDP发是线程安全
		while(_udpPacketsToSend.TryDequeue(out var bs))
		{
			_udpcSend.Send(bs, bs.Length, iPEndPointRemote);
			LogUtil.Debug($"udp send, local:{iPEndPointLocal}, remote:{iPEndPointRemote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
		}
	}
	public static void SendByets(byte[] bs)
	{
		_udpPacketsToSend.Enqueue(bs);
	}
}
partial class UDPServerUtil
{
	private static UdpClient _udpcReceive;
	private static IPEndPoint _iPEndPointLocal;
	private static IPEndPoint _iPEndPointRemote;
	public static void StartClientThreads(int localPort)
	{
		_iPEndPointLocal = new IPEndPoint(IPAddress.Loopback, localPort);
		_udpcReceive = new UdpClient(_iPEndPointLocal);
		_iPEndPointRemote = new IPEndPoint(IPAddress.Any, 0);
		NetUtil.GenerateServiceThread("udp receive bytes", TickReceiveMsg).Start();
	}
	private static void TickReceiveMsg(long timestamp)
	{
		var bs = _udpcReceive.Receive(ref _iPEndPointRemote);
		if(bs == null || bs.Length <= 0) return;
		foreach (var handler in packetHandlers)
		{
			handler(bs);
		}
		LogUtil.Debug($"udp receive, local:{_iPEndPointLocal}, remote:{_iPEndPointRemote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
	}
	public delegate void PacketHandler(byte[] bytes);
	public static List<PacketHandler> packetHandlers = new List<PacketHandler>();
	public static void AddListen(PacketHandler packetHandler)
	{
		packetHandlers.Add(packetHandler);
	}
}