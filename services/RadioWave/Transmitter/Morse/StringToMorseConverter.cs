using System.Collections.Generic;
using Transmitter.Utils;

namespace Transmitter.Morse
{
	public class StringToMorseConverter : ConvertMapConverter<char, MorseChars>
	{
		public StringToMorseConverter(IEnumerator<char> input) : base(MorseMappings.CharToMorse, input)
		{
		}
	}
}