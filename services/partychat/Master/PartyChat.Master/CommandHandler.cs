using System.Collections.Generic;
using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal class CommandHandler
    {
        private readonly SessionStorage sessionStorage;
        private readonly HeartbeatStorage heartbeatStorage;

        private string nick;

        public CommandHandler(SessionStorage sessionStorage, HeartbeatStorage heartbeatStorage)
        {
            this.sessionStorage = sessionStorage;
            this.heartbeatStorage = heartbeatStorage;
        }

        public async Task HandleCommand(Command command, Session session)
        {
            Group group;
            switch (command.Name)
            {
                case Commands.Heartbeat:
                    if (!TrySetNick(command.Text) || !sessionStorage.TryRegister(nick, session))
                    {
                        await session.Kill();
                        return;
                    }

                    heartbeatStorage.RegisterHeartbeat(nick);
                    session.SendResponse(command.Id, "OK");
                    break;
                
                case Commands.Say:
                    group = Group.ExtractGroup(command.Text);
                    foreach (var member in group)
                    {
                        var memberSession = sessionStorage[member];
                        if (memberSession == null || !memberSession.IsAlive)
                            continue;
                        
                        memberSession.SendCommand(Commands.Say, command.Text);
                    }
                    break;
                
                case Commands.History:
                    group = Group.ExtractGroup(command.Text);
                    group.Add(nick);
                    var responses = new List<Response>();
                    foreach (var member in group)
                    {
                        var memberSession = sessionStorage[member];
                        if (memberSession == null || !memberSession.IsAlive)
                            continue;
                        
                        var response = await memberSession.SendCommandWithResponse(Commands.History, command.Text);
                        responses.Add(response);
                    }

                    var mergedResponse = HistoryMerger.Merge(responses);
                    session.SendResponse(command.Id, mergedResponse);
                    break;
            }
        }

        private bool TrySetNick(string value)
        {
            if (nick != null && nick != value)
                return false;

            nick = value;
            return true;
        }
    }
}