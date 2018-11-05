using System.Collections;
using System.Collections.Generic;

namespace Transmitter.Morse
{
	public class StringInfiniteEnumerator : IEnumerator<char>
	{
		private readonly string str;
		private int pos;

		public StringInfiniteEnumerator(string str)
		{
			this.str = str;
			pos = str.Length - 1;
		}

		public bool MoveNext()
		{
			++pos;
			if (pos == str.Length)
				pos = 0;
			return true;
		}

		public void Reset()
		{
		}

		object IEnumerator.Current => Current;

		public char Current => str[pos];

		public void Dispose()
		{
		}
	}
}