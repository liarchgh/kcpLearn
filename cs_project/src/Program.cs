using System.Runtime.InteropServices;
using USER_TYPE = int;

public class Program
{
	public static void Main(string[] args)
	{
		LogUtil.Info(string.Join(",", args));
		NetUtil.StartThreads();

		while (true)
		{
			var input = Console.ReadLine();
			if(input == null)
			{
				continue;
			}
			var bs = System.Text.Encoding.UTF8.GetBytes(input);
			NetUtil.SendBytes(bs);
		}
	}
}

