using System.Net;

namespace UdpProxy
{
    public struct UdpMessage
    {
        public IPEndPoint Sender;
        public byte[] Bytes;
    }
}