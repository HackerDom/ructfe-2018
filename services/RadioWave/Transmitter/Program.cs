using System;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Transmitter.Db;
using Transmitter.Utils;
using Transmitter.Utils.Log4Net;
using Transmitter.WebSockets;

namespace Transmitter
{
	internal static class Program
	{
		private static void Main()
		{
			Log4NetHelper.Init();
			try
			{
				var settings = Settings.Load();
				MainAsync(settings).Wait();
			}
			catch (Exception ex)
			{
				Log.Fatal("Unhandled exception", ex);
				Environment.Exit(-1);
			}
		}

		private static async Task MainAsync(Settings settings)
		{
			DbClient.Init(settings.DbUri);
			var channels = new Channels(200);
			var handler = new WsHandler(channels);
			var server = new WsServer(settings.Port, handler.ProcessWsConnectionAsync);
			await server.StartAsync().ConfigureAwait(false);
			channels.StartSending();
			Thread.Sleep(Timeout.Infinite);
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
	}
}
