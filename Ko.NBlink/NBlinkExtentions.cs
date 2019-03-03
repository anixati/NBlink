using Ko.NBlink.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ko.NBlink
{
    public static class NBlinkExtentions
    {
        public static void AddLocalHttpServer(this IServiceCollection services)
        {
            services.AddSingleton<IHostedService, NBlinkHttpService>();
        }

        public static void AddBlinkService(this IServiceCollection services)
        {
            services.AddSingleton<IBlinkDispatcher, BlinkDispatcher>();
            services.AddScoped<IBrowser>(sp =>
            {
                using (var scope = sp.CreateScope())
                {
                    var logfactory = sp.GetService<ILoggerFactory>();
                    var dispatcher = sp.GetService<IBlinkDispatcher>();
                    var options = sp.GetService<IOptions<NBlinkSettings>>();
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return new WinBrowser(logfactory, options, dispatcher);
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        return new WinBrowser(logfactory, options, dispatcher);
                    }

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        return new WinBrowser(logfactory, options, dispatcher);
                    }

                    throw new PlatformNotSupportedException();
                }
            });
            services.AddScoped<ICdpService, CdpService>();
            services.AddSingleton<IHostedService, NBlinkService>();
            services.AddImplementedInterfaceTypes<IController>();
            services.AddSingleton<IRoutingService, RoutingService>();
        }

        internal static ActionResult Execute(this IRoutingService handler, string name)
        {
            return handler.Execute(new ActionContext(name, 0, string.Empty));
        }

        //internal static ActionResult Execute(this IRoutingService handler, string name, string json)
        //{
        //    return handler.Execute(new ActionContext(name,0, json));
        //}

        internal static void AddImplementedInterfaceTypes<T>(this IServiceCollection svc)
        {
            svc.AddImplementedInterfaceTypes<T>(Assembly.GetEntryAssembly());
        }

        internal static void AddImplementedInterfaceTypes<T>(this IServiceCollection svc, Assembly assembly)
        {
            foreach (var ti in assembly.GetImplementedInterfaceTypes<T>())
            {
                svc.AddScoped(typeof(T), ti.AsType());
            }
        }

        internal static IEnumerable<TypeInfo> GetImplementedInterfaceTypes<T>(this Assembly assembly)
        {
            foreach (TypeInfo ti in assembly.DefinedTypes.Where(x => !x.IsAbstract &&
                                                                     x.ImplementedInterfaces.Contains(typeof(T))))
            {
                yield return ti;
            }
        }
    }
}