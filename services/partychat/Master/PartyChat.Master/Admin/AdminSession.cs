using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class AdminSession
    {
        private readonly Link link;
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;

        public AdminSession(Link link, SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage)
        {
            this.link = link;
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
        }

        public void Run() => Task.Run(ProcessCommands);

        private async Task ProcessCommands()
        {
            while (true)
            {
                var command = await link.ReceiveCommand();

                switch (command.Name)
                {
                    case "kick":
                        await link.SendCommand(new Command("-", "killing session " + command.Text, -1));
                        sessionStorage[command.Text]?.Kill();
                        break;
                    case "kill":
                        await link.SendCommand(new Command("-", "killing node " + command.Text, -1));
                        sessionStorage[command.Text]?.Kill(true);
                        break;
                    case "list":
                        foreach (var nick in sessionStorage.ListAlive())
                        {
                            await link.SendCommand(new Command("-", $"'{nick}': stable = {heartbeatStorage.IsStable(nick)}", -1));
                        }
                        break;
                    case "hist":
                        var parts = command.Text.Split('|');
                        var askWho = parts[0];
                        var askAbout = parts[1];
                        var session = sessionStorage[askWho];
                        if (session == null)
                            await link.SendCommand(new Command("-", "there's no session with " + askWho, -1));
                        else
                        {
                            var watch = Stopwatch.StartNew();
                            var response = await session.SendCommandWithResponse(Commands.History, askAbout, TimeSpan.FromMinutes(9));
                            if (response == null)
                                await link.SendCommand(new Command("-", "timed out", -1));
                            else
                            {
                                await link.SendCommand(new Command("-", "took " + watch.Elapsed, -1));
                                foreach (var line in response)
                                {
                                    await link.SendCommand(new Command("-", line, -1));
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}