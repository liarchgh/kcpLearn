class TimeUtil
{
	public static long GetTimeStamp()
	{
		return DateTimeOffset.Now.ToUnixTimeMilliseconds();
	}
}