using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Models;
using OSGeo.MapGuide;

namespace MvcCoreSample.Controllers
{
    public class TasksController : MgBaseController
    {
        public IActionResult Home(MapGuideCommandModel model) => View(model);

        public IActionResult HelloMap(MapGuideCommandModel model) => View(model);

        public IActionResult GotoPoint(GotoPointModel model) => View(model);

        public IActionResult HelloViewer(MapGuideCommandModel model) => View(model);

        public IActionResult DisplaySpatialReference(MapGuideCommandModel model)
        {
            var conn = this.CreateConnection(model);
            var map = new MgMap(conn);
            map.Open(model.MapName);
            
            ViewData["SpatialReference"] = map.GetMapSRS();
            return View(model);
        }
    }
}