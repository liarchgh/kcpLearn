using System.Runtime.InteropServices;
using USER_TYPE = int;

public class Program
{
	public static string RUN_TYPE_SERVER = "server";
	public static string RUN_TYPE_CLIENT = "client";
	public static void Main(string[] args)
	{
		LogUtil.Info(string.Join(",", args));
		if(args.Contains(RUN_TYPE_SERVER))
		{
			NetUtil.StartServerThreads((bs) =>
			{
				var ss = System.Text.Encoding.UTF8.GetString(bs);
				LogUtil.Info($"output:{ss}");
			});
		}
		else if(args.Contains(RUN_TYPE_CLIENT))
		{
			NetUtil.StartClientThreads();
			while (true)
			{
				var input = Console.ReadLine();
				if(input == null)
				{
					continue;
				}
				LogUtil.Info($"input:{input}");
				var bs = System.Text.Encoding.UTF8.GetBytes(input);
				NetUtil.SendBytes(bs);
			}
		}

	}
}

