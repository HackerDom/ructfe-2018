
using System.Net;

namespace UdpProxy
{
    public class ProxyRequest
    {
        public byte[] Bytes { get; set; }
        public IPEndPoint TargetHost { get; set; }
        public IPEndPoint SourceHost { get; set; }
    }
}