using System;

namespace NTPTools
{
    public interface ITimeProvider
    {
        byte[] GetTime();
        byte[] GetTime(string timeProvider);
    }
}