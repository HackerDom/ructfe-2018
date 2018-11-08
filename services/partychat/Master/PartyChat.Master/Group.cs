using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PartyChat.Master
{
    internal class Group : IEnumerable<string>
    {
        private readonly List<string> members;

        private Group(List<string> members) => this.members = members;

        public static Group ExtractGroup(string message)
        {
            var tokens = message.Split(' ');
            var members = new HashSet<string>();
            foreach (var token in tokens)
            {
                if (token.Length > 0 && token[0] == '@')
                    members.Add(token);
            }

            var membersList = members.ToList();
            membersList.Sort();
            
            return new Group(membersList);
        }

        public void Add(string nick)
        {
            members.Add(nick);
            members.Sort();
        }

        public override string ToString() => string.Join(", ", members);

        public IEnumerator<string> GetEnumerator() => members.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) members).GetEnumerator();
    }
}