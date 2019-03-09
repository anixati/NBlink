using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    internal class WinBrowser : BrowserBase, IBlinkCmdHandler<StartCmd>, IBlinkCmdHandler<CleanUpCmd>
    {
        private const string _chromePath = @"\Google\Chrome\Application\chrome.exe";
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
            var paths = new List<string>();
            paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            paths.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            foreach (var path in paths)
            {
                var exepath = $"{path}{_chromePath}";
                if (File.Exists(exepath))
                    return exepath;
            }
            throw new Exception("Chrome not installed on the system !");
        }

        protected override string GetUserDirectory()
        {
            string rval = Path.GetFullPath(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(rval);
            return rval;
        }
    }
}