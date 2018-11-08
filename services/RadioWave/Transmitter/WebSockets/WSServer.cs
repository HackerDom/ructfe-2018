using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Transmitter.Utils.Log4Net;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;

namespace Transmitter.WebSockets
{
	public class WsServer
	{
		private readonly Func<WebSocket, Task> process;
		private readonly WebSocketListener listener;

		public WsServer(ushort port, Func<WebSocket, Task> process)
		{
			var timeout = TimeSpan.FromSeconds(3);
			var rwTimeout = TimeSpan.FromMilliseconds(200);
			const int bufferSize = 1024 * 8;
			const int buffersCount = 100;

			var options = new WebSocketListenerOptions
			{
				PingTimeout = timeout,
				NegotiationTimeout = timeout,
				PingMode = PingMode.BandwidthSaving,
				ParallelNegotiations = 16,
				NegotiationQueueCapacity = 256,
				BufferManager = BufferManager.CreateBufferManager(bufferSize * buffersCount, bufferSize),
				Logger = new ILogWrapper(Log),
			};

			options.Standards.RegisterRfc6455(_ => {});

			options.Transports.ConfigureTcp(tcp =>
			{
				tcp.BacklogSize = 100; // max pending connections waiting to be accepted
				tcp.ReceiveBufferSize = bufferSize;
				tcp.SendBufferSize = bufferSize;
				tcp.ReceiveTimeout = rwTimeout;
				tcp.SendTimeout = rwTimeout;
			});

			var endpoint = new IPEndPoint(IPAddress.Loopback, port);

			listener = new WebSocketListener(endpoint, options);
			this.process = process ?? throw new ArgumentException("can't be null", nameof(process));
		}

		public async Task StartAsync()
		{
			await listener.StartAsync().ConfigureAwait(false);
			Log.Info($"Start listening {string.Join(",", listener.LocalEndpoints.Select(point => point.ToString()))}");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Run(() => AcceptLoopAsync(CancellationToken.None));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
		}

		private async Task AcceptLoopAsync(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					var ws = await listener.AcceptWebSocketAsync(token).ConfigureAwait(false);
					if (ws == null)
						continue;
					Log.Info($"{ws.RemoteEndpoint} -> {ws.HttpRequest.RequestUri}");
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
					Task.Run(() => process.Invoke(ws), token);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
				}
				catch (Exception ex)
				{
					Log.Warn($"{nameof(WsServer)}.{nameof(AcceptLoopAsync)}: something wrong while accept connect", ex);
				}
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(WsServer));
	}
}