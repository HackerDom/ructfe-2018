using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transmitter.Db;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public class Channels
	{
		private readonly ConcurrentDictionary<string, Channel> channels = new ConcurrentDictionary<string, Channel>();
		private readonly int writeTimeout;
		private Timer timer;

		public Channels(int writeTimeout)
		{
			this.writeTimeout = writeTimeout;
		}

		public void Add(string channel, WebSocket ws)
		{
			channels.AddOrUpdate(channel, s => new Channel(writeTimeout, ws), (s, ch) => ch.Add(ws));
		}

		public void StartSending()
		{
			var interval = TimeSpan.FromMilliseconds(1000);
			timer = new Timer(UpdateAndSend, null, interval, interval);
		}

		private void UpdateAndSend(object state)
		{
			Task.WaitAll(channels.Select(pair => UpdateAndSendAsync(pair.Key, pair.Value)).ToArray());
		}

		private static Task UpdateAndSendAsync(string name, Channel channel)
		{
			var messages = DbClient.GetMessages(name);
			channel.UpdateMixer(messages);

			return channel.PrepareAndSendAsync();
		}
	}
}