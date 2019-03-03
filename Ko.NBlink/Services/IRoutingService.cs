using Ko.NBlink.Handlers;
using System.Collections.Generic;

namespace Ko.NBlink
{
    public interface IRoutingService
    {
        void SetupRoutes();

        List<string> GetBindingRoutes();

        ActionResult Execute(ActionContext context);
    }
}