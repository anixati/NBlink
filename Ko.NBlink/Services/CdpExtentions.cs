using Ko.NBlink.Handlers;
using Newtonsoft.Json.Linq;

namespace Ko.NBlink
{
    internal static class CdpExtentions
    {
        internal static bool HasKeyValue(this CdpResponse response, string key, string value)
        {
            return string.Compare(response.GetStrValue(key), value, true) == 0;
        }

        internal static bool IsMsgTargetPageCreated(this CdpResponse response)
        {
            return response.IsMethod(CdpApi.Methods.TargetCreated) &&
                response.HasKeyValue("params.targetInfo.type", "page");
        }

        internal static bool IsMsgSessionCreated(this CdpResponse response)
        {
            return response.Id == 1 && response.HasToken("result.sessionId");
        }

        internal static bool IsMsgFromTargetSession(this CdpResponse response, string sessionId)
        {
            return response.IsMethod(CdpApi.Methods.TargetRecvMsg)
                   && response.HasKeyValue("params.sessionId", sessionId);
        }

        internal static bool IsMsgRuntimeLog(this CdpResponse response)
        {
            return response.IsMethod(CdpApi.Methods.RuntimeConsoleApi) ||
                   response.IsMethod(CdpApi.Methods.RuntimeException);
        }

        internal static bool IsMsgRuntimeBindCalled(this CdpResponse response)
        {
            return response.IsMethod(CdpApi.Methods.RuntimeBindCalled);
        }

        internal static bool IsMsgTargetDestroyed(this CdpResponse response, string targetId)
        {
            return response.IsMethod(CdpApi.Methods.TargetDestroyed)
                && response.HasKeyValue("params.targetId", targetId);
        }

        internal static CdpResponse GetSessionMessage(this CdpResponse response)
        {
            if (response.HasToken("params.message"))
            {
                var tkval = response.GetStrValue("params.message");
                if (!string.IsNullOrEmpty(tkval))
                {
                    return new CdpResponse(tkval);
                }
            }
            return null;
        }

        internal static ActionContext GetActionContext(this CdpResponse response)
        {
            var name = response.GetStrValue("params.name");
            var plstr = response.GetStrValue("params.payload");
            if (!string.IsNullOrEmpty(plstr))
            {
                var jo = JObject.Parse(plstr);
                if (jo.HasValues)
                {
                    var id = jo["id"].ToObject<int>();
                    return new ActionContext(name, id, jo["args"]);
                }
            }
            return null;
        }
    }
}