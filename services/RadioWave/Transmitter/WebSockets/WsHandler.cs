using System.Threading.Tasks;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public class WsHandler
	{
		private readonly Channels channels;

		public WsHandler(Channels channels)
		{
			this.channels = channels;
		}

		public Task ProcessWsConnectionAsync(WebSocket ws)
		{
			var channel = ws.HttpRequest.RequestUri.AbsolutePath;
			channels.Add(channel, ws);
			return Task.CompletedTask;
		}
	}
}