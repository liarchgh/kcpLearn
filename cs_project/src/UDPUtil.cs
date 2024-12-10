using System.Net;
using System.Net.Sockets;
partial class UDPUtil
{
	private static UdpClient _udpClient;
	private static IPEndPoint _iPEndPointLocal;
	public static void Init(int localPort)
	{
		_iPEndPointLocal = new IPEndPoint(IPAddress.Loopback, localPort);
		while(true)
		{
			try
			{
				_udpClient = new UdpClient(_iPEndPointLocal);
				LogUtil.Info($"game is on port:{_iPEndPointLocal}");
				break;
			}
			catch (SocketException e)
			{
				LogUtil.Debug($"port is in used, port:{localPort}, exception:{e}");
				++_iPEndPointLocal.Port;
				continue;
			}
		}
		ThreadUtil.GenerateServiceThreadNoSleep("udp receive", TickReceiveMsg).Start();
	}
	public static void SendByets(IPEndPoint remote, byte[] bs)
	{
		_udpClient.Send(bs, bs.Length, remote);
		LogUtil.Debug($"udp send, local:{_iPEndPointLocal}, remote:{remote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
	}
	// from https://www.cnblogs.com/chxl800/p/12072751.html
	public static IPEndPoint ParseIPEndPort(string ip, string port)
	{
		return new IPEndPoint(IPAddress.Parse(ip), int.Parse(port));
	}

	private class PckReceiveInfo
	{
		public byte[] Data;
		public IPEndPoint Remote;
		public PckReceiveInfo(byte[] bs, IPEndPoint remote)
		{
			Data = bs;
			Remote = remote;
		}
	}
	private static Queue<PckReceiveInfo> _pcksReceived = new Queue<PckReceiveInfo>();
	public static void TickReceiveMsg(long timestamp)
	{
		IPEndPoint remote = null;
		var bs = _udpClient.Receive(ref remote);
		if(bs == null || bs.Length <= 0 || remote == null) return;
		var newPckInfo = new PckReceiveInfo(bs, remote);
		_pcksReceived.Enqueue(newPckInfo);
		LogUtil.Debug($"udp receive, local:{_iPEndPointLocal}, remote:{remote}, bytes:{System.Text.Encoding.UTF8.GetString(bs)}");
	}

	public static void HandleReceiveMsg(long timestamp)
	{
		while (_pcksReceived.TryDequeue(out var pckInfo))
		{
			foreach (var handler in _packetHandlers)
			{
				handler(pckInfo.Remote, pckInfo.Data);
			}
			LogUtil.Debug($"udp handle, local:{_iPEndPointLocal}, remote:{pckInfo.Remote}");
		}
	}
	public delegate void PacketHandler(IPEndPoint source, byte[] bytes);
	private static List<PacketHandler> _packetHandlers = new List<PacketHandler>();
	public static void AddListen(PacketHandler packetHandler)
	{
		_packetHandlers.Add(packetHandler);
	}
}