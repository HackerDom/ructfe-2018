using System.Collections.Concurrent;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public class Channels
	{
		private readonly ConcurrentDictionary<string, Channel> channels = new ConcurrentDictionary<string, Channel>();
		private readonly int writeTimeout;

		public Channels(int writeTimeout)
		{
			this.writeTimeout = writeTimeout;
		}

		public void Add(string channel, WebSocket ws)
		{
			channels.AddOrUpdate(channel, s => new Channel(channel, writeTimeout, ws), (s, ch) => ch.Add(ws));
		}
	}
}