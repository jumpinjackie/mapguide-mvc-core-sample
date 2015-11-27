using Microsoft.AspNet.Mvc;
using OSGeo.MapGuide;
using Sheboygan.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Controllers
{
    public class SearchController : MgBaseController
    {
        // POST: SelectFeature
        [HttpPost]
        public IActionResult SelectFeature(SelectInputModel input)
        {
            MgSiteConnection conn = CreateConnection(input);
            MgMap map = new MgMap(conn);
            map.Open(input.MapName);

            MgLayerCollection layers = map.GetLayers();
            int lidx = layers.IndexOf("Parcels");
            if (lidx < 0)
            {
                throw new Exception("Layer not found on map: Parcels");
            }
            MgLayerBase layer = layers.GetItem(lidx);
            MgClassDefinition clsDef = layer.GetClassDefinition();
            MgPropertyDefinitionCollection props = clsDef.GetProperties();
            MgPropertyDefinition idProp = props.GetItem(0);
            string idPropName = idProp.Name;
            MgFeatureQueryOptions query = new MgFeatureQueryOptions();
            query.SetFilter(idPropName + " = " + input.id);

            MgFeatureReader reader = layer.SelectFeatures(query);
            MgSelection selection = new MgSelection(map, "");
            MgResourceService resSvc = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            string result = "";
            try
            {
                selection.Open(resSvc, input.MapName);
                selection.FromXml(""); //Clear existing
                selection.AddFeatures(layer, reader, 0);
                result = selection.ToXml();
            }
            finally
            {
                reader.Close();
            }

            return Content(result, MgMimeType.Xml);
        }

        // GET: Search
        public ActionResult Index(MapGuideViewerInputModel input)
        {
            return View(new MapGuideViewModel() { MapName = input.MapName, Session = input.Session });
        }

        static string MakeWktPolygon(double x1, double y1, double x2, double y2)
        {
            return "POLYGON((" + x1 + " " + y1 + ", " + x2 + " " + y1 + ", " + x2 + " " + y2 + ", " + x1 + " " + y2 + ", " + x1 + " " + y1 + "))";
        }

        // GET: Search/Results
        public ActionResult Results(SearchInputModel input)
        {
            MgSiteConnection conn = CreateConnection(input);
            MgMap map = new MgMap(conn);
            map.Open(input.MapName);

            MgLayerCollection layers = map.GetLayers();
            int lidx = layers.IndexOf("Parcels");
            if (lidx < 0)
            {
                throw new Exception("Layer not found on map: Parcels");
            }
            MgLayerBase layer = layers.GetItem(lidx);
            MgFeatureQueryOptions query = new MgFeatureQueryOptions();
            //Don't fret about the input here. This is not a SQL injection attack vector. This filter string is 
            //not SQL, it's a FDO filter. A 'DROP TABLE' or any other destructive SQL will fail on
            //query execution when this filter is parsed to a FDO filter.
            switch (input.By)
            {
                case "OWNER":
                    query.SetFilter("RNAME LIKE '%" + input.Query.Replace("'", "\'") + "%'");
                    break;
                case "ADDRESS":
                    query.SetFilter("RPROPAD LIKE '%" + input.Query.Replace("'", "\'") + "%'");
                    break;
                case "BBOX":
                    double[] parts = input.Query.Split(',').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                    if (parts.Length != 4)
                    {
                        throw new Exception("Invalid BBOX parameter");
                    }
                    MgWktReaderWriter wktRw = new MgWktReaderWriter();
                    MgGeometry filterGeom = wktRw.Read(MakeWktPolygon(parts[0], parts[1], parts[2], parts[3]));
                    query.SetSpatialFilter(layer.FeatureGeometryName, filterGeom, MgFeatureSpatialOperations.Intersects);
                    break;
                default:
                    throw new Exception("Unknown query type: " + input.By);
            }

            SearchResultViewModel vm = new SearchResultViewModel();
            vm.Session = input.Session;
            vm.MapName = input.MapName;
            vm.LoadResults(layer, query);
            return View(vm);
        }
    }
}
