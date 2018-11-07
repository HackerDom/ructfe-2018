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
			var channel = Channel.GetChannelId(ws.HttpRequest.RequestUri);
			channels.Add(channel, ws);
			return Task.CompletedTask;
		}
	}
}