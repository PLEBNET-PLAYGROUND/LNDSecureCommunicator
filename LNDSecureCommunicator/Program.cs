using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TorHiddenServiceHelper;

namespace LNDSecureCommunicator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
                .ConfigureServices((context, services) =>
                {
                    services.AddOptions<TorHSHelperOptions>().Bind(context.Configuration.GetSection("Tor"));
                    services.AddOptions<CommunicatorSettings>().Bind(context.Configuration.GetSection("CommunicatorSettings"));
                });
    }
}
