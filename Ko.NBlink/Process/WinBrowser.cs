using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    internal class WinBrowser : BrowserBase, IBlinkCmdHandler<StartCmd>, IBlinkCmdHandler<CleanUpCmd>
    {
        public WinBrowser(ILoggerFactory logfactory, IOptions<NBlinkSettings> settings, IBlinkDispatcher dispatcher)
            : base(logfactory, settings, dispatcher)
        {
        }

        public async Task Execute(StartCmd message, CancellationToken token)
        {
            await Task.Delay(1);
            StartBrowserProcess();
        }

        public async Task Execute(CleanUpCmd message, CancellationToken token)
        {
            Terminate();
            await Task.Delay(1);
        }

        protected override string GetAppPath()
        {
            var rs = GetResource("loader.html");

            return @"data:text/html;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(rs));
        }

        protected override string GetBrowserPath()
        {
            return "C:/Program Files (x86)/Google/Chrome/Application/chrome.exe";
        }

        protected override string GetUserDirectory()
        {
            string rval = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(rval);
            return rval;
        }
    }
}