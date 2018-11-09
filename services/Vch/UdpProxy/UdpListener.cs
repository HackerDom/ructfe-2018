using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UdpProxy
{
    public class UdpListener : IDisposable
    {
        public UdpListener(int port) : this(new IPEndPoint(IPAddress.Any, port))
        {
        }

        private UdpListener(IPEndPoint endpoint)
        {
            client = new UdpClient(endpoint);
        }


        public async Task<UdpMessage> Receive()
        {
            var result = await client.ReceiveAsync();
            return new UdpMessage
            {
                Bytes = result.Buffer,
                Sender = result.RemoteEndPoint
            };
        }

        public void Reply(byte[] datagram, IPEndPoint endpoint)
        {
            client.Send(datagram, datagram.Length, endpoint);
        }

        private readonly UdpClient client;

        public void Dispose()
        {
            client.Close();
        }
    }
}