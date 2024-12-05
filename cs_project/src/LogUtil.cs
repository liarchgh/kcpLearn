class LogUtil
{
	public static void Info(string msg)
	{
		Console.WriteLine($"[info] {DateTime.Now}: {msg}");
	}
	public static void Error(string msg)
	{
		Console.WriteLine($"[error] {DateTime.Now}: {msg}");
	}
}