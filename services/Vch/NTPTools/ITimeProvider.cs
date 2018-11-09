using System;
using System.Threading.Tasks;

namespace NTPTools
{
    public interface ITimeProvider
    {
	    Task<ulong> GetTimestamp(string timeProvider);
    }
}