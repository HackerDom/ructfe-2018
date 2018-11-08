using System;

namespace NTPTools
{
    public interface ITimeProvider
    {
        double GetTimestamp();
        double GetTimestamp(string timeProvider);
    }
}