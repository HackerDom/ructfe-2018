using System.Collections.Concurrent;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public static class Channels
	{
		private static readonly ConcurrentDictionary<string, Channel> ChannelsPool = new ConcurrentDictionary<string, Channel>();
		public static int WriteTimeout;

		public static void Add(string channel, WebSocket ws)
		{
			ChannelsPool.AddOrUpdate(channel, s => new Channel(channel, WriteTimeout, ws), (s, ch) => ch.Add(ws));
		}
	}
}