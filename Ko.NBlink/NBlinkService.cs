using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    public class NBlinkService : IHostedService, IBlinkCmdHandler<ExitCmd>
    {
        private readonly IBlinkDispatcher _dispatcher;
        private readonly IBrowser _browser;
        private readonly ILogger _logger;
        private readonly NBlinkSettings _settings;
        private readonly IApplicationLifetime _appLifetime;

        public NBlinkService(IServiceProvider provider)
        {
            _appLifetime = provider.GetRequiredService<IApplicationLifetime>();
            _logger = provider.GetRequiredService<ILoggerFactory>()
                .CreateLogger(this.GetType().Name);
            _settings = provider.GetRequiredService<IOptions<NBlinkSettings>>()
                .Value;
            _dispatcher = provider.GetRequiredService<IBlinkDispatcher>();
            _browser = provider.GetRequiredService<IBrowser>();
            Initialise(provider);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1000);
            _dispatcher.Publish(new StartCmd());
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _browser.Terminate();
            await Task.Delay(1);
            _logger.LogInformation("Stopped Blink Service !");
        }

        private void Initialise(IServiceProvider provider)
        {
            var router = provider.GetRequiredService<IRoutingService>();
            router.SetupRoutes();
            AsHandler<StartCmd>(_browser);
            AsHandler<CleanUpCmd>(_browser);

            var socks = provider.GetRequiredService<ICdpService>();
            AsHandler<ConnectCmd>(socks);
            AsHandler<CdpRequest>(socks);
            AsHandler<CleanUpCmd>(socks);
            AsHandler<CdpSession>(socks);

            _dispatcher.Register(() => { return this; });
        }

        private void AsHandler<T>(object item) where T : class, IBlinkCmd
        {
            _dispatcher.Register<T>(() => { return item as IBlinkCmdHandler<T>; });
        }

        public async Task Execute(ExitCmd message, CancellationToken token)
        {
            await Task.Delay(1);
            _dispatcher.Publish(new CleanUpCmd(), token).Wait();
            _logger.LogInformation("Stopping application  ...");
            _appLifetime.StopApplication();
        }
    }
}