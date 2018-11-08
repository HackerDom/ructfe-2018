using System.Threading.Tasks;

namespace PartyChat.Master
{
    internal static class TaskExtensions
    {
        public static Task SilentlyContinue(this Task task) => task.ContinueWith(_ => {});
    }
}