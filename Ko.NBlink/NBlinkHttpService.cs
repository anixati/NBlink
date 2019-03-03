using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    public class NBlinkHttpService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly NBlinkSettings _settings;
        private HttpListener _listener;
        private readonly string _rootPath;
        private readonly string _rootPrefix;
        private Thread _serverThread;

        public NBlinkHttpService(ILoggerFactory loggerFactory, IOptions<NBlinkSettings> settings)
        {
            if (!HttpListener.IsSupported)
            {
                throw new ApplicationException(
                    "Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
            }

            _logger = loggerFactory.CreateLogger(this.GetType().Name);
            _settings = settings.Value;
            _rootPath = GetRootPath();
            if (!Directory.Exists(_rootPath))
            {
                throw new ApplicationException($"{_rootPath} don't exist");
            }

            _rootPrefix = GetHostAddress();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(2000);
            _serverThread = new Thread(WaitforRequests)
            {
                IsBackground = true
            };
            _serverThread.Start();
            NBlinkContext.HostAddress = _rootPrefix;
            _logger.LogInformation($"Started Http server @ {_rootPrefix}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
            }
            await Task.Delay(1);
            //_serverThread.Abort();
            _logger.LogInformation($"Stopped Http server !");
        }

        private void WaitforRequests()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_rootPrefix);
            _listener.Start();

            while (true)
            {
                var context = _listener.GetContext();
                ExecuteRequest(context);
            }
        }

        private void ExecuteRequest(HttpListenerContext context)
        {
            try
            {
                var resource = GetResourcePath(context);
                context.Response.ContentType = GetMimeHeader(Path.GetExtension(resource));
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(resource).ToString("r"));
                using (var rs = new FileStream(resource, FileMode.Open, FileAccess.Read))
                {
                    context.Response.ContentLength64 = rs.Length;
                    byte[] buffer = new byte[1024 * 16];
                    int read;
                    while ((read = rs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        context.Response.OutputStream.Write(buffer, 0, read);
                    }

                    rs.Close();
                }
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            catch (FileNotFoundException fex)
            {
                Console.WriteLine($":{fex}");
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            catch (Exception ex)
            {
                Console.WriteLine($":{ex}");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                context.Response.OutputStream.Flush();
                context.Response.OutputStream.Close();
            }
        }

        private string GetResourcePath(HttpListenerContext context)
        {
            var resource = context.Request?.Url?.AbsolutePath;
            if (string.IsNullOrEmpty(resource))
            {
                throw new Exception("empty path specified");
            }

            resource = resource.Substring(1);
            if (string.IsNullOrEmpty(resource))
            {
                resource = NBlinkContext.DefaultPage;
            }

            resource = Path.Combine(_rootPath, resource);
            if (!File.Exists(resource))
            {
                throw new FileNotFoundException(resource);
            }

            return resource.ToLower();
        }

        private string GetMimeHeader(string extention)
        {
            switch (extention)
            {
                case ".ico":
                    return "image/x-icon";

                case ".htm":
                case ".html":
                    return "text/html";

                case ".jpeg":
                case ".jpg":
                    return "image/jpeg";

                case ".png":
                    return "image/png";

                case ".js":
                    return "application/x-javascript";

                case ".css":
                    return "text/css";

                default:
                    return "application/octet-stream";
            }
        }

        private string GetHostAddress()
        {
            var port = 0;
            if (!_settings.Port.HasValue)
            {
                var tcpl = new TcpListener(IPAddress.Loopback, 0);
                tcpl.Start();
                port = ((IPEndPoint)tcpl.LocalEndpoint).Port;
                tcpl.Stop();
            }
            else
            {
                port = _settings.Port.Value;
            }
            return $"http://localhost:{port}/";
        }

        private string GetRootPath()
        {
            if (string.IsNullOrEmpty(_settings.RootPath))
            {
                return $"{AppContext.BaseDirectory}\\Web";
            }
            return _settings.RootPath;
        }
    }
}