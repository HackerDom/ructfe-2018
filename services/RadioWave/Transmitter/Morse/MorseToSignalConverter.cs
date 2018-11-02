using System.Collections.Generic;
using Transmitter.Utils;

namespace Transmitter.Morse
{
	public class MorseToSignalConverter : ConvertMapConverter<MorseChars, Signals>
	{
		public MorseToSignalConverter(IEnumerator<MorseChars> input) : base(MorseMappings.MorseToSignal, input)
		{
		}
	}
}