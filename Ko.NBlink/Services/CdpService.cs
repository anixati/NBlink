using Ko.NBlink.Handlers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ko.NBlink
{
    internal class CdpService : DisposableBase, ICdpService,
        IBlinkCmdHandler<ConnectCmd>, IBlinkCmdHandler<CleanUpCmd>,
        IBlinkCmdHandler<CdpRequest>, IBlinkCmdHandler<CdpSession>
    {
        private const int TimeOut = 1500;
        private const int ChunkSize = 1024;
        private Uri _cwsUrl = null;
        private readonly ClientWebSocket _cws;
        private readonly IBlinkDispatcher _dispatcher;
        private readonly IRoutingService _routeService;
        private readonly ILogger _logger;
        private readonly BlockingCollection<CdpRequest> _sendQueue = new BlockingCollection<CdpRequest>(1000);
        private readonly BlockingCollection<CdpResponse> _recvQueue = new BlockingCollection<CdpResponse>(1000);
        private readonly ConcurrentDictionary<int, SessionCallback> _cbMap = new ConcurrentDictionary<int, SessionCallback>();
        private int _methodId;
        private string _cdpTargetId = null;
        private string _cdpSessionId = null;
        private int _cdpWindowId;

        public CdpService(ILoggerFactory loggerFactory, IBlinkDispatcher dispatcher, IRoutingService routeService)
        {
            _methodId = 2;
            _logger = loggerFactory.CreateLogger(this.GetType().Name);
            _dispatcher = dispatcher;
            _routeService = routeService;
            _cws = new ClientWebSocket { Options = { KeepAliveInterval = new TimeSpan(1, 0, 0) } };
            Task.Run(() =>
            {
                foreach (var response in _recvQueue.GetConsumingEnumerable())
                {
                    HandleCdpResponse(response);
                }
            });
        }

        #region Web Sockets

        private async Task StartConnect(CancellationToken token)
        {
            if (_cws == null)
            {
                return;
            }

            if (_cws.State != WebSocketState.Open)
            {
                await _cws.ConnectAsync(_cwsUrl, token);
                Task.Run(async () =>
                {
                    while (_cws.State != WebSocketState.Open)
                    {
                        await Task.Delay(1);
                    }
                }).Wait(15000);
            }

            StartSender(token);

            StartReceiver(token);
        }

        private void StartSender(CancellationToken token)
        {
            Task.Run(async () =>
            {
                foreach (var request in _sendQueue.GetConsumingEnumerable())
                {
                    try
                    {
                        var reqMsg = request.AsSerialized();

                        Debug.WriteLine("");
                        Debug.WriteLine($"--->>> {reqMsg} ");
                        Debug.WriteLine("");
                        var sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(reqMsg));
                        await _cws.SendAsync(sendBuffer, WebSocketMessageType.Text, true, token);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Send Error");
                    }
                }
            });
        }

        private void StartReceiver(CancellationToken token)
        {
            Task.Run(async () =>
            {
                while (_cws.State == WebSocketState.Open)
                {
                    try
                    {
                        var rcvMsg = string.Empty;
                        ReadChunk:
                        var buffer = new byte[ChunkSize];
                        var rcvBuffer = new ArraySegment<byte>(buffer);
                        var result = await _cws.ReceiveAsync(rcvBuffer, token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.LogWarning("Closing ws msg received!");
                            await _cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
                            break;
                        }
                        else
                        {
                            var recBytes = rcvBuffer.Skip(rcvBuffer.Offset).Take(result.Count).ToArray();
                            if (!result.EndOfMessage)
                            {
                                rcvMsg += Encoding.UTF8.GetString(recBytes).TrimEnd('\0');
                                goto ReadChunk;
                            }
                            rcvMsg += Encoding.UTF8.GetString(recBytes).TrimEnd('\0');
                        }
                        Debug.WriteLine("");
                        Debug.WriteLine($" <<<--- {rcvMsg} ");
                        Debug.WriteLine("");
                        _recvQueue.Add(new CdpResponse(rcvMsg));
                    }
                    catch (WebSocketException wex)
                    {
                        _logger.LogError(wex, "WS Error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Receive Error");
                    }
                }
            });
        }

        private void DisConnect(CancellationToken token)
        {
            try
            {
                if (_cws.State == WebSocketState.Open)
                {
                    _cws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", token)
                           .Wait(100);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect");
            }
        }

        protected override void DisposeLocal()
        {
            if (_cws != null)
            {
                if (_cws.State == WebSocketState.Open)
                {
                    DisConnect(CancellationToken.None);
                }

                _cws.Dispose();
            }
        }

        #endregion Web Sockets

        #region Handlers

        public async Task Execute(CleanUpCmd message, CancellationToken token)
        {
            _logger.LogInformation("Cleaning up browser process ...");
            await Task.Delay(1);
            _recvQueue.CompleteAdding();
            _sendQueue.CompleteAdding();
            DisConnect(token);
        }

        public async Task Execute(ConnectCmd message, CancellationToken token)
        {
            _cwsUrl = message.WsUrl;
            await StartConnect(token);
            _logger.LogInformation($"Connected to {message.WsUrl}");
            await Send(CdpApi.GetDiscoverTargetsMsg());
        }

        #endregion Handlers

        #region Request Sender

        public async Task Execute(CdpRequest message, CancellationToken token)
        {
            await Send(message);
        }

        public async Task Execute(CdpSession message, CancellationToken token)
        {
            await Publish(message, CancellationToken.None);
        }

        private async Task Publish(CdpSession request, CancellationToken token,
            Action<CdpSession, CdpResponse> callback = null)
        {
            Interlocked.Add(ref _methodId, 1);
            request.Setup(_methodId, _cdpSessionId);
            _cbMap[_methodId] = new SessionCallback
            {
                Id = _methodId,
                Request = request,
                Callback = callback ?? HandleCallback
            };
            await Send(new CdpRequest(CdpApi.Methods.TargetSendMsg, _methodId, request));
        }

        private void Publish(string method, dynamic paramsObj, Action<CdpSession, CdpResponse> callback = null)
        {
            Publish(CdpSession.Create(method, paramsObj), CancellationToken.None, callback).Wait();
        }

        private void Publish(string method, string paramsObj, Action<CdpSession, CdpResponse> callback = null)
        {
            Publish(CdpSession.Create(method, paramsObj), CancellationToken.None, callback).Wait();
        }

        private void Publish(CdpSession message, Action<CdpSession, CdpResponse> callback = null)
        {
            Publish(message, CancellationToken.None, callback).Wait();
        }

        private async Task Send(CdpRequest message)
        {
            await Task.Run(() =>
            {
                if (_sendQueue.IsAddingCompleted)
                {
                    return;
                }

                _sendQueue.Add(message);
            });
        }

        private class SessionCallback
        {
            public int Id;
            public CdpSession Request;
            public Action<CdpSession, CdpResponse> Callback;
        }

        #endregion Request Sender

        #region Route Handling

        private void HandleCdpResponse(CdpResponse response)
        {
            if (response.IsMsgTargetPageCreated())
            {
                _cdpTargetId = response.GetStrValue("params.targetInfo.targetId");
                _logger.LogInformation($"Target Id : {_cdpTargetId}");
                Send(CdpApi.GetOpenSessionMsg(_cdpTargetId)).Wait();
                return;
            }

            if (response.IsMsgSessionCreated())
            {
                _cdpSessionId = response.GetStrValue("result.sessionId");
                _logger.LogInformation($"Session Id : {_cdpSessionId}");
                foreach (var setting in CdpApi.Configuration)
                {
                    Publish(setting, new { });
                }

                Publish(CdpApi.Methods.TargetAutoAttach, CdpApi.TargetAutoAttachConfig);
                Publish(CdpApi.Methods.BrowserGetWinTarget, new { targetId = _cdpTargetId }, (rq, rs) =>
                  {
                      rs.WithVal<int>("result.windowId", x => { _cdpWindowId = x; });
                      _logger.LogInformation($"Window Id : {_cdpWindowId}");
                      HandleActionResult(_routeService.Execute(NBlinkContext.DefaultRoute()));
                  });
                return;
            }
            if (string.IsNullOrEmpty(_cdpSessionId))
            {
                return;
            }

            if (response.IsMsgFromTargetSession(_cdpSessionId))
            {
                HandleTargetSessionMsg(response);
                return;
            }

            if (response.IsMsgTargetDestroyed(_cdpTargetId))
            {
                _logger.LogError($"{_cdpTargetId} destroyed shitting down application...");
                _dispatcher.Publish(new ExitCmd());
            }
        }

        private void HandleTargetSessionMsg(CdpResponse response)
        {
            var msg = response.GetSessionMessage();
            if (msg == null)
            {
                return;
            }

            if (msg.IsMsgRuntimeLog())
            {
                _logger.LogDebug($"Chrome Msg:  {msg}");
                return;
            }

            if (msg.IsMsgRuntimeBindCalled())
            {
                var actx = msg.GetActionContext();
                if (actx == null)
                {
                    return;
                }

                var rs = _routeService.Execute(actx);
                HandleActionResult(rs);
                return;
            }
            if (!msg.Id.HasValue)
            {
                return;
            }

            if (_cbMap.TryGetValue(msg.Id.Value, out SessionCallback cbk))
            {
                _cbMap.TryRemove(msg.Id.Value, out _);
                cbk.Callback(cbk.Request, msg);
            }
        }

        private void HandleActionResult(ActionResult result)
        {
            switch (result)
            {
                case NavigateResult nr:
                    {
                        Publish(CdpApi.Navigate(nr.Url.ToString()),
                            (pr, px) =>
                            {
                                Publish(CdpApi.SetWindowBounds(_cdpWindowId, nr.Width, nr.Height));
                                AddPageScript(CdpApi.Scripts.GetInvokerScript());
                            });
                        break;
                    }
                case BindingResult jr:
                    {
                        Publish(CdpApi.EvalMsg(CdpApi.Scripts.GetEvalRequest(jr)));
                        break;
                    }
            }
        }

        private static bool _addedInvokerScripts;

        private void AddPageScript(string script)
        {
            if (_addedInvokerScripts)
            {
                return;
            }

            Publish(CdpApi.AddScript(script), (r, rs) =>
            {
                Publish(CdpApi.EvalMsg(script), (r1, rs1) =>
                {
                    foreach (var name in _routeService.GetBindingRoutes())
                    {
                        Publish(CdpApi.AddBinding(name), (r2, rs2) => Publish(CdpApi.EvalMsg($"bindCode('{name}');")));
                    }
                    _addedInvokerScripts = true;
                });
            });
        }

        private void HandleCallback(CdpSession request, CdpResponse response)
        {
        }

        #endregion Route Handling
    }
}