using Ko.NBlink.Handlers;
using System;
using System.Threading.Tasks;

namespace Ko.NBlink.DemoApp.Controllers
{
    public class SystemController : BaseController
    {
        public BindingResult Monitor()
        {
            Task.Run(() =>
            {
                while (true)
                {

                    //- simulate memory changes
                    var rnd = new Random();
                    var ds = $" [{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)},{rnd.Next(1, 10)} ]";
                    Publish("renderData('" + ds + "')");
                    Task.Delay(1000).Wait();
                }
            });
            return Json(new { op = true });
        }
    }
}
