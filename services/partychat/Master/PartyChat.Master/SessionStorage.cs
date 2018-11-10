using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Vostok.Logging.Abstractions;

#pragma warning disable 4014

namespace PartyChat.Master
{
    internal class SessionStorage
    {
        private readonly ILog log;
        private readonly ConcurrentDictionary<string, Session> sessions = new ConcurrentDictionary<string, Session>();
        private readonly ConcurrentDictionary<IPAddress, string> ipIndex = new ConcurrentDictionary<IPAddress, string>();

        public SessionStorage(ILog log) => this.log = log.ForContext(GetType().Name);

        public Session this[string nick] => sessions.TryGetValue(nick, out var session) ? session : null;

        public bool TryRegister(string nick, Session session)
        {
            if (session.RemoteEndpoint.Address.ToString().StartsWith("10.10."))
            {
                sessions[nick] = session;
                return true;
            }
            
            if (ipIndex.TryGetValue(session.RemoteEndpoint.Address, out var oldNick) && !Equals(nick, oldNick))
                return false;

            if (sessions.TryAdd(nick, session))
            {
                ipIndex[session.RemoteEndpoint.Address] = nick;
                return true;
            }

            if (!sessions.TryGetValue(nick, out var existingSession))
                return false;

            if (ReferenceEquals(existingSession, session))
                return true;
            
            if (existingSession.IsAlive && !Equals(existingSession.RemoteEndpoint.Address, session.RemoteEndpoint.Address))
                return false;

            existingSession.Kill(true);
            sessions[nick] = session;
            return true;
        }

        public void CollectDead()
        {
            foreach (var pair in sessions)
            {
                if (!pair.Value.IsAlive)
                {
                    ((ICollection<KeyValuePair<IPAddress, string>>) ipIndex)
                        .Remove(new KeyValuePair<IPAddress, string>(pair.Value.RemoteEndpoint.Address, pair.Key));
                    ((ICollection<KeyValuePair<string, Session>>) sessions).Remove(pair);
                }
            }
            foreach (var pair in sessions)
            {
                log.Info("Sessions: '{nick}': {session}", pair.Key, pair.Value);
            }
        }

        public Response ListAlive() => 
            new Response(sessions.Where(pair => pair.Value.IsAlive).Select(pair => pair.Key).OrderBy(e => e));
    }
}