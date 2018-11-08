using System;
using System.Collections;
using System.Linq;
using NTPTools;
using Vch.Core.Meta;
using VchUtils;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var vchClient = new VchClient(new Uri("https://localhost:44332"));
            //vchClient.RegisterUser(new UserMeta
            //{
            //    FirstName = "fname",
            //    LastName = "lname",
            //    TrackingCode = "flag",
            //    VaultTimeProvider = "someshit"
            //}).GetAwaiter().GetResult();

            var a = new byte[6] {1, 1, 1, 1, 1, 1,}.Skip(4).Take(4).ToArray();

            var timeProvider = new TimeProvider(new NTSourceProvider());
            var time = timeProvider.GetTimestamp().FromUnixTimestamp(); 
        }
    }
}
