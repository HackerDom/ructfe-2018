using System;
using System.Buffers;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class Link : IDisposable
    {
        private static readonly MemoryPool<byte> Buffers = MemoryPool<byte>.Shared;
        
        private readonly Socket client;

        private int commandId;

        private const int MaxCommandLength = 1000;

        public Link(Socket client) => this.client = client;

        public async Task<Command> SendCommand(string name, string args)
        {
            var command = new Command(name, args, commandId++);

            var line = $"{command.Id} {command.Name} {command.Args}\n";
            if (line.Length > MaxCommandLength)
                throw new InvalidOperationException("The command was too long.");

            using (var buffer = Buffers.Rent(line.Length))
            {
                var byteLength = Encoding.ASCII.GetBytes(line, buffer.Memory.Span);
                if (byteLength != line.Length)
                    throw new InvalidOperationException("Go search for a bug!");

                var dataToSend = buffer.Memory.Slice(0, byteLength);
                while (!dataToSend.IsEmpty)
                {
                    var bytesSent = await client.SendAsync(dataToSend, SocketFlags.None);
                    dataToSend = dataToSend.Slice(bytesSent);
                }
            }

            return command;
        }

        public async Task<Command> ReceiveCommand()
        {
            using (var handle = StringBuilderCache.Acquire())
            using (var buffer = Buffers.Rent())
            {
                while (true)
                {
                    var bytesReceived = await client.ReceiveAsync(buffer.Memory, SocketFlags.None);
                    if (handle.Builder.Length + bytesReceived > MaxCommandLength)
                        throw new InvalidOperationException("The command was too long.");
                    
                    var line = Encoding.ASCII.GetString(buffer.Memory.Span.Slice(0, bytesReceived));

                    var newlinePosition = line.IndexOf('\n');
                    if (newlinePosition >= 0)
                    {
                        handle.Builder.Append(line.AsSpan(0, newlinePosition));
                        break;
                    }
                    
                    handle.Builder.Append(line);
                }

                return ParseCommand(handle.Builder.ToString());
            }
        }

        private static Command ParseCommand(string s) // TODO
        {
            var parts = s.Split(' ', 3);
            
            return new Command(parts[1], parts[2], int.Parse(parts[0]));
        }

        public void Dispose() => client.Dispose();
    }
}