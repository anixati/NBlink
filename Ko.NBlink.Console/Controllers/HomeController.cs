using Ko.NBlink.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Ko.NBlink.DemoApp.Controllers
{
    public class HomeController : BaseController
    {
        public NavigateResult Index()
        {
            return Redirect(NBlinkContext.DefaultUrl());
        }

        public BindingResult Execute(string name)
        {
            var data = $"Hello {name} ! @ {DateTime.Now.ToString()}";
            return Json(new { data });
        }
        
        public BindingResult OpenNotepad()
        {
           var p = Process.Start("notepad.exe");
            return Json(new { pid=p.Id });
        }

      
    }
}
