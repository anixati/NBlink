using Ko.NBlink.Handlers;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Ko.NBlink
{
    public static class CdpApi
    {
        internal const string Id = "id";
        internal const string Method = "method";
        internal const string SessionId = "sessionId";

        internal class Methods
        {
            public const string DiscoverTargets = "Target.setDiscoverTargets";
            public const string TargetAutoAttach = "Target.setAutoAttach";
            public const string TargetCreated = "Target.targetCreated";
            public const string TargetAttach = "Target.attachToTarget";
            public const string TargetDestroyed = "Target.targetDestroyed";
            public const string TargetSendMsg = "Target.sendMessageToTarget";
            public const string TargetRecvMsg = "Target.receivedMessageFromTarget";
            public const string RuntimeEval = "Runtime.evaluate";
            public const string RuntimeAddBind = "Runtime.addBinding";
            public const string RuntimeBindCalled = "Runtime.bindingCalled";
            public const string RuntimeConsoleApi = "Runtime.consoleAPICalled";
            public const string RuntimeException = "Runtime.exceptionThrown";
            public const string PageNavigate = "Page.navigate";
            public const string PageAddScript = "Page.addScriptToEvaluateOnNewDocument";
            public const string BrowserGetWinTarget = "Browser.getWindowForTarget";
            public const string BrowserSetWinBounds = "Browser.setWindowBounds";
            public const string BrowserGetWinBounds = "Browser.getWindowBounds";
        }

        internal static string TargetAutoAttachConfig = "{\"autoAttach\":true,\"waitForDebuggerOnStart\": false}";

        internal static readonly List<string> Configuration = new List<string>
            {"Page.enable", "Network.enable", "Runtime.enable", "Security.enable", "Performance.enable", "Log.enable"};

        internal static CdpRequest GetDiscoverTargetsMsg()
        {
            return new CdpRequest(Methods.DiscoverTargets, 0).Add(new { discover = true });
        }

        internal static CdpRequest GetOpenSessionMsg(string targetId)
        {
            return new CdpRequest(Methods.TargetAttach, 1).Add(new { targetId = targetId });
        }

        internal static CdpSession Navigate(string url)
        {
            return CdpSession.Create(Methods.PageNavigate, new { url });
        }

        internal static CdpSession AddBinding(string name)
        {
            return CdpSession.Create(Methods.RuntimeAddBind, new { name });
        }

        internal static CdpSession AddScript(string source)
        {
            return CdpSession.Create(Methods.PageAddScript, new { source });
        }

        internal static CdpSession SetWindowBounds(int windowId, int width, int height, int left = 100, int top = 100,
            string windowState = "normal")
        {
            return CdpSession.Create(Methods.BrowserSetWinBounds,
                new { windowId, bounds = new { left, top, width, height, windowState } });
        }

        internal static CdpSession EvalMsg(dynamic expression)
        {
            return CdpSession.Create(Methods.RuntimeEval,
                new { expression, awaitPromise = true, returnByValue = true });
        }

        public static CdpSession EvalMsg(JObject expression)
        {
            return CdpSession.Create(Methods.RuntimeEval, new JObject
            {
                ["expression"] = JObject.FromObject(expression),
                ["awaitPromise"] = true,
                ["returnByValue"] = true
            });
        }

        public static CdpSession EvalMsg(string script)
        {
            return CdpSession.Create(Methods.RuntimeEval, new JObject
            {
                ["expression"] = script,
                ["awaitPromise"] = true,
                ["returnByValue"] = true
            });
        }

        internal class Scripts
        {
            private static Regex _jscln = new Regex(@"\r\n?|\n|\t", RegexOptions.Compiled);

            internal static string GetEvalRequest(BindingResult result)
            {
                var pload = _jscln.Replace(result.Data.ToString(), string.Empty);
                return $"rs=ivf.getResolver({result.MethodId.GetValueOrDefault()},'{result.Method}').resolve(JSON.parse('{pload}'));";
            }

            internal static string GetInvokerScript()
            {
                return _jscln.Replace(_ivkscript, string.Empty);
            }

            private const string _ivkscript = @"
let ivkInstance = null;
class InvokeFactory {
      constructor() {
         if (ivkInstance) { return ivkInstance;}
         this._ivkmap = new Map();
         this.ivkInstance = this;
      }
      getInvoker(name) {
       if (!this._ivkmap.has(name)) {
               this._ivkmap.set(name, new Invoker(name));
       }
       const ivk = this._ivkmap.get(name);
       return ivk.addResolver(name);
      }
      getResolver(idx, name) {
       const ivk = this._ivkmap.get(name);
       return ivk.getResolver(idx);
      }
      Ver(){alert('1.0');}
 }
 class Invoker
 {
     constructor() {
      this._index = 0;
      this._rsmap = new Map();
     }
     addResolver(name) {
      this._index = this._index + 1;
      let rs = new Resolver(this._index, name);
      this._rsmap.set(this._index, rs);
      return rs;
     }
     getResolver(idx) {
      return this._rsmap.get(idx);
      }
 }
class Resolver
{
      constructor(idx, name) {
       this._idx = idx;
       this._name = name;
       this.promise = new Promise((resolve, reject) => {
        this.reject = reject;
        this.resolve = resolve;
       })
      }
      getIndex() {
       return this._idx;
      }
}
let ivf = new InvokeFactory();
 function bindCode(name){
 (() => {const bm = window[name];
     window[name] = (...args) => {
        let rs = ivf.getInvoker(name);
        let id = rs.getIndex();
        bm(JSON.stringify({id,args}));
        return rs.promise;
    };
})();
}";
        }
    }
}