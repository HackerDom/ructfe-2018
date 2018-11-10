using System;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class GarbageCollector
    {
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;
        private readonly TimeSpan period;
        private readonly ILog log;

        public GarbageCollector(SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage, TimeSpan period, ILog log)
        {
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
            this.period = period;
            this.log = log.ForContext(GetType().Name);
        }

        public void Start()
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await Task.Delay(period);
                            await CollectGarbage();
                        }
                        catch (Exception error)
                        {
                            log.Error(error);
                        }
                    }
                });
        }

        private async Task CollectGarbage()
        {
            var staleClients = heartbeatStorage.CollectDead();
            foreach (var client in staleClients)
            {
                var session = sessionStorage[client];
                
                log.Info("Killing a stale client '{nick}' at {endpoint}..", client, session?.RemoteEndpoint?.ToString() ?? "<unknown>");

                try
                {
                    if (session != null)
                        await session.Kill();
                }
                catch (Exception error)
                {
                    log.Error(error);
                    throw;
                }
            }
            
            sessionStorage.CollectDead();
        }
    }
}