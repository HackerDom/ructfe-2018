using System;
using System.Collections.Generic;
using System.Linq;

namespace PartyChat.Master
{
    internal static class HistoryMerger
    {
        public static Response Merge(IList<Response> responses)
        {
            if (responses.Count == 0)
                return null;

            return responses.Where(r => r != null).OrderByDescending(r => r.Count).FirstOrDefault();
        }
    }
}