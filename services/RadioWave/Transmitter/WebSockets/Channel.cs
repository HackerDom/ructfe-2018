using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transmitter.Morse;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public class Channel
	{
		private readonly int writeTimeout;
		private readonly List<WebSocket> sockets = new List<WebSocket>();
		private readonly MixConverter mixer = new MixConverter(8000);
		private readonly byte[] buffer = new byte[8000];

		public Channel(int writeTimeout, WebSocket ws)
		{
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

		public Task PrepareAndSendAsync()
		{
			lock (this)
			{
				if (!sockets.Any())
					return Task.CompletedTask;
			}

			for (var i = 0; i < buffer.Length; i++)
			{
				mixer.MoveNext();
				buffer[i] = (byte)mixer.Current;
			}

			return SendAsync(buffer);
		}

		public void UpdateMixer(IEnumerable<Message> messages)
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

		private static Task SendAsync(WebSocket ws, byte[] message, CancellationToken token)
		{
			using (var stream = ws.CreateMessageWriter(WebSocketMessageType.Binary))
			{
				return stream.WriteAsync(message, 0, message.Length, token);
			}
		}
	}
}