using System.Net;

public partial class KCPUtil
{
	public static int FRG_MAX = 127;
	public static Dictionary<KCPConnectKey, KCPConnect> _KCPConnects = new Dictionary<KCPConnectKey, KCPConnect>();
	public static bool TryGetKCPData(KCPConnectKey connectKey, out IKCPCB kcpData)
	{
		if(_KCPConnects.TryGetValue(connectKey, out var kcp))
		{
			kcpData = kcp.GetKCPData();
			return true;
		}
		kcpData = new IKCPCB();
		return false;
	}
	public static KCPConnectKey Connect(IPEndPoint iPEndPoint)
	{
		var connectKey = new KCPConnectKey(iPEndPoint);
		var kcp = new KCPConnect();
		kcp.Create(iPEndPoint);
		_KCPConnects.Add(connectKey, kcp);
		return connectKey;
	}
	public static void Update(uint current)
	{
		foreach(var kcp in _KCPConnects)
		{
			kcp.Value.Update(current);
		}
	}
	public void Release(KCPConnectKey connectKey)
	{
		_KCPConnects[connectKey].Release();
		_KCPConnects.Remove(connectKey);
	}
	public static void Input(IPEndPoint iPEndPoint, byte[] data)
	{
		var connectKey = new KCPConnectKey(iPEndPoint);
		if(!_KCPConnects.ContainsKey(connectKey))
		{
			Connect(iPEndPoint);
		}
		_KCPConnects[connectKey].Input(data);
	}

	public static void Send(KCPConnectKey connectKey, byte[] data)
	{
		_KCPConnects[connectKey].Send(data);
	}
	public static bool TryReceive(out KCPConnectKey connectKey, out byte[] data)
	{
		foreach (var kcp in _KCPConnects)
		{
			if (kcp.Value.TryReceive(out data))
			{
				connectKey = kcp.Key;
				return true;
			}
		}
		connectKey = new KCPConnectKey();
		data = null;
		return false;
	}
	public static void Flush(KCPConnectKey connectKey)
	{
		_KCPConnects[connectKey].Flush();
	}
}