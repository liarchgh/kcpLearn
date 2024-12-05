using System.Runtime.InteropServices;

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
		Console.WriteLine("Hello, World!");
	}
	public static int millisecondsTimeout = 500;
	public static void TestKCP()
	{
		var kcpDataPtr = KCPUtil.ikcp_create(1, 33);
		var kcpData = Marshal.PtrToStructure<IKCPCB>(kcpDataPtr);
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		while (true) {
			try
			{
				var begin = TimeUtil.GetTimeStamp();
				KCPUtil.ikcp_update(kcpDataPtr, (uint)begin);
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
		KCPUtil.ikcp_release(kcpDataPtr);
	}
}

