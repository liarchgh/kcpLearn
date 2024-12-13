using System.Net;

public partial class KCPClientUtil: KCPUtil
{
	public static KCPConnectKey MainConnectKey{ get; private set; }
	public static new KCPConnectKey Connect(IPEndPoint iPEndPoint)
	{
		MainConnectKey = KCPUtil.Connect(iPEndPoint);
		return MainConnectKey;
	}
	public static void Send(byte[] data)
	{
		_KCPConnects[MainConnectKey].Send(data);
	}
}