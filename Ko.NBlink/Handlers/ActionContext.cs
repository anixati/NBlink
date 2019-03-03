using Newtonsoft.Json.Linq;

namespace Ko.NBlink.Handlers
{
    public class ActionContext
    {
        public ActionContext(string method, int id, JToken payload)
        {
            Id = id;
            Method = method.Replace(".", "_");
            Payload = payload;
        }

        public int Id { get; private set; }
        public string Method { get; private set; }
        public JToken Payload { get; private set; }

        public int? GetMethodId()
        {
            return Id > 0 ? Id : (int?)null;
        }
    }
}