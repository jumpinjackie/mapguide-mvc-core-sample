using Microsoft.AspNet.Mvc;
using OSGeo.MapGuide;
using Sheboygan.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Controllers
{
    public class DwfPlotController : MgBaseController
    {
        // GET: DwfPlot
        public IActionResult Index(MapGuideViewerInputModel input)
        {
            MgSiteConnection conn = CreateConnection(input);
            MgMap map = new MgMap(conn);
            map.Open(input.MapName);

            MgPoint center = map.ViewCenter;
            MgCoordinate coord = center.Coordinate;

            DwfPlotViewModel vm = new DwfPlotViewModel()
            {
                MapName = input.MapName,
                Session = input.Session,
                Scale = map.GetViewScale(),
                X = coord.X,
                Y = coord.Y
            };

            return View(vm);
        }

        // GET: DwfPlot/CurrentView
        public IActionResult CurrentView(DwfPlotInputModel input)
        {
            MgByteReader rdr = Plot(input, input.UseLayout, null);
            return ByteReaderResult(rdr);
        }

        public IActionResult AtScale(DwfPlotAtScaleInputModel input)
        {
            MgByteReader rdr = Plot(input, input.UseLayout, input.Scale);
            return ByteReaderResult(rdr);
        }

        private MgByteReader Plot(MapGuideViewerInputModel input, bool useLayout, double? scale)
        {
            MgSiteConnection conn = CreateConnection(input);
            MgMap map = new MgMap(conn);
            map.Open(input.MapName);

            MgPoint center = map.ViewCenter;
            MgCoordinate coord = center.Coordinate;

            MgMappingService mappingService = (MgMappingService)conn.CreateService(MgServiceType.MappingService);

            MgDwfVersion dwfVersion = new MgDwfVersion("6.01", "1.2");
            MgPlotSpecification plotSpec = new MgPlotSpecification(8.5f, 11f, MgPageUnitsType.Inches, 0f, 0f, 0f, 0f);
            plotSpec.SetMargins(0.5f, 0.5f, 0.5f, 0.5f);

            MgLayout layout = null;
            if (useLayout)
            {
                MgResourceIdentifier layoutRes = new MgResourceIdentifier("Library://Samples/Sheboygan/Layouts/SheboyganMap.PrintLayout");
                layout = new MgLayout(layoutRes, "City of Sheboygan", MgPageUnitsType.Inches);
            }

            if (!scale.HasValue)
            {
                return mappingService.GeneratePlot(map, plotSpec, layout, dwfVersion);
            }
            else
            {
                MgCoordinate mapCenter = map.GetViewCenter().GetCoordinate();
                return mappingService.GeneratePlot(map, mapCenter, scale.Value, plotSpec, layout, dwfVersion);
            }
        }
    }
}
