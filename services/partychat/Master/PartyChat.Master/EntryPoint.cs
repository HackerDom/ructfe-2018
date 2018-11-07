using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace PartyChat.Master
{
    internal static class EntryPoint
    {
        public static void Main(string[] args)
        {
            SetupThreadPool();
            
            var heartbeatStorage = new HeartbeatStorage(TimeSpan.FromSeconds(15));
            var sessionStorage = new SessionStorage();
            
            var server = new TcpListener(IPAddress.Any, 16770);

            server.Start(100);

            while (true)
            {
                var client = server.AcceptSocket();

                var session = new Session(new Link(client), new CommandHandler(sessionStorage, heartbeatStorage));
                session.Run();
            }
        }

        private static void SetupThreadPool()
        {
            var threads = Math.Min(Environment.ProcessorCount * 128, short.MaxValue);
            
            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(threads, threads);   
        }
    }
}
