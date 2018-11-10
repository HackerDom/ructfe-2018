using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class HeartbeatStorage
    {
        private readonly ConcurrentDictionary<string, (DateTime time, int number)> lastHeartbeats = new ConcurrentDictionary<string, (DateTime time, int number)>();
        private readonly TimeSpan keepAlive;
        private readonly ILog log;

        public HeartbeatStorage(TimeSpan keepAlive, ILog log)
        {
            this.keepAlive = keepAlive;
            this.log = log.ForContext(GetType().Name);
        }

        public void RegisterHeartbeat(string nick) => lastHeartbeats.AddOrUpdate(nick, _ => (DateTime.UtcNow, 0), (k, v) => (DateTime.UtcNow, v.number + 1));

        public IEnumerable<string> CollectDead()
        {
            var now = DateTime.UtcNow;
            
            foreach (var pair in lastHeartbeats)
            {
                if (now - pair.Value.time <= keepAlive)
                    continue;
                
                yield return pair.Key;
                ((ICollection<KeyValuePair<string, (DateTime, int)>>) lastHeartbeats).Remove(pair);
            }
        }

        public void RemoveSession(string nick) => lastHeartbeats.TryRemove(nick, out var _);

        public bool IsStable(string nick) => lastHeartbeats.TryGetValue(nick, out var value) && value.number > 1;
    }
}