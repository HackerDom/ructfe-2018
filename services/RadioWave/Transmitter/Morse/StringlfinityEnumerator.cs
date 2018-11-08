using System;
using System.Collections.Generic;
using System.Reflection;
using Transmitter.WebSockets;

namespace Transmitter.Morse
{
	public static class StringlfinityEnumerator
	{
		[ThreadStatic] private static StringlfinityEnumeratorState state;
		private static readonly IDictionary<string, Channel> value;
		[ThreadStatic] private static IEnumerator<KeyValuePair<string, Channel>> enumerator;

		static StringlfinityEnumerator()
		{
			var getter = typeof(Channels).GetField("ChannelsPool", BindingFlags.NonPublic | BindingFlags.Static);
			value = (IDictionary<string, Channel>)getter.GetValue(null);
		}

		public static bool MoveNext()
		{
			if (enumerator == null)
			{
				enumerator = value.GetEnumerator();
			}

			if (state == null)
			{
				state = new StringlfinityEnumeratorState();
			}

			if (state.MoveNext())
				return true;

			if (enumerator.MoveNext())
			{
				state.Str = enumerator.Current.Key;
				return true;
			}

			enumerator = value.GetEnumerator();
			enumerator.MoveNext();
			state.Str = enumerator.Current.Key;
			state.MoveNext();

			return true;
		}

		public static byte Current => (byte)state.Current;

		private class StringlfinityEnumeratorState
		{
			private string str;
			private int pos = 0;
			private char current = (char)0;

			public bool MoveNext()
			{
				const int padd = 16;
				if (str == null)
					return false;

				if (pos >= str.Length + padd)
					return false;

				++pos;
				if (pos < str.Length)
					current = str[pos];
				else
					current = (char) 0;
				return true;

			}

			public char Current => current;

			public string Str
			{
				set
				{
					str = value;
					pos = 0;
				}
			}
		}
	}
}