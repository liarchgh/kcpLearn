using System.Runtime.InteropServices;
using USER_TYPE = int;

public class Program
{
	public static string RUN_TYPE_SERVER = "server";
	public static string RUN_TYPE_CLIENT = "client";

	public static int CLIENT_PORT = 19041;
	public static int SERVER_PORT = 19042;
	public static void Main(string[] args)
	{
		LogUtil.Info(string.Join(",", args));
		var runType = args[0];
		var localPortStr = args[1];
		var remoteIP = args[2];
		var remotePort = args[3];

		if(runType == RUN_TYPE_SERVER)
		{
			NetUtil.StartServerThreads(int.Parse(localPortStr), IPPort.Parse(remoteIP, remotePort),
				(bs) =>
				{
					var dataType = (NetUtil.DATA_TYPE)bs[0];
					if(!NetUtil.PacketHandlers.TryGetValue(dataType, out var packetHandler)) return;
					packetHandler.Invoke(bs[1..]);
				}
			);
		}
		else if(runType == RUN_TYPE_CLIENT)
		{
			NetUtil.StartClientThreads(int.Parse(localPortStr), IPPort.Parse(remoteIP, remotePort));
			var fileSuffix = "f:";
			var txtSuffix = "t:";
			while (true)
			{
				var input = Console.ReadLine();
				if(input == null)
				{
					continue;
				}
				LogUtil.Info($"input:{input}");
				if(input.StartsWith(fileSuffix))
				{
					var filePath = input[fileSuffix.Length..];
					NetUtil.SendFile(filePath);
				}
				else if(input.StartsWith(txtSuffix))
				{
					NetUtil.SendText(input[txtSuffix.Length..]);
				}
			}
		}

	}
}

