using System;

namespace Transmitter.Morse
{
	public class NoiseGenerator
	{
		private static readonly Random GlobalRand = new Random();
		[ThreadStatic] private static Random rnd;

		public static double Get()
		{
			if (rnd == null)
			{
				lock (GlobalRand)
				{
					rnd = new Random(GlobalRand.Next());
				}
			}

			return rnd.NextDouble();
		}
	}
}