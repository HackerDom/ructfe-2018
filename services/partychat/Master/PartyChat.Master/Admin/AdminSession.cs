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
                }
            }
        }
    }
}