using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace PartyChat.Master
{
    internal class Link : IDisposable
    {
        private static readonly MemoryPool<byte> Buffers = MemoryPool<byte>.Shared;
        
        private readonly Socket client;
        private readonly ILog log;

        private const int MaxCommandLength = 1000;
        

        public Link(Socket client, ILog log)
        {
            this.client = client;
            this.log = log.ForContext(GetType().Name);
        }

        public IPEndPoint RemoteEndpoint => client.RemoteEndPoint as IPEndPoint;

        public async Task SendCommand(Command command)
        {
            var line = $"{command.Id} {command.Name} {command.Text}\n";
            if (line.Length > MaxCommandLength)
                throw new InvalidOperationException("The command was too long.");

            log.Info("<- '{command}'", line.Substring(0, line.Length - 1));
            
            using (var buffer = Buffers.Rent(line.Length))
            {
                var byteLength = Encoding.ASCII.GetBytes(line, buffer.Memory.Span);
                if (byteLength != line.Length)
                    throw new InvalidOperationException("Go search for a bug!");

                var dataToSend = buffer.Memory.Slice(0, byteLength);
                while (!dataToSend.IsEmpty)
                {
                    var bytesSent = await client.SendAsync(dataToSend, SocketFlags.None);
                    if (bytesSent == 0)
                        throw new InvalidOperationException("0 bytes sent");
                    dataToSend = dataToSend.Slice(bytesSent);
                }
            }
        }

        public async Task<Command> ReceiveCommand()
        {
            using (var handle = StringBuilderCache.Acquire())
            using (var buffer = Buffers.Rent())
            {
                while (true)
                {
                    var bytesReceived = await client.ReceiveAsync(buffer.Memory, SocketFlags.None);
                    if (bytesReceived == 0)
                        throw new InvalidOperationException("0 bytes received");
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

                var command = handle.Builder.ToString();
                log.Info("-> '{command}'.", command);

                return ParseCommand(command);
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