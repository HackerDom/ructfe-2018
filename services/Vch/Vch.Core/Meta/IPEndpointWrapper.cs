using System.Net;

namespace Vch.Core.Meta
{
    public class IPEndpointWrapper
    {
        public IPEndPoint Endpoint() => IPAddress.TryParse(IPAddres, out var address)
            ? new IPEndPoint(address, Port)
            : null;
     
        public string IPAddres { get; set; }
        public int Port { get; set; }
    }
}