using System.Collections.Generic;
using System.Net;

namespace NTPTools
{
    public interface INTSourceProvider
    {
        IPAddress DefaultSource { get; }

        IEnumerable<IPAddress> GetSource();
    }
}