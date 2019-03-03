using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ko.NBlink
{
    internal class CdpRequest : IBlinkCmd
    {
        public readonly int _id;
        private readonly string _method;
        private readonly JObject _data;

        public CdpRequest(string method, int id = 0)
        {
            _method = method;
            _id = id;
            _data = JObject.FromObject(new { id = _id, method = _method });
        }

        public CdpRequest(string method, int id, CdpSession request)
        {
            _method = method;
            _id = id;
            _data = JObject.FromObject(new { id = _id, method = _method });
            var pjs = JObject.FromObject(new { sessionId = request.Session });
            pjs["message"] = request.AsSerialized();
            _data["params"] = pjs;
        }

        public CdpRequest Add(dynamic value)
        {
            _data["params"] = JObject.FromObject(value);
            return this;
        }

        public string AsSerialized()
        {
            return _data.ToString(Formatting.None);
        }
    }
}