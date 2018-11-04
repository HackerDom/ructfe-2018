using System;
using System.Collections.Generic;

namespace NTPTools
{
    public class NTSourceProvider : INTSourceProvider
    {
        public string DefaultSource { get; } = "time.windows.com";

        public IEnumerable<string> GetSource()
        {
            yield return DefaultSource;

        }
    }
}