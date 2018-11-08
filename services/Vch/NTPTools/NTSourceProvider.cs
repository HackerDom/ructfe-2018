using System;
using System.Collections.Generic;
using System.Net;

namespace NTPTools
{
    public class NTSourceProvider : INTSourceProvider
    {
        public NTSourceProvider()
        {
        }

        public IPAddress DefaultSource { get; } = Dns.GetHostEntry("time.windows.com").AddressList[0];

        public IEnumerable<IPAddress> GetSource()
        {
            yield return DefaultSource;
        }
    }
}