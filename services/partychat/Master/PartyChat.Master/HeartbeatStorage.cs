using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class HeartbeatStorage
    {
        private readonly ConcurrentDictionary<string, DateTime> lastHeartbeats = new ConcurrentDictionary<string, DateTime>();
        private readonly TimeSpan keepAlive;
        private readonly ILog log;

        public HeartbeatStorage(TimeSpan keepAlive, ILog log)
        {
            this.keepAlive = keepAlive;
            this.log = log.ForContext(GetType().Name);
        }

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

        public void RemoveSession(string nick) => lastHeartbeats.TryRemove(nick, out var _);
    }
}