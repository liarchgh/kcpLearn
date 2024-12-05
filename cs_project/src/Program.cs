public class Program
{
	public static int ikcp_output(byte[] buf, int len, ref IKCPCB kcp, object user) {
		LogUtil.Info($"ikcp_output, len={len}, user={user}, conv={kcp.conv}");
		return 0; }
	public static void ikcp_writelog(string log, ref IKCPCB kcp, object user) { }
	public static void Main(string[] args)
	{
		var netThread = new Thread(TestKCP);
		netThread.Start();
		// See https://aka.ms/new-console-template for more information
		Console.WriteLine("Main done");
	}
	public static int millisecondsTimeout = 500;
	public static void TestKCP()
	{
		KCPUtil.Create(1, 1101);
		var kcpData = KCPUtil.GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		KCPUtil.SetKCPData(kcpData);
		while (true) {
			try
			{
				var begin = TimeUtil.GetTimeStamp();
				KCPUtil.Update((uint)begin);
				var end = TimeUtil.GetTimeStamp();
				var cost = end - begin;
				var sleep = millisecondsTimeout - cost;
				if(sleep > 0)
				{
					LogUtil.Info($"sleep: {sleep}");
					Thread.Sleep((int)sleep);
				}
				else
				{
					LogUtil.Error($"sleep time too long: {sleep}, cost: {cost}");
				}
			}
			catch (Exception e)
			{
				LogUtil.Error($"kcp exception, {e}");
				break;
			}
		}
		KCPUtil.Release();
	}
}

