using Transmitter.Utils;

namespace Transmitter.Morse
{
	public static class NoiseGenerator
	{
		public static double Get()
		{
			StringlfinityEnumerator.MoveNext();
			return (StringlfinityEnumerator.Current ^ Random.Next()) / 64.0;
		}
	}
}