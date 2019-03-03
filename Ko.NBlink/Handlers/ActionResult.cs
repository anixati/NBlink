using Newtonsoft.Json.Linq;
using System;

namespace Ko.NBlink.Handlers
{
    public abstract class ActionResult
    {
        public int? MethodId { get; set; }
        public string Method { get; set; }
    }

    public class NavigateResult : ActionResult
    {
        public NavigateResult(Uri url)
        {
            Url = url;
        }

        public int Width { get; set; } = 800;
        public int Height { get; set; } = 800;
        public Uri Url { get; private set; }
    }

    public class BindingResult : ActionResult
    {
        public BindingResult(string payload)
        {
            Data = JObject.Parse(payload);
        }

        public BindingResult(dynamic payload)
        {
            Data = JObject.FromObject(payload);
        }

        public JObject Data { get; private set; }
    }
}