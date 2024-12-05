class TimeUtil
{
	public static long GetTimeStamp()
	{
		return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
	}
}