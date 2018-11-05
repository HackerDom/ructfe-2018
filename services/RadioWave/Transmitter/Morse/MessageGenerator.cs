using System.Collections;
using System.Collections.Generic;

namespace Transmitter.Morse
{
	public class MessageGenerator : IEnumerator<double>
	{
		private readonly Message message;
		private readonly StringInfiniteEnumerator textStream;
		private readonly SignalToPcmConverter pcmStream;

		public MessageGenerator(Message message, int rate)
		{
			this.message = message;
			textStream = new StringInfiniteEnumerator(message.Text);
			var morseStream = new StringToMorseConverter(textStream);
			var signalStream = new MorseToSignalConverter(morseStream);
			pcmStream = new SignalToPcmConverter(signalStream, message.Frequency, 60_000.0 / message.DPM, 1.0 / rate);
		}

		private sealed class MessageGeneratorComparer : IEqualityComparer<MessageGenerator>
		{
			public bool Equals(MessageGenerator x, MessageGenerator y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (ReferenceEquals(x, null)) return false;
				if (ReferenceEquals(y, null)) return false;
				if (x.GetType() != y.GetType()) return false;
				return Message.Comparer.Equals(x.message, y.message);
			}

			public int GetHashCode(MessageGenerator obj) 
				=> Message.Comparer.GetHashCode(obj.message);
		}

		public static readonly IEqualityComparer<MessageGenerator> Comparer = new MessageGeneratorComparer();

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