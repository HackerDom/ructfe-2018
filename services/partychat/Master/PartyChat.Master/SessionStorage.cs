using System.Collections.Concurrent;
using System.Collections.Generic;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class SessionStorage
    {
        private readonly ILog log;
        private readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();

        public SessionStorage(ILog log) => this.log = log.ForContext(GetType());

        public Session this[string nick] => sessions.TryGetValue(nick, out var session) ? session : null;

        public bool TryRegister(string nick, Session session)
        {
            if (sessions.TryAdd(nick, session))
                return true;

            if (!sessions.TryGetValue(nick, out var existingSession))
                return false;

            if (!Equals(existingSession.RemoteEndpoint, session.RemoteEndpoint))
                return false;

            sessions[nick] = session;
            return true;
        }

        public void CollectDead()
        {
            foreach (var pair in sessions)
            {
                log.Info("CollectDead: nick '{nick}', session: {session}.", pair.Key, pair.Value);
                if (!pair.Value.IsAlive)
                    ((ICollection<KeyValuePair<string, Session>>) sessions).Remove(pair);
            }
        }
    }
}