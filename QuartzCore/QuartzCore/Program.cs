using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz.HostedService;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace QuartzCore
{
    namespace Quartz.HostedService.Sample
    {
        internal class Program
        {
            private static int Main(string[] args)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .CreateLogger();

                try
                {
                    Log.Information("Starting host");
                    BuildHost(args).Run();
                    return 0;
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, "Host terminated unexpectedly");
                    return 1;
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }

            private static IHost BuildHost(object args) =>
                new HostBuilder()
                    .ConfigureHostConfiguration(configHost =>
                    {
                        configHost.SetBasePath(Directory.GetCurrentDirectory());
                    })
                    .ConfigureAppConfiguration((hostContext, configApp) =>
                    {
                        configApp.AddJsonFile("appsettings.json", true);
                    })
                    .ConfigureServices((hostContext, services) =>
                    {
                        services.AddQuartzHostedService(hostContext.Configuration);
                        services.AddSingleton<TestJob, TestJob>();
                    })
                    .UseConsoleLifetime()
                    .UseSerilog()
                    .Build();
        }
    }
}