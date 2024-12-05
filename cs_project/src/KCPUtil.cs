using System.Runtime.InteropServices;
/*
a11s用unsafe接了一套，可参考着写
接口可参考：https://github.com/a11s/kcp_warpper/blob/master/kcpwarpper/KCP.cs
类可参考：https://github.com/a11s/kcp_warpper/blob/master/kcpwarpper/IKCPSEG.cs
*/

[StructLayout(LayoutKind.Sequential)]
public class IQUEUEHEAD {
	public IntPtr next, prev;
};
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int KCP_OUTPUT(byte[] buf, int len, ref IKCPCB kcp, object user);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void KCP_WRITELOG(string log, ref IKCPCB kcp, object user);
[StructLayout(LayoutKind.Sequential)]
public struct IKCPCB
{
	public uint conv, mtu, mss, state;
	public uint snd_una, snd_nxt, rcv_nxt;
	public uint ts_recent, ts_lastack, ssthresh;
	public int rx_rttval, rx_srtt, rx_rto, rx_minrto;
	public uint snd_wnd, rcv_wnd, rmt_wnd, cwnd, probe;
	public uint current, interval, ts_flush, xmit;
	public uint nrcv_buf, nsnd_buf;
	public uint nrcv_que, nsnd_que;
	public uint nodelay, updated;
	public uint ts_probe, probe_wait;
	public uint dead_link, incr;
	public IQUEUEHEAD snd_queue;
	public IQUEUEHEAD rcv_queue;
	public IQUEUEHEAD snd_buf;
	public IQUEUEHEAD rcv_buf;
	// byte*
	public IntPtr acklist;
	public uint ackcount;
	public uint ackblock;
	// ikcp_setoutput和里面output接收参数格式一致
	public int user;
	// byte*
	public IntPtr buffer;
	public int fastresend;
	public int fastlimit;
	public int nocwnd, stream;
	public int logmask;
	public KCP_OUTPUT output;
	public KCP_WRITELOG writelog;
};
public class KCPUtil
{
	private const string DLL_NAME = "kcp";
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	public static extern IntPtr ikcp_create(uint conv, object user);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	public static extern void ikcp_update(IntPtr kcp, uint current);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	public static extern void ikcp_release(IntPtr kcp);
}
