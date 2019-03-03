using System;

namespace Ko.NBlink
{
    public class ConnectCmd : IBlinkCmd
    {
        public ConnectCmd(string wsurl)
        {
            WsUrl = new Uri(wsurl);
        }

        public Uri WsUrl { get; private set; }
    }
}