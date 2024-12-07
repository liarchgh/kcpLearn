using System.Net;
using System.Net.Sockets;
// from https://www.cnblogs.com/chxl800/p/12072751.html
partial class UDPUtil
{
	private static UdpClient _udpClient;
	private static IPEndPoint _iPEndPointLocal;
	private static IPEndPoint _iPEndPointRemote;
	public static void Init(int localPort, int remotePort)
	{
		_iPEndPointLocal = new IPEndPoint(IPAddress.Loopback, localPort);
		_iPEndPointRemote = new IPEndPoint(IPAddress.Loopback, remotePort);
		_udpClient = new UdpClient(_iPEndPointLocal);
		NetUtil.GenerateServiceThread("udp receive", TickReceiveMsg).Start();
	}
	public static void SendByets(byte[] bs)
	{
		_udpClient.Send(bs, bs.Length, _iPEndPointRemote);
		LogUtil.Debug($"udp send, local:{_iPEndPointLocal}, remote:{_iPEndPointRemote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
	}

	private static Queue<byte[]> _pcksReceived = new Queue<byte[]>();
	public static void TickReceiveMsg(long timestamp)
	{
		var bs = _udpClient.Receive(ref _iPEndPointRemote);
		if(bs == null || bs.Length <= 0) return;
		_pcksReceived.Enqueue(bs);
		LogUtil.Debug($"udp receive, local:{_iPEndPointLocal}, remote:{_iPEndPointRemote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
	}

	public static void HandleReceiveMsg(long timestamp)
	{
		while (_pcksReceived.TryDequeue(out var bs))
		{
			foreach (var handler in packetHandlers)
			{
				handler(bs);
			}
			LogUtil.Debug($"udp handle, local:{_iPEndPointLocal}, remote:{_iPEndPointRemote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
		}
	}
	public delegate void PacketHandler(byte[] bytes);
	public static List<PacketHandler> packetHandlers = new List<PacketHandler>();
	public static void AddListen(PacketHandler packetHandler)
	{
		packetHandlers.Add(packetHandler);
	}
}