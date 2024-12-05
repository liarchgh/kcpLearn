using System.Runtime.InteropServices;
public class IQUEUEHEAD {
	public IQUEUEHEAD next, prev;
};
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int KCP_OUTPUT(byte[] buf, int len, ref IKCPCB kcp, object user);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void KCP_WRITELOG(string log, ref IKCPCB kcp, object user);
public struct IKCPCB
{
	uint conv, mtu, mss, state;
	uint snd_una, snd_nxt, rcv_nxt;
	uint ts_recent, ts_lastack, ssthresh;
	int rx_rttval, rx_srtt, rx_rto, rx_minrto;
	uint snd_wnd, rcv_wnd, rmt_wnd, cwnd, probe;
	uint current, interval, ts_flush, xmit;
	uint nrcv_buf, nsnd_buf;
	uint nrcv_que, nsnd_que;
	uint nodelay, updated;
	uint ts_probe, probe_wait;
	uint dead_link, incr;
	IQUEUEHEAD snd_queue;
	IQUEUEHEAD rcv_queue;
	IQUEUEHEAD snd_buf;
	IQUEUEHEAD rcv_buf;
	UIntPtr acklist;
	uint ackcount;
	uint ackblock;
	// ikcp_setoutput和里面output接收参数格式一致
	object user;
	Byte[] buffer;
	int fastresend;
	int fastlimit;
	int nocwnd, stream;
	int logmask;
	KCP_OUTPUT output;
	KCP_WRITELOG writelog;
};
public class tt
{
	[DllImport("kcp")]
	public static extern IKCPCB ikcp_create(uint conv, object user);
}
public class Program
{
	public static void Main(string[] args)
	{
		var t = tt.ikcp_create(1, "test");
		// See https://aka.ms/new-console-template for more information
		Console.WriteLine("Hello, World!");
	}
}

