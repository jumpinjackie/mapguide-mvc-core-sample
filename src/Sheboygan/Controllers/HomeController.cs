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
        public IActionResult TaskPaneHome() => View();

        public IActionResult Index()
        {
            string mapDef = "Library://Samples/Sheboygan/Maps/Sheboygan.MapDefinition";

            var model = new ViewerModel(mapDef, Url.Action(nameof(TaskPaneHome)));
            return View(model);
        }
    }
}
