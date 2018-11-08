using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UdpProxy
{
    class Program
    {
        private static IPEndPoint[] Hosts =
        {
            new IPEndPoint(Dns.GetHostAddresses("time.windows.com").First(), 123)
        };


        static void Main(string[] args)
        {
            var server = new UdpListener(124);

            var random = new Random(Guid.NewGuid().GetHashCode());

            var requests = new BlockingCollection<ProxyRequest>(new ConcurrentStack<ProxyRequest>(), 1000);

            ThreadPool.SetMinThreads(3000, 3000);


            Enumerable.Range(1, 1000).All(i =>
            {
                HandleRequests(requests, new UdpClient(new IPEndPoint(IPAddress.Any, 0)));
                return true;
            });

            Task.Run(async () => { await ReciveRequests(server, random, requests); }).GetAwaiter().GetResult();


        }

        private static async Task ReciveRequests(UdpListener server, Random random, BlockingCollection<ProxyRequest> requests)
        {
            while (true)
            {
                try
                {

                    var udpMessage = await server.Receive();
                    var ntpHost = Hosts[random.Next(0, Hosts.Length - 1)];

                    requests.TryAdd(new ProxyRequest
                    {
                        Bytes = udpMessage.Bytes,
                        SourceHost = udpMessage.Sender,
                        TargetHost = ntpHost
                    });
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e);
                }
            }
        }

        private static async Task HandleRequests(BlockingCollection<ProxyRequest> requests, UdpClient client)
        {
            while (true)
            {
                if (requests.TryTake(out var request))
                {
                    try
                    {
                        var requestDatagramm = new byte[48];
                        var responseDatagramm = new byte[58];

                        Array.Copy(request.Bytes, requestDatagramm, Math.Min(request.Bytes.Length, 48));
                        
                        using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                        {
                            socket.Connect(request.TargetHost);
                            socket.ReceiveTimeout = 3000;

                            socket.Send(requestDatagramm);
                            socket.Receive(responseDatagramm);
                            socket.Close();
                        }

                        await client.SendAsync(responseDatagramm, responseDatagramm.Length, request.SourceHost);
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e);
                    }
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}