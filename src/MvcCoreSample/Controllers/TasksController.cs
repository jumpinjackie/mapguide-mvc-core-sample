using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Models;

namespace MvcCoreSample.Controllers
{
    public class TasksController : MgBaseController
    {
        public IActionResult Home(MapGuideCommandModel model) => View(model);
    }
}