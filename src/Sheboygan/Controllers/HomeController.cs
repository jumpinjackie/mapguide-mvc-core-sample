using Microsoft.AspNet.Mvc;
using Sheboygan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult TaskPaneHome(string session, string mapname)
        {
            return View(new MapGuideViewModel() { Session = session, MapName = mapname });
        }

        public IActionResult Index()
        {
            string mapDef = "Library://Samples/Sheboygan/Maps/Sheboygan.MapDefinition";

            var model = new ViewerModel(mapDef, Url.Action(nameof(TaskPaneHome)));
            return View(model);
        }
    }
}
