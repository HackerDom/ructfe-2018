using System;
using System.Threading.Tasks;

namespace NTPTools
{
    public interface ITimeProvider
    {
	    Task<double> GetTimestamp();
	    Task<double> GetTimestamp(string timeProvider);
    }
}