using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class CheckerSession
    {
        private readonly Link link;
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;

        public CheckerSession(Link link, SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage)
        {
            this.link = link;
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
        }

        public void Run() => Task.Run(ProcessCommands);

        private async Task ProcessCommands()
        {
            string status;
            while (true)
            {
                var command = await link.ReceiveCommand();

                switch (command.Name)
                {
                    case "say":
                        var group = Group.ExtractGroup(command.Text);
                    
                        foreach (var member in group)
                        {
                            var memberSession = sessionStorage[member];
                            if (memberSession == null || !memberSession.IsAlive)
                                continue;
                        
                            memberSession.SendCommand(Commands.Say, command.Text);
                        }
                        break;
                    case "hb":
                        status = sessionStorage[command.Text] != null && heartbeatStorage.IsStable(command.Text) ? "ok" : "not ok";
                        await link.SendCommand(new Command("!", status, 0));
                        break;
                    case "history":
                        var parts = command.Text.Split('|');
                        var askWho = parts[0];
                        var askAbout = parts[1];
                        var flag = parts[2];
                        var session = sessionStorage[askWho];
                        if (session == null)
                            await link.SendCommand(new Command("!", "not ok", 0));
                        else
                        {
                            var response = await session.SendCommandWithResponse(Commands.History, askAbout, TimeSpan.FromSeconds(20));
                            status = response.Any(e => e.Contains(flag)) ? "ok" : "not ok";
                            await link.SendCommand(new Command("!", status, 0));
                        }
                        break;
                }
            }
        }
    }
}