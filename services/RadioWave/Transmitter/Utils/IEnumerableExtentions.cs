using System;
using System.Collections.Generic;

namespace Transmitter.Utils
{
	public static class IEnumerableExtentions
	{
		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			if (enumerable == null || action == null)
				return;
			foreach (var item in enumerable)
				action.Invoke(item);
		}

		public static TV GetOrDefault<TK, TV>(this IDictionary<TK, TV> dict, TK key, TV def = default(TV))
		{
			if (dict == null || key == null)
				return def;
			return dict.TryGetValue(key, out var res) ? res : def;
		}

		public static TV[] GetOrDefault<TK, TV>(this IDictionary<TK, TV[]> dict, TK key, TV def = default(TV))
		{
			if(dict == null || key == null)
				return new[] { def };
			return dict.TryGetValue(key, out var res) ? res : new[] {def};
		}

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T value)
		{
			if (enumerable == null)
			{
				yield return value;
				yield break;
			}

			foreach (var val in enumerable)
				yield return val;

			yield return value;
		}

		public static IEnumerable<T> With<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (var value in enumerable)
			{
				action.Invoke(value);
				yield return value;
			}
		}
	}
}