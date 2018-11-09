using System.Collections.Generic;

namespace PartyChat.Master
{
    internal class Response : List<string>
    {
        public Response()
        {
        }

        public Response(IEnumerable<string> collection)
            : base(collection)
        {
        }
    }
}