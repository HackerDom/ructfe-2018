using System.Collections.Generic;
using Transmitter.Morse;

namespace Transmitter.Db
{
	public static class DbClient
	{
		public static IEnumerable<Message> GetMessages(string key)
		{
			yield return new Message {Text = "hello, world!", DPM = 120, Frequency = 2000};
			yield return new Message {Text = "RuCTF", DPM = 200, Frequency = 5000};
		}
	}
}