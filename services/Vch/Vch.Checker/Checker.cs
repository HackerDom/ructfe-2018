using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
                client = new VchClient(new Uri($"http://{checkerArgs.Host}"));

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
                Console.Write(e.ToJson());
                return (int) ServiceState.CheckerError;
            }

        }

        private static IPAddress GetTimeSource()
        {
            var random = new Random();
            return new IPAddress(new byte[] {10, 10, 10, 3});
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
                var result = await client.RegisterUser(new UserMeta
                {
                    FirstName = GenerateFirstName(),
                    LastName = GenerateLastName(),
                    TrackingCode = args.Flag,
                    VaultTimeSource = args.NTP ?? new IPEndpointWrapper
                    {
                        IPAddres = GetTimeSource().ToString(),
                        Port = 123
                    }
                });

                await client.PostMessage(result.UserId, GenerateMessage(result));

                Console.Write(result.UserId);
            }
            catch (Exception e)
            {
                Console.Write(e);
                return (int) ServiceState.MUMBLE;
            }

            return (int) ServiceState.OK;
        }

        public static async Task<int> Check(CheckerArgs args)
        {
            try
            {
                await client.GetAll();
                return (int)ServiceState.OK;
            }
            catch (Exception e)
            {
                return (int) ServiceState.MUMBLE;
            }

        }

        public static async Task<int> Get(CheckerArgs args)
        {
            try
            {
                var result = await client.GetUserMessages(UInt64.Parse(args.FlagId));
                return (int)(result.All(message => message?.userInfo?.Meta?.TrackingCode == args.Flag) ? ServiceState.OK : ServiceState.Corrupt);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return (int)ServiceState.MUMBLE;
            }
        }

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
