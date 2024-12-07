
class RandomUtil
{
	private static Random rnd = new Random();
	public static int GenerateInt(int min, int max)
	{
		return rnd.Next(min, max);
	}
}