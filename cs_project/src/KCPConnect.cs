using System.Net;
using System.Runtime.InteropServices;
using USER_TYPE = int;
/*
from https://github.com/skywind3000/kcp
a11s用unsafe接了一套，可参考着写
接口可参考：https://github.com/a11s/kcp_warpper/blob/master/kcpwarpper/KCP.cs
类可参考：https://github.com/a11s/kcp_warpper/blob/master/kcpwarpper/IKCPSEG.cs
*/

[StructLayout(LayoutKind.Sequential)]
public struct IQUEUEHEAD {
	public IntPtr next, prev;
};
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int KCP_OUTPUT(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user);
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void KCP_WRITELOG(string log, ref IKCPCB kcp, USER_TYPE user);


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
	public USER_TYPE user;
	// byte*
	public IntPtr buffer;
	public int fastresend;
	public int fastlimit;
	public int nocwnd, stream;
	public int logmask;
	public KCP_OUTPUT output;
	public KCP_WRITELOG writelog;
};
public partial class KCPConnect
{
	private const string DLL_NAME = "kcp";
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr ikcp_create(uint conv, object user);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern void ikcp_update(IntPtr kcp, uint current);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern void ikcp_release(IntPtr kcp);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern int ikcp_input(IntPtr kcp, byte[] data, long size);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.StdCall)]
	private static extern int ikcp_send(IntPtr kcp, byte[] buffer, int len);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern int ikcp_recv(IntPtr kcp, byte[] buffer, int len);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern void ikcp_flush(IntPtr kcp);
	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	private static extern int ikcp_peeksize(IntPtr kcp);
}

public struct KCPConnectKey
{
	public IPEndPoint IPEndPoint;
	public KCPConnectKey(IPEndPoint iPEndPoint)
	{
		IPEndPoint = iPEndPoint;
	}
	public override int GetHashCode()
	{
		return IPEndPoint.GetHashCode();
	}
	public override string ToString()
	{
		return IPEndPoint.ToString();
	}
}

public partial class KCPConnect()
{
	// 不启用，直接用Socket来做区分
	private static uint CONV = 0;
	private static USER_TYPE USER = 0;
	public IntPtr KCPDataPtr = IntPtr.Zero;
	private IPEndPoint _remote = new IPEndPoint(IPAddress.Loopback, 0);
	// from https://chenqinghe.com/?p=25
	//KCP帧头8字节对齐，KCP空包大小为24字节。

	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |             conv              |  cmd  |  frg  |      wnd      |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |               ts              |               sn              |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |               una             |               len             |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	// |                                                               |
	// *                              data                             *
	// |                                                               |
	// +-------+-------+-------+-------+-------+-------+-------+-------+
	private int ikcp_output(IntPtr buf, int len, ref IKCPCB kcp, USER_TYPE user)
	{
		// from https://developer.aliyun.com/article/943678
		// 回调的话得用IntPtr，不能直接用byte[]，然后自己转bytes[]
		var bytes = new byte[len];
		Marshal.Copy(buf, bytes, 0, len);
		var cmd = bytes[4];
		var frg = bytes[5];
		LogUtil.Debug($"ikcp_output, len={len}, frg:{frg}, cmd:{cmd}, user={user}, stream:{kcp.stream}, mtu:{kcp.mtu}, mss:{kcp.mss}, state:{kcp.state}, conv={kcp.conv}");
		// , buf:{System.Text.Encoding.UTF8.GetString(bytes)}
		UDPUtil.SendByets(_remote, bytes);
		return 0;
	}
	private static void ikcp_writelog(string log, ref IKCPCB kcp, USER_TYPE user)
	{
		LogUtil.Info($"ikcp_writelog, log={log}, user={user}, conv={kcp.conv}");
	}
	public IKCPCB GetKCPData()
	{
		return Marshal.PtrToStructure<IKCPCB>(KCPDataPtr);
	}
	public void SetKCPData(IKCPCB kcpData)
	{
		Marshal.StructureToPtr(kcpData, KCPDataPtr, true);
	}
	public void Create(IPEndPoint remote)
	{
		_remote = remote;
		KCPDataPtr = ikcp_create(CONV, USER);

		var kcpData = GetKCPData();
		kcpData.output = ikcp_output;
		kcpData.writelog = ikcp_writelog;
		SetKCPData(kcpData);
	}
	public void Update(uint current)
	{
		// from https://github.com/skywind3000/kcp/wiki/KCP-Basic-Usage
		// 如 10ms调用一次，或用 ikcp_check确定下次调用 update的时间不必每次调用
		ikcp_update(KCPDataPtr, current);
	}
	public void Release()
	{
		ikcp_release(KCPDataPtr);
	}
	public void Input(byte[] data)
	{
		LogUtil.Debug($"KCPUtil.Input:{System.Text.Encoding.UTF8.GetString(data)}");
		ikcp_input(KCPDataPtr, data, data.Length);
	}

	public void Send(byte[] data)
	{
		LogUtil.Debug($"KCPUtil.Send:{System.Text.Encoding.UTF8.GetString(data)}");
		ikcp_send(KCPDataPtr, data, data.Length);
	}
	public bool TryReceive(out byte[] data)
	{
		var pckSize = ikcp_peeksize(KCPDataPtr);
		if(pckSize < 0)
		{
			data = [];
			return false;
		}
		data = new byte[pckSize];
		ikcp_recv(KCPDataPtr, data, data.Length);
		return true;
	}
	public void Flush()
	{
		ikcp_flush(KCPDataPtr);
	}
}