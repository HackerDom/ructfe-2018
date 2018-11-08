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

			AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

			var settings = Settings.Load();
			MainAsync(settings).Wait();
		}

		private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = e.ExceptionObject as Exception;
			Console.Error.WriteLine("Unhandled exception");
			Console.Error.WriteLine(exception);
			Log.Fatal("Unhandled exception", exception);
			Environment.Exit(-1);
		}

		private static async Task MainAsync(Settings settings)
		{
			DbClient.Init(settings.DbUri);
			Channels.WriteTimeout = 200;
			var server = new WsServer(settings.Port, WsHandler.ProcessWsConnectionAsync);
			await server.StartAsync().ConfigureAwait(false);
			Thread.Sleep(Timeout.Infinite);
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
	}
}
