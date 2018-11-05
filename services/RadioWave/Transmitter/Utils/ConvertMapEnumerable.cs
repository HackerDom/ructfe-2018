using System.Collections;
using System.Collections.Generic;

namespace Transmitter.Utils
{
	public class ConvertMapConverter<TK, TV>: IEnumerator<TV>
	{
		private readonly IDictionary<TK, TV[]> mapping;
		private readonly IEnumerator<TK> input;
		private int pos;
		private TV[] curr;

		protected ConvertMapConverter(IDictionary<TK, TV[]> mapping, IEnumerator<TK> input)
		{
			this.mapping = mapping;
			this.input = input;
			pos = int.MaxValue;
		}

		public bool MoveNext()
		{
			if (pos < curr.Length - 1)
			{
				++pos;
				return true;
			}

			if (!input.MoveNext())
				return false;

			pos = 0;
			curr = mapping.GetOrDefault(input.Current);

			return true;
		}

		public void Reset()
		{
			input.Reset();
			pos = int.MaxValue;
		}

		public TV Current
			=> curr[pos];

		object IEnumerator.Current 
			=> Current;

		public void Dispose()
		{
			input.Dispose();
		}
	}
}