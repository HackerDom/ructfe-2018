using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.Logging.File;
using Vostok.Logging.File.Configuration;

namespace PartyChat.Master
{
    internal static class EntryPoint
    {
        public static void Main(string[] args)
        {
            var log = CreateLog(args);
            
            SetupThreadPool();
            log.Info("Master is starting. IsServerGC = {IsServerGC}.", GCSettings.IsServerGC);

            var sessionStorage = new SessionStorage(log);
            var heartbeatStorage = new HeartbeatStorage(TimeSpan.FromSeconds(15), log);
            var garbageCollector = new GarbageCollector(sessionStorage, heartbeatStorage, TimeSpan.FromSeconds(2), log);
            
            var server = new TcpListener(IPAddress.Any, 16770);
            var adminServer = new AdminServer(16777, sessionStorage, heartbeatStorage);
            adminServer.Run();

            server.Start(100);
            garbageCollector.Start();

            while (true)
            {
                var client = server.AcceptSocket();

                var sessionLog = log.ForContext($"Session({client.RemoteEndPoint})");
                //sessionLog.Info("Accepted new client.");
                var session = new Session(new Link(client, sessionLog), new CommandHandler(sessionStorage, heartbeatStorage, sessionLog), sessionLog);
                session.Run();
            }
        }

        private static void SetupThreadPool()
        {
            var threads = Math.Min(Environment.ProcessorCount * 128, short.MaxValue);
            
            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(threads, threads);   
        }

        private static ILog CreateLog(string[] args)
        {
            var fileLog = new FileLog(new FileLogSettings
            {
                FilePath = "master.log",
                RollingStrategy = new RollingStrategyOptions
                {
                    MaxFiles = 10,
                    Period = RollingPeriod.Hour,
                    MaxSize = 10 * 1024 * 1024,
                    Type = RollingStrategyType.Hybrid
                }
            });
            
            if (args.Contains("--quiet"))
                return fileLog;
            
            return new CompositeLog(new ConsoleLog(), fileLog);
        }
    }
}
