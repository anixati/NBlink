using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Ko.NBlink.DemoApp
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("NBlink Demo 1 ..");
                using (var runtime = BuildConsoleRuntime(args))
                {
                    await runtime.RunAsync();
                    Console.WriteLine("Shutting down..");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        private static IHost BuildConsoleRuntime(string[] args)
        {
            var builder = new HostBuilder();
            builder.ConfigureAppConfiguration(sconfig =>
            {
                sconfig.SetBasePath(Path.Combine(AppContext.BaseDirectory));
                sconfig.AddJsonFile("appsettings.json", optional: true);
                sconfig.AddCommandLine(args);
            });
            builder.ConfigureServices(ConfigureServices);
            builder.ConfigureLogging((host, logBuilder) =>
            {
                logBuilder.AddConsole();
                
            });
            builder.UseConsoleLifetime();
           return builder.Build();
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection provider)
        {
            provider.AddOptions();
            provider.Configure<NBlinkSettings>(context.Configuration.GetSection("NBlinkSettings"));
            provider.AddBlinkService();
            //Optional use of embedded file server 
            provider.AddLocalHttpServer(); 
        }
    }
}
