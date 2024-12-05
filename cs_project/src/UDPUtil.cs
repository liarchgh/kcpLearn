class UDPUtil
{
	public static Queue<byte[]> pcks = new Queue<byte[]>();
	public static void SendBytes(byte[] bytes)
	{
		pcks.Enqueue(bytes);
	}
	public delegate void PacketHandler(byte[] bytes);
	public static List<PacketHandler> packetHandlers = new List<PacketHandler>();
	public static void AddListen(PacketHandler packetHandler)
	{
		packetHandlers.Add(packetHandler);
	}
	public static void StartThreads()
	{
		var thread = new Thread(() =>
		{
			while (true)
			{
				if (pcks.TryDequeue(out var pck))
				{
					foreach (var handler in packetHandlers)
					{
						handler(pck);
					}
				}
				Thread.Sleep(2000);
			}
		});
		thread.Start();
	}
}