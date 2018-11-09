using System;
using System.Net;
using System.Threading.Tasks;

namespace NTPTools
{
    public interface ITimeProvider
    {
	    Task<double> GetTimestamp(IPEndPoint timeProvider);
    }
}