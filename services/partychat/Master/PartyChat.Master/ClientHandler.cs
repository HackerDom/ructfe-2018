using System;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class ClientHandler
    {
        private readonly Link client;
        private readonly HeartbeatStorage heartbeatStorage;
        private readonly BlockingQueue<(string name, string args)> pendingCommands = new BlockingQueue<(string name, string args)>();
        private Task receiveTask;
        private Task sendTask;

        public bool IsAlive { get; private set; } = true;

        public ClientHandler(Link client, HeartbeatStorage heartbeatStorage)
        {
            this.client = client;
            this.heartbeatStorage = heartbeatStorage;
        }

        public void Run()
        {
            receiveTask = Task.Run(ReceiveRoutine);
            sendTask = Task.Run(SendRoutine);
        }

        public void SendCommand(string name, string args)
        {
            pendingCommands.Add((name, args));
        }

        public async Task Kill()
        {
            IsAlive = false;
            // TODO send command
            
            client.Dispose();
            pendingCommands.Dispose();
            
            await receiveTask.SilentlyContinue();
            await sendTask.SilentlyContinue();
        }

        private async Task SendRoutine()
        {
            while (IsAlive)
            {
                var command = await pendingCommands.TakeAsync();

                await client.SendCommand(command.name, command.args);
            }
        }

        private async Task ReceiveRoutine()
        {
            while (IsAlive)
            {
                var command = await client.ReceiveCommand();

                if (command.Name == "hb")
                    SendCommand("!", "OK");
            }
        }
    }
}