using System.Collections;
using System.Collections.Generic;

namespace Transmitter.Morse
{
	public class MessageGenerator : IEnumerator<double>
	{
		private readonly SignalToPcmConverter pcmStream;

		public MessageGenerator(Message message, int rate)
		{
			var textStream = new StringInfiniteEnumerator(message.Text);
			var morseStream = new StringToMorseConverter(textStream);
			var signalStream = new MorseToSignalConverter(morseStream);
			pcmStream = new SignalToPcmConverter(signalStream, message.Frequency, 60_000.0 / message.DPM, 1.0 / rate);
		}

		public bool MoveNext()
			=> pcmStream.MoveNext();

		public void Reset()
			=> pcmStream.Reset();

		public double Current
			=> pcmStream.Current;

		object IEnumerator.Current
			=> Current;

		public void Dispose()
			=> pcmStream.Dispose();
	}
}