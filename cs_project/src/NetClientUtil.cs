using System.Net;

partial class NetClientUtil:NetUtil
{
	public static void StartClientThreads(int localPort, IPEndPoint iPPort)
	{
		UDPUtil.Init(localPort);

		KCPClientUtil.Connect(iPPort);

		UDPUtil.AddListen(KCPClientUtil.Input);

		ThreadUtil.GenerateServiceThread("kcp update", (begin) =>
		{
			KCPClientUtil.Update((uint)begin);
			UDPUtil.HandleReceiveMsg(begin);
			if(_pcksToSend.TryDequeue(out var bs))
			{
				KCPClientUtil.Send(GenKCPPck(bs.Item1, bs.Item2));
			}
		}).Start();
	}
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
		_sendBytes(KCPClientUtil.MainConnectKey, dataType, bs);
	}
}