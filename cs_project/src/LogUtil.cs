class LogUtil
{
	public static void Info(string msg)
	{
		_log("info", msg);
	}
	public static void Error(string msg)
	{
		_log("error", msg);
	}
	public static bool EnableDebugLog = false;
	public static void Debug(string msg)
	{
		if(!EnableDebugLog) return;
		_log("debug", msg);
	}
	private static void _log(string logType, string msg)
	{
		Console.WriteLine($"[{logType}] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}: {msg}");
	}
}