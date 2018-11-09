using System;
using System.Threading;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace VchAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
            SetupThreadPool();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddAutofac())
                .UseStartup<Startup>()
                .UseKestrel(options => 
                    options.ListenAnyIP(19999)
                );

        private static void SetupThreadPool()
        {
            var threads = Math.Min(Environment.ProcessorCount * 128, short.MaxValue);

            ThreadPool.SetMaxThreads(short.MaxValue, short.MaxValue);
            ThreadPool.SetMinThreads(threads, threads);
        }
    }
}