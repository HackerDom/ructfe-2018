using System.Threading.Tasks;
using vtortola.WebSockets;

namespace Transmitter.WebSockets
{
	public static class WsHandler
	{
		public static Task ProcessWsConnectionAsync(WebSocket ws)
		{
			var channel = Channel.GetChannelId(ws.HttpRequest.RequestUri);
			Channels.Add(channel, ws);
			return Task.CompletedTask;
		}
	}
}