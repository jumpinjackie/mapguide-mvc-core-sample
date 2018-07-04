using System.Collections.Generic;
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
                MapName = model.MapName,
                Session = model.Session,
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

        private MgFeatureReader SelectParcelsInDistrict(MgMap map, int parcelId = 6)
        {
            var layers = map.GetLayers();
            var districtsLayer = layers.GetItem("Districts");
            var parcelsLayer = layers.GetItem("Parcels");
            var districtsQuery = new MgFeatureQueryOptions();
            //ID is a string property in Districts layer
            districtsQuery.SetFilter($"ID = '{parcelId}'");

            var featureReader = districtsLayer.SelectFeatures(districtsQuery);
            try 
            {
                if (featureReader.ReadNext())
                {
                    // Convert the AGF binary data to MgGeometry.
                    var agfRw = new MgAgfReaderWriter();
                    var agf = featureReader.GetGeometry(districtsLayer.GetFeatureGeometryName());
                    var geom = agfRw.Read(agf);

                    // Create a filter to select the desired features.
                    // Combine a basic filter and a spatial filter.
                    var query = new MgFeatureQueryOptions();
                    query.SetFilter("RNAME LIKE 'SCHMITT%'");
                    query.SetSpatialFilter("SHPGEOM", geom, MgFeatureSpatialOperations.Inside);
                    
                    //Select the features
                    var fr = parcelsLayer.SelectFeatures(query);
                    return fr;
                }
            } 
            finally //Must make sure feature readers are *always* closed
            {
                featureReader.Close();
            }
            return null;
        }

        public IActionResult QueryFeatures(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var fr = SelectParcelsInDistrict(map);
            var results = new List<string>();
            if (fr != null)
            {
                try 
                {
                    while (fr.ReadNext())
                    {
                        results.Add(fr.GetString("RPROPAD"));
                    }
                } 
                finally //Must make sure feature readers are *always* closed
                {
                    fr.Close();
                }
            }
            var vm = new QueryFeaturesModel
            {
                MapName = model.MapName,
                Session = model.Session,
                Addresses = results
            };
            return View(vm);
        }

        public IActionResult SetActiveSelection(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var layers = map.GetLayers();
            var parcelsLayer = layers.GetItem("Parcels");
            var fr = SelectParcelsInDistrict(map);
            string xml = string.Empty;
            if (fr != null)
            {
                var sel = new MgSelection(map);
                sel.AddFeatures(parcelsLayer, fr, 0);
                xml = sel.ToXml();
            }
            var vm = new SetActiveSelectionModel
            {
                MapName = model.MapName,
                Session = model.Session,
                Selection = xml
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult ListSelectedFeatures(SetActiveSelectionModel model)
        {
            var (conn, map) = OpenMap(model);
            //Init selection object from selection XML
            var sel = new MgSelection(map, model.Selection);

            var layers = sel.GetLayers();
            //See if Parcels layer is among the selection
            var parcelsLayer = layers?.FirstOrDefault(layer => layer.GetName() == "Parcels");
            var results = new List<SelectedParcel>();
            var vm = new SelectedFeaturesModel
            {
                MapName = model.MapName,
                Session = model.Session,
                Results = results
            };
            if (parcelsLayer != null)
            {
                //Get all selected features from it and collect data of interest
                var fr = sel.GetSelectedFeatures(parcelsLayer, parcelsLayer.GetFeatureClassName(), false);
                while (fr.ReadNext())
                {
                    results.Add(new SelectedParcel 
                    {
                        Name = fr.GetString("NAME"),
                        Address = fr.GetString("RPROPAD")
                    });
                }
                vm.HasSelectedLayers = true;
            }
            else
            {
                vm.HasSelectedLayers = false;
            }
            
            return View(vm);
        }
    }
}