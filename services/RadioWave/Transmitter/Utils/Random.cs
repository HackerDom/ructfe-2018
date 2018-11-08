using System;

namespace Transmitter.Utils
{
	public static class Random
	{
		private static readonly RandomInternal rnd = new RandomInternal((ulong) DateTime.UtcNow.Ticks);
		[ThreadStatic] private static RandomInternal random;

		public static ulong Next()
		{
			if (random == null)
			{
				lock (rnd)
				{
					random = new RandomInternal(rnd.Next());
				}
			}

			return random.Next();
		}

		private class RandomInternal
		{
			private readonly byte[] state;
			private byte a = 1, b = 5, c = 7;

			public RandomInternal(ulong seed)
			{
				seed ^= 0x4497bd8dcfbc24e5L;
				state = new byte[8];
				for(var i = 0; i < 64; i += 8)
					state[i] = (byte)(seed >> i);
			}

			public ulong Next()
			{
				Move();
				return state[c];
			}

			private void Move()
			{
				Move(ref a);
				Move(ref b);
				Move(ref c);
				state[c] = (byte)(state[a] + state[b]);
			}

			private void Move(ref byte pos)
			{
				++pos;
				if(pos == state.Length)
					pos = 0;
			}
		}
	}
}