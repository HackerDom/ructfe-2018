using System.Collections.Generic;

namespace NTPTools
{
    public interface INTSourceProvider
    {
        string DefaultSource { get; }

        IEnumerable<string> GetSource();
    }
}