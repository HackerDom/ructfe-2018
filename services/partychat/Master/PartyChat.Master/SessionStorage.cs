using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PartyChat.Master
{
    internal class SessionStorage
    {
        private readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();
        
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
                if (!pair.Value.IsAlive)
                    ((ICollection<KeyValuePair<string, Session>>) sessions).Remove(pair);
            }
        }
    }
}