using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Transmitter.Db;
using Transmitter.Morse;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public class Channel
	{
		private readonly string channelId;
		private readonly int writeTimeout;
		private readonly List<WebSocket> sockets = new List<WebSocket>();
		private readonly MixConverter mixer = new MixConverter(8000);
		private readonly byte[] buffer = new byte[8000];
		private Task<List<Message>> getMessagesTask;

		public Channel(string channelId, int writeTimeout, WebSocket ws)
		{
			this.channelId = channelId;
			this.writeTimeout = writeTimeout;
			sockets.Add(ws);
		}

		public Channel Add(WebSocket ws)
		{
			lock (this)
			{
				sockets.Add(ws);
			}

			return this;
		}

		public async Task PrepareAndSendAsync()
		{
			lock (this)
			{
				if (!sockets.Any())
				{
					Log.Info($"[{channelId}]: no clients");
					return;
				}
			}

			var sw = Stopwatch.StartNew();

			if(getMessagesTask == null)
				getMessagesTask = Task.Run(() => DbClient.GetMessagesAsync(channelId));

			if(getMessagesTask.IsCompleted)
			{
				if(getMessagesTask.Status == TaskStatus.RanToCompletion)
					UpdateMixer(getMessagesTask.Result);
				getMessagesTask = null;
			}

			for (var i = 0; i < buffer.Length; i++)
			{
				mixer.MoveNext();
				buffer[i] = (byte)mixer.Current;
			}

			await SendAsync(buffer).ConfigureAwait(false);

			Log.Info($"[{channelId}]: send all, elapsed {sw.Elapsed}");
		}

		private void UpdateMixer(IEnumerable<Message> messages)
			=> mixer.Sync(messages);

		private async Task<bool> SendAsync(byte[] message)
		{
			List<Task> tasks;
			var tokenSource = new CancellationTokenSource();
			lock(this)
			{
				sockets.RemoveAll(socket => !socket.IsConnected);
				if (!sockets.Any())
					return false;
				tasks = sockets.Select(socket => SendAsync(socket, message, tokenSource.Token)).ToList();
			}

			tokenSource.CancelAfter(writeTimeout);
			await Task.WhenAll(tasks).ConfigureAwait(false);
			return true;
		}

		private async Task SendAsync(WebSocket ws, byte[] message, CancellationToken token)
		{
			using (var stream = ws.CreateMessageWriter(WebSocketMessageType.Binary))
			{
				var sw = Stopwatch.StartNew();
				await stream.WriteAsync(message, 0, message.Length, token).ConfigureAwait(false);
				Log.Info($"[{channelId}]: send to {ws.RemoteEndpoint} {message.Length} bytes, elapsed {sw.Elapsed}");
			}
		}

		public static string GetChannelId(Uri uri)
			=> uri.IsAbsoluteUri ? uri.AbsolutePath : uri.ToString().TrimStart('/');

		private static readonly ILog Log = LogManager.GetLogger(typeof(Channel));
	}
}