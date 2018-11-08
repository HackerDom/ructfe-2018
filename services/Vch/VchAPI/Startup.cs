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
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            var client = new MongoClient(MongoUrl.Create("mongodb://localhost"));

            containerBuilder.RegisterInstance(client).As<IMongoClient>();

            containerBuilder.RegisterType<UserStorage>().As<IUserStorage>();
            containerBuilder.RegisterType<MessageStorage>().As<IMessageStorage>();
            containerBuilder.RegisterType<UUIDProvider>().As<IUUIDProvider>();
            containerBuilder.RegisterType<TimeProvider>().As<ITimeProvider>();
            containerBuilder.RegisterType<NTSourceProvider>().As<INTSourceProvider>().SingleInstance();
            containerBuilder.RegisterType<BoardController>().PropertiesAutowired();
        }
        

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(this.Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            app.UseMvc();
        }

        public IConfigurationRoot Configuration { get; }
    }
}