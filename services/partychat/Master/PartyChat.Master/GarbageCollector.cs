using System;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class GarbageCollector
    {
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;
        private readonly TimeSpan period;

        public GarbageCollector(SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage, TimeSpan period)
        {
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
            this.period = period;
        }

        public void Start()
        {
            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        await Task.Delay(period);
                        await CollectGarbage();
                    }
                });
        }

        private async Task CollectGarbage()
        {
            var staleClients = heartbeatStorage.CollectDead();
            foreach (var client in staleClients)
            {
                var session = sessionStorage[client];
                if (session != null)
                    await session.Kill();
            }
            
            sessionStorage.CollectDead();
        }
    }
}