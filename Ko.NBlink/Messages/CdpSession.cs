using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ko.NBlink
{
    public class CdpSession : IBlinkCmd
    {
        private readonly JObject _data;

        private CdpSession(JObject data)
        {
            _data = data;
        }

        public void Setup(int id, string session)
        {
            Session = session;
            _data["id"] = id;
        }

        public string Session { get; private set; }

        public string AsSerialized()
        {
            return _data.ToString(Formatting.None);
        }

        public static CdpSession Create(string method, dynamic paramsObj)
        {
            var data = JObject.FromObject(new { method });
            data["params"] = JObject.FromObject(paramsObj);
            return new CdpSession(data);
        }

        public static CdpSession Create(string method, JObject paramsObj)
        {
            var data = JObject.FromObject(new { method });
            data["params"] = paramsObj;
            return new CdpSession(data);
        }

        public static CdpSession Create(string method, string paramsStr)
        {
            var data = JObject.FromObject(new { method });
            data["params"] = JObject.Parse(paramsStr);
            return new CdpSession(data);
        }

        internal static CdpSession Create(string method)
        {
            return new CdpSession(JObject.FromObject(new { method }));
        }
    }
}