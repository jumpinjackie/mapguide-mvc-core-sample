using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Models;
using OSGeo.MapGuide;

namespace MvcCoreSample.Controllers
{
    public class HomeController : MgBaseController
    {
        readonly IHostingEnvironment _hostEnv;

        public HomeController(IHostingEnvironment hostEnv)
        {
            _hostEnv = hostEnv;
        }

        public IActionResult Index() => View();

        public IActionResult About() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Main()
        {
            var conn = new MgSiteConnection();
            var userInfo = new MgUserInformation("Anonymous", "");
            conn.Open(userInfo);
            var site = conn.GetSite();
            var sessionId = site.CreateSession();
            //Important: Attach the generated session id, otherwise we can't save to any resources in the session repo
            userInfo.SetMgSessionId(sessionId);
            var resSvc = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);

            //mapguide-react-layout makes an incorrect assumption during init that if it was initialized with a session id, then
            //a runtime map must've already been created as it assumes it's coming back from a browser refresh, so to cater for
            //this assumption, pre-create the runtime map state and save it.
            var map = new MgMap(conn);
            var mdfId = new MgResourceIdentifier("Library://Samples/Sheboygan/Maps/Sheboygan.MapDefinition");
            var mapName = mdfId.Name;
            map.Create(mdfId, mapName);
            var mapStateId = new MgResourceIdentifier($"Session:{sessionId}//{mapName}.Map");
            var sel = new MgSelection(map);
            sel.Save(resSvc, mapName);
            map.Save(resSvc, mapStateId);

            var path = Path.Combine(_hostEnv.ContentRootPath, "Data/WebLayout.xml");
            var content = new StringBuilder(System.IO.File.ReadAllText(path))
                .Replace("$RESOURCE_ID", mdfId.ToString())
                .Replace("$HOME_URL", Url.Action(nameof(SamplesController.Home), "Samples", null, Url.ActionContext.HttpContext.Request.Scheme));

            var bytes = Encoding.UTF8.GetBytes(content.ToString());
            var bs = new MgByteSource(bytes, bytes.Length);
            var br = bs.GetReader();

            var resId = new MgResourceIdentifier($"Session:{sessionId}//Sheboygan.WebLayout");
            resSvc.SetResource(resId, br, null);

            var model = new ViewerModel
            {
                LayoutId = resId.ToString(),
                SessionId = sessionId
            };
            return View(model);
        }
    }
}
