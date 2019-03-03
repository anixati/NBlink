using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    internal class LinuxBrowser : BrowserBase, IBlinkCmdHandler<StartCmd>, IBlinkCmdHandler<CleanUpCmd>
    {
        public LinuxBrowser(ILoggerFactory logfactory, IOptions<NBlinkSettings> settings, IBlinkDispatcher dispatcher)
            : base(logfactory, settings, dispatcher)
        {
        }

        public Task Execute(StartCmd message, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task Execute(CleanUpCmd message, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override string GetAppPath()
        {
            throw new NotImplementedException();
        }

        protected override string GetBrowserPath()
        {
            throw new NotImplementedException();
        }

        protected override string GetUserDirectory()
        {
            throw new NotImplementedException();
        }
    }
}