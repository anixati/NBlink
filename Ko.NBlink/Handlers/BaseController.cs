using System;

namespace Ko.NBlink.Handlers
{
    public abstract class BaseController : IController
    {
        protected NavigateResult Redirect(string urlstr)
        {
            return Redirect(NBlinkContext.ResolveUrl(urlstr));
        }

        protected NavigateResult Redirect(Uri url, int width = 1200, int height = 800)
        {
            return new NavigateResult(url) { Width = width, Height = height };
        }

        protected BindingResult Json(string payload)
        {
            return new BindingResult(payload);
        }

        protected BindingResult Json(dynamic payload)
        {
            return new BindingResult(payload);
        }

        private IBlinkDispatcher _dispatcher;

        public void Initialise(IBlinkDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        protected void Publish(string payload)
        {
            _dispatcher.Publish(CdpApi.EvalMsg(payload));
        }

        protected void Publish(dynamic expression)
        {
            _dispatcher.Publish(CdpApi.EvalMsg(expression));
        }
    }
}