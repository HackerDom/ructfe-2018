using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NTPTools;
using Vch.Core.Helpers;
using Vch.Core.Meta;
using VchUtils;

namespace Vch.Checker
{
    class Checker
    {
        static int Main(string[] args)
        {
            var checkerArgs = new CheckerArgs(args);

            if (!checkerArgs.Mode.Equals("info", StringComparison.InvariantCultureIgnoreCase)) 
            {
                client = new VchClient(new Uri($"http://{checkerArgs.Host}:19999"));
            }

            var modes = new Dictionary<string, Func<CheckerArgs, Task<int>>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "info", Info},
                { "check", Check},
                { "put", Put},
                { "get", Get},
            };

            try
            {
                return modes[checkerArgs.Mode](checkerArgs).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.Write(e);
                return (int) ServiceState.CheckerError;
            }
        }

        private static IEnumerable<IPAddress> GetTimeSource()
        {
            for (int index = 1; index < 352; index++)
            {
                yield return IPAddress.Parse($"10.{60 + index / 256}.{index % 256}.1");
            }
        }

        private static IPAddress GetRandomAlive()
        {
            var random = new Random();
            var shuffledIp = GetTimeSource().OrderBy(address => random.Next()).ToArray();

            for (int i = 0; i < 35; i+=10)
            {
                var checkTasks = new List<Task<IPAddress>>();
                for (int j = 0; j < 10; j++)
                {
                    var task = CheckIp(shuffledIp[i * j]);
                    checkTasks.Add(task);
                }
                Task.WhenAll(checkTasks).GetAwaiter().GetResult();
                var result = checkTasks.Where(task1 => task1.Result != null).ToArray();
                if (result.Any()) return result.First().Result;
            }
            return null;
        }

        private static async Task<IPAddress> CheckIp(IPAddress ipAddress)
        {
            try
            {
                var ntpClient = new TimeProvider();
                var result = await ntpClient.GetNetworkTime(new IPEndPoint(ipAddress, 123));
                return ipAddress;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private static string GenerateMessage(UserInfo info)
        {
            return "message";
        }

        private static string GenerateFirstName()
        {
            return "Vault supervisor";
        }

        private static string GenerateLastName()
        {
            return "1.0.0";
        }

        public static async Task<int> Info(CheckerArgs args)
        {
            Console.Write("1");
            return (int) ServiceState.OK;
        }

        public static async Task<int> Put(CheckerArgs args)
        {
            try
            {
                var useCustom=  DateTime.Now.Millisecond % 3 == 0;
                var result = await client.RegisterUser(new UserMeta
                {
                    FirstName = GenerateFirstName(),
                    LastName = GenerateLastName(),
                    TrackingCode = args.Flag,
                    VaultTimeSource = args.NTP ?? 
                    new IPEndpointWrapper
                    {
                        IPAddres = (useCustom ? GetRandomAlive() ?? defaultIp : defaultIp).ToString(),
                        Port = 123
                    }
                });

                await client.PostMessage(result.UserId, GenerateMessage(result));

                Console.Write(result.UserId);
            }
            catch (VchClient.InternalServerError e)
            {
                Console.Write(e);
                return (int) ServiceState.MUMBLE;
            }
            catch (VchClient.ConntectionFailed e)
            {
                return (int)ServiceState.DOWN;
            }
            catch (Exception e)
            {
                Console.Write("Put  |"  + e);
                return (int)ServiceState.CheckerError;
            }

            return (int) ServiceState.OK;
        }

        public static async Task<int> Check(CheckerArgs args)
        {
            try
            {
                await client.GetAll();
                var ntpClient = new TimeProvider();

                try
                {
                   await ntpClient.GetNetworkTime(new IPEndPoint(IPAddress.Parse(args.Host), 123));
                }
                catch (Exception e)
                {
                    return (int) ServiceState.DOWN;
                }

                return (int)ServiceState.OK;
            }
            catch (VchClient.InternalServerError e)
            {
                return (int) ServiceState.MUMBLE;
            }
            catch (VchClient.ConntectionFailed e)
            {
                return (int)ServiceState.DOWN;
            }
            catch (Exception e)
            {
                Console.Write("Check  |" + e);
                return (int)ServiceState.CheckerError;
            }
        }

        public static async Task<int> Get(CheckerArgs args)
        {
            try
            {
                var result = await client.GetUserMessages(UInt64.Parse(args.FlagId));
               return  (int) (result.Any() && result.All(message => message?.userInfo?.Meta?.TrackingCode == args.Flag)
                    ? ServiceState.OK
                    : ServiceState.Corrupt);
            }
            catch (VchClient.InternalServerError e)
            {
                return (int) ServiceState.MUMBLE;
            }
            catch (VchClient.ConntectionFailed e)
            {
                return (int) ServiceState.DOWN;
            }
            catch (Exception e)
            {
                Console.Write("Get  |" + e);
                return (int) ServiceState.CheckerError;
            }
        }

        private static IPAddress defaultIp = new IPAddress(new byte[] {10, 10, 10, 10});
        private static VchClient client;

        internal class CheckerArgs
        {
            public CheckerArgs(string[] args)
            {
                this.args = args;
            }

            private readonly string[] args;
            public string Mode => args[0];
            public string Host => args[1];
            public string FlagId => args[2];
            public string Flag => args[3];
            public IPEndpointWrapper NTP => args.Length > 3  ?
                new IPEndpointWrapper{Port = args.Length > 4 ? int.Parse(args[5]) : 123, IPAddres = args[4] } : null;
        }

        enum ServiceState
        {
            OK = 101,
            Corrupt = 102,
            MUMBLE = 103,
            DOWN = 104,
            CheckerError = 110
        }
    }
}
