using System;

namespace Ko.NBlink
{
    public static class NBlinkContext
    {
        public const string DefaultPage = "index.html";

        public static string HostAddress { get; internal set; }

        public static Uri DefaultUrl()
        {
            return ResolveUrl(DefaultPage);
        }

        public static Uri ResolveUrl(string urlstr)
        {
            return new Uri($"{HostAddress}{urlstr}");
        }

        public static string ControllerName { get; internal set; } = "Home";
        public static string ActionName { get; internal set; } = "Index";

        public static string DefaultRoute()
        {
            return $"{ControllerName}_{ActionName}";
        }
    }
}