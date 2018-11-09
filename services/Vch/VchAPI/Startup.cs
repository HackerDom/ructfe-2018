using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NTPTools;
using Vch.Core.Meta;
using Vch.Core.Sorages;
using VchAPI.Controllers;

namespace VchAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            var client = new MongoClient(MongoUrl.Create(Configuration.GetConnectionString("mongo")));

            containerBuilder.RegisterInstance(client).As<IMongoClient>();

            containerBuilder.RegisterType<UserStorage>().As<IUserStorage>().SingleInstance();
            containerBuilder.RegisterType<MessageStorage>().As<IMessageStorage>().SingleInstance();
            containerBuilder.RegisterType<UUIDProvider>().As<IUUIDProvider>().SingleInstance();
            containerBuilder.RegisterType<TimeProvider>().As<ITimeProvider>();
            containerBuilder.RegisterType<BoardController>().PropertiesAutowired().SingleInstance();
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            app.UseMvc();
        }

        public IConfigurationRoot Configuration { get; }
    }
}