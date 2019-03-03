using Newtonsoft.Json.Linq;
using System;

namespace Ko.NBlink
{
    public class CdpResponse
    {
        private readonly JObject _data;

        public CdpResponse(string payload)
        {
            if (!string.IsNullOrEmpty(payload))
            {
                _data = JObject.Parse(payload);
                if (HasKey(CdpApi.Id))
                {
                    Id = int.Parse(GetStrValue(CdpApi.Id));
                }

                if (HasKey(CdpApi.Method))
                {
                    Method = GetStrValue(CdpApi.Method);
                }
            }
        }

        public int? Id { get; private set; }
        public string Method { get; private set; }

        public bool HasKey(string key)
        {
            return _data.ContainsKey(key);
        }

        public JToken GetToken(string key)
        {
            return _data.SelectToken(key, errorWhenNoMatch: false);
        }

        public bool HasToken(string key)
        {
            return GetToken(key) != null;
        }

        public string GetStrValue(string key)
        {
            return GetToken(key)?.ToString();
        }

        public bool IsMethod(string key)
        {
            return string.Compare(key, Method, true) == 0;
        }

        public void WithVal<T>(string key, Action<T> action)
        {
            var val = GetToken(key);
            if (val != null)
            {
                action(val.ToObject<T>());
            }
        }

        public override string ToString()
        {
            return $"R : {_data.ToString()}";
        }
    }
}