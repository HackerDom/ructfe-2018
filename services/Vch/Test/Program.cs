using System;
using Vch.Core.Meta;
using VchUtils;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var vchClient = new VchClient(new Uri("https://localhost:44332"));
            vchClient.RegisterUser(new UserMeta
            {
                FirstName = "fname",
                LastName = "lname",
                VaultAuthCode = "flag",
                VaultTimeProvider = "someshit"
            }).GetAwaiter().GetResult();
        }
    }
}
