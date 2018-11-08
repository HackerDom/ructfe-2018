using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

#pragma warning disable 4014

namespace PartyChat.Master
{
    internal class Session
    {
        private readonly Link client;
        private readonly CommandHandler commandHandler;
        private readonly ILog log;
        private readonly BlockingQueue<Command> pendingCommands = new BlockingQueue<Command>();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<Response>> executingCommands = 
            new ConcurrentDictionary<int, TaskCompletionSource<Response>>();
        private readonly Dictionary<int, Response> responseBuffers = new Dictionary<int, Response>();
        
        private Task receiveTask;
        private Task sendTask;

        private int commandId;

        public bool IsAlive { get; private set; } = true;

        public IPEndPoint RemoteEndpoint => client.RemoteEndpoint;
        
        public Session(Link client, CommandHandler commandHandler, ILog log)
        {
            this.client = client;
            this.commandHandler = commandHandler;
            this.log = log;
        }

        public void Run()
        {
            receiveTask = Task.Run(ReceiveRoutine);
            sendTask = Task.Run(SendRoutine);
        }

        public void SendCommand(string name, string text, int id) => pendingCommands.Add(new Command(name, text, id));

        public void SendCommand(string name, string text) => SendCommand(name, text, Interlocked.Increment(ref commandId));

        public async Task<Response> SendCommandWithResponse(string name, string text, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);

            var id = Interlocked.Increment(ref commandId);
            executingCommands[id] = tcs;
            
            SendCommand(name, text, id);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));
            
            executingCommands.TryRemove(id, out _);
            
            return completedTask is Task<Response> taskWithResponse ? taskWithResponse.Result : null;
        }

        public void SendResponse(int id, string response) => SendCommand(Commands.Response, response, id);

        public void SendResponse(int id, Response response)
        {
            if (response != null)
            {
                foreach (var line in response)
                    SendResponse(id, line);
            }
            SendResponse(id, "");
        }

        public async Task Kill(bool killNode = false)
        {
            if (!IsAlive)
                return;
            
            IsAlive = false;

            try
            {
                await Task.WhenAny(EndHimRightly(killNode), Task.Delay(1000));
            
                client.Dispose();
                pendingCommands.Dispose();
            
                await receiveTask.SilentlyContinue();
                await sendTask.SilentlyContinue();
            }
            catch (Exception error)
            {
                log.Warn(error);
            }
        }

        private Task EndHimRightly(bool killNode)
        {
            var commandName = killNode ? Commands.Die : Commands.End;
            var commandText = ThreadSafeRandom.Select(TerminationMessages);

            return client.SendCommand(new Command(commandName, commandText, -1));
        }

        private async Task SendRoutine()
        {
            while (IsAlive)
            {
                try
                {
                    var command = await pendingCommands.TakeAsync();

                    await client.SendCommand(command);
                }
                catch (Exception error)
                {
                    log.Error(error);
                    throw;
                }
            }
        }

        private async Task ReceiveRoutine()
        {
            while (IsAlive)
            {
                try
                {
                    var command = await client.ReceiveCommand();

                    if (command.Name == Commands.Response)
                        HandleResponse(command.Id, command.Text);
                    else
                        Task.Run(() => commandHandler.HandleCommand(command, this));
                }
                catch (Exception error)
                {
                    log.Error(error);
                    throw;
                }
            }
        }

        private void HandleResponse(int id, string text)
        {
            const int maxResponseLines = 50;
            
            if (!executingCommands.ContainsKey(id))
                return;

            if (!responseBuffers.TryGetValue(id, out var response))
                responseBuffers[id] = response = new Response();

            if (text.Length == 0)
            {
                CommitResponse(id, response);
                return;
            }
            
            response.Add(text);

            if (response.Count >= maxResponseLines)
                CommitResponse(id, response);
        }

        private void CommitResponse(int id, Response response)
        {
            if (!executingCommands.TryGetValue(id, out var waiter))
                return;

            waiter.TrySetResult(response);
            responseBuffers.Remove(id);
        }

        private static readonly string[] TerminationMessages = {
            "I'll do you for that.",
            "Now go away or I will taunt you a second time.",
            "Ni!",
            "You make me sad.",
            "I don't want to talk to you no more, you empty headed animal food trough wiper.",
            "Now... go!"
        };
    }
}