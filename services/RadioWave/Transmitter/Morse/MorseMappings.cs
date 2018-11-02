using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transmitter.Utils;

namespace Transmitter.Morse
{
	public static class MorseMappings
	{
		public static readonly Dictionary<char, MorseChars[]> CharToMorse;
		public static readonly Dictionary<MorseChars, Signals[]> MorseToSignal;

		static MorseMappings()
		{
			var charsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mappings", "chars");

			CharToMorse = File.ReadAllLines(charsFilePath)
				.Select(s => s.Split('='))
				.ToDictionary(
					list => list[0][0],
					list => list[1]
						.Select(ParseMorse)
						.Concat(MorseChars.Delim)
						.ToArray()
				);
			CharToMorse[' '] = new [] {MorseChars.Space};

			MorseToSignal = new Dictionary<MorseChars, Signals[]>
			{
				{MorseChars.Dot, new[] {Signals.Signal, Signals.Silence}},
				{MorseChars.Dash, new[] {Signals.Signal, Signals.Signal, Signals.Signal, Signals.Silence}},
				{MorseChars.Delim, new[] {Signals.Silence, Signals.Silence}},
				{MorseChars.Space, new[] {Signals.Silence, Signals.Silence, Signals.Silence, Signals.Silence, Signals.Silence, Signals.Silence}},
			};
		}

		private static MorseChars ParseMorse(char ch)
		{
			switch (ch)
			{
				case '.':
					return MorseChars.Dot;
				case '-':
					return MorseChars.Dash;
				default:
					return MorseChars.Err;
			}
		}
	}
}