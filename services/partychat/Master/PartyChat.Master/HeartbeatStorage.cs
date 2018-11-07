using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PartyChat.Master
{
    internal class HeartbeatStorage
    {
        private readonly ConcurrentDictionary<string, DateTime> lastHeartbeats = new ConcurrentDictionary<string, DateTime>();
        private readonly TimeSpan keepAlive;

        public HeartbeatStorage(TimeSpan keepAlive) => this.keepAlive = keepAlive;

        public void RegisterHeartbeat(string nick) => lastHeartbeats[nick] = DateTime.UtcNow;

        public IEnumerable<string> CollectDead()
        {
            var now = DateTime.UtcNow;
            
            foreach (var pair in lastHeartbeats)
            {
                if (now - pair.Value <= keepAlive)
                    continue;
                
                yield return pair.Key;
                ((ICollection<KeyValuePair<string, DateTime>>) lastHeartbeats).Remove(pair);
            }
        }
    }
}