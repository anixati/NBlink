using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    internal abstract class BrowserBase : IBrowser
    {
        private bool _isExitHandled;
        private static readonly Regex dtregex = new Regex(@"^DevTools listening on (ws://.*)$");
        private static readonly AutoResetEvent _cdpStart = new AutoResetEvent(false);
        private Process _process;
        protected IBlinkDispatcher Dispatcher;
        private readonly ILogger _logger;
        private readonly NBlinkSettings _settings;

        public BrowserBase(ILoggerFactory loggerFactory, IOptions<NBlinkSettings> settings, IBlinkDispatcher dispatcher)
        {
            _logger = loggerFactory.CreateLogger(this.GetType().Name);
            _settings = settings.Value;
            Dispatcher = dispatcher;
        }

        protected void StartBrowserProcess()
        {
            var stdout = new StringBuilder();
            _process = new Process
            {
                StartInfo = GetProcessInfo()
            };
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.EnableRaisingEvents = true;
            _process.OutputDataReceived += (sender, ed) => { Log(ed.Data); };
            _process.Exited += (sender, ed) =>
            {
                Log("Cdp closed");
                _isExitHandled = true;
            };

            _process.ErrorDataReceived += (sender, ed) =>
            {
                if (!string.IsNullOrEmpty(ed.Data))
                {
                    MatchCollection results = dtregex.Matches(ed.Data);
                    if (results.Count > 0)
                    {
                        var wsurl = results.First().Groups[1].Value;
                        if (string.IsNullOrEmpty(wsurl))
                        {
                            throw new Exception("Failed to get ws url");
                        }

                        Dispatcher.Publish(new ConnectCmd(wsurl));
                        _cdpStart.Set();
                    }
                }

                stdout.Append(ed.Data);
            };
            try
            {
                if (!_process.Start())
                {
                    throw new Exception("Failed to start the process");
                }

                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
                if (_cdpStart.WaitOne(new TimeSpan(0, 0, 10)))
                {
                    Log("Found WS Url");
                }
                else
                {
                    Log("timed out");
                    throw new Exception("Timed out waiting for CDP");
                }
                while (!_isExitHandled)
                {
                    Task.Delay(100);
                }
                Dispatcher.Publish(new ExitCmd());
            }
            finally
            {
                _process.CancelOutputRead();
                _process.CancelErrorRead();
                Terminate();
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            if (_process != null)
            {
                if (!_process.HasExited)
                {
                    _process.Kill();
                }
                _process.Dispose();
                _process = null;
            }
        }

        private void Log(string msg)
        {
            _logger.LogInformation(msg);
        }

        private ProcessStartInfo GetProcessInfo()
        {
            HashSet<string> args = new HashSet<string>
            {
                "--output",
                "--disable-background-mode",
                "--disable-plugins",
                "--disable-plugins-discovery",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-breakpad",
                "--disable-client-side-phishing-detection",
                "--disable-default-apps",
                "--disable-dev-shm-usage",
                "--disable-infobars",
                "--disable-extensions",
                "--disable-features=site-per-process",
                "--disable-hang-monitor",
                "--disable-ipc-flooding-protection",
                "--disable-popup-blocking",
                "--disable-prompt-on-repost",
                "--disable-renderer-backgrounding",
                "--disable-sync",
                "--disable-translate",
                "--metrics-recording-only",
                "--no-experiments",
                "--no-pings",
                "--no-first-run",
                "--safebrowsing-disable-auto-update",
                "--enable-automation",
                "--password-store=basic",
                "--use-mock-keychain",
                "--window-size=600,400",
                "--remote-debugging-port=0",
                $"--app=\"{GetAppPath()}\"",
                $"--user-data-dir={GetUserDirectory()}"
            };
            foreach (string x in GetUserArgs())
            {
                args.Add(x);
            }

            return new ProcessStartInfo(GetBrowserPath(), string.Join(" ", args.ToArray()));
        }

        protected virtual List<string> GetUserArgs()
        {
            return new List<string>();
        }

        protected abstract string GetBrowserPath();

        protected abstract string GetUserDirectory();

        protected abstract string GetAppPath();

        protected string GetResource(string fname)
        {
            var asm = typeof(BrowserBase).Assembly;
            using (var rs = asm.GetManifestResourceStream($"{asm.GetName().Name}.{fname}"))
            using (var srdr = new StreamReader(rs))
            {
                return srdr.ReadToEnd();
            }
        }
    }
}