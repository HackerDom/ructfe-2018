using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class AdminServer
    {
        private readonly int port;
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;

        public AdminServer(int port, SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage)
        {
            this.port = port;
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
        }

        public void Run() => Task.Run(ServeRequests);

        private async Task ServeRequests()
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            
            listener.Start();
            
            while (true)
            {
                var client = await listener.AcceptSocketAsync();
                
                new AdminSession(new Link(client, new SilentLog()), sessionStorage, heartbeatStorage).Run();
            }
        }
    }
}