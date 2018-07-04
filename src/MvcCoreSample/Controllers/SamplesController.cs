using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Models;
using OSGeo.MapGuide;

namespace MvcCoreSample.Controllers
{
    public class SamplesController : MgBaseController
    {
        public IActionResult Home(MapGuideCommandModel model) => View(model);

        public IActionResult HelloMap(MapGuideCommandModel model) => View(model);

        public IActionResult GotoPoint(GotoPointModel model) => View(model);

        public IActionResult HelloViewer(MapGuideCommandModel model) => View(model);

        public IActionResult InteractingWithLayers(MapGuideCommandModel model) => View(model);

        public IActionResult WorkingWithFeatures(MapGuideCommandModel model) => View(model);

        public IActionResult ModifyingMapsAndLayers(MapGuideCommandModel model) => View(model);

        public IActionResult AnalyzingFeatures(MapGuideCommandModel model) => View(model);

        public IActionResult DigitizingAndRedlining(MapGuideCommandModel model) => View(model);

        public IActionResult CustomOutput(MapGuideCommandModel model) => View(model);

        public IActionResult DisplaySpatialReference(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            
            ViewData["SpatialReference"] = map.GetMapSRS();
            return View(model);
        }

        public IActionResult ShowLayerVisibility(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);

            var layers = map.GetLayers();
            var vm = new LayerVisibilityModel
            {
                Layers = layers.Select(layer => new LayerVisiblity { LayerName = layer.GetName(), GetVisibleResult = layer.GetVisible(), IsVisibleResult = layer.IsVisible() })
            };
            return View(vm);
        }

        public IActionResult RenameRoadsLayer(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);

            var layers = map.GetLayers();
            
            var roadLayer = layers.GetItem("Roads");
            var roadLabel =roadLayer.GetLegendLabel();
            string newLabel = null;
            if (roadLabel == "Roads") 
                newLabel = "Streets";
            else
                newLabel = "Roads";

            roadLayer.SetLegendLabel(newLabel);
            // You must save the updated map or the
            // changes will not be applied
            // Also be sure to refresh the map on page load.
            map.Save();
            ViewData["NewLabel"] = newLabel;
            return View(model);
        }
    }
}