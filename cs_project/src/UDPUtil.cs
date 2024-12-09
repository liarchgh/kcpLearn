using System.Net;
using System.Net.Sockets;
// from https://www.cnblogs.com/chxl800/p/12072751.html
public struct IPPort
{
	public IPAddress IP;
	public int Port;
	public IPPort(IPAddress ip, int port)
	{
		IP = ip;
		Port = port;
	}
	public static IPPort Parse(string ip, string port)
	{
		return new IPPort(IPAddress.Parse(ip), int.Parse(port));
	}
}
partial class UDPUtil
{
	private static UdpClient _udpClient;
	private static IPEndPoint _iPEndPointLocal;
	private static IPEndPoint _iPEndPointRemote;
	public static void Init(int localPort, IPPort iPPort)
	{
		_iPEndPointLocal = new IPEndPoint(IPAddress.Loopback, localPort);
		_iPEndPointRemote = new IPEndPoint(iPPort.IP, iPPort.Port);
		_udpClient = new UdpClient(_iPEndPointLocal);
		ThreadUtil.GenerateServiceThreadNoSleep("udp receive", TickReceiveMsg).Start();
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