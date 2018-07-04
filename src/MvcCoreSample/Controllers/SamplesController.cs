using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MvcCoreSample.Extensions;
using MvcCoreSample.Models;
using OSGeo.MapGuide;

namespace MvcCoreSample.Controllers
{
    public class SamplesController : MgBaseController
    {
        readonly IHostingEnvironment _hostEnv;

        public SamplesController(IHostingEnvironment hostEnv)
        {
            _hostEnv = hostEnv;
        }

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
            var roadLabel = roadLayer.GetLegendLabel();
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
            districtsQuery.SetFilter("ID = '{parcelId}'");

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

        public IActionResult DrawLine(DrawLineModel model)
        {
            var (conn, map) = OpenMap(model);
            var resSvc = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var featSvc = (MgFeatureService)conn.CreateService(MgServiceType.FeatureService);
            var fsId = new MgResourceIdentifier("Session:{model.Session}//TemporaryLines.FeatureSource");

            var className = "Lines";
            var layerName = "Lines";
            var layerLegendLabel = "New Lines";
            var groupName = "Analysis";
            var groupLegendLabel = "Analysis";
            if (!resSvc.ResourceExists(fsId))
            {
                // Create a temporary feature source to draw the lines on

                // Create a feature class definition for the new feature
                // source
                var clsDef = new MgClassDefinition();
                clsDef.SetName(className);
                clsDef.SetDescription("Lines to display");

                var clsProps = clsDef.GetProperties();
                var clsIdProps = clsDef.GetIdentityProperties();

                //id 
                var idProp = new MgDataPropertyDefinition("KEY");
                idProp.SetDataType(MgPropertyType.Int32);
                idProp.SetAutoGeneration(true);
                idProp.SetReadOnly(true);

                clsIdProps.Add(idProp);
                clsProps.Add(idProp);

                //name
                var nameProp = new MgDataPropertyDefinition("NAME");
                nameProp.SetDataType(MgPropertyType.String);
                clsProps.Add(nameProp);

                //geometry
                var geomProp = new MgGeometricPropertyDefinition("SHPGEOM");
                clsDef.SetDefaultGeometryPropertyName(geomProp.Name);
                clsProps.Add(geomProp);

                //add to schema
                var fs = new MgFeatureSchema("SHP_Schema", "Line Schema");
                var fsClasses = fs.GetClasses();
                fsClasses.Add(clsDef);

                //Create the feature source
                var sdfParams = new MgFileFeatureSourceParams("OSGeo.SDF", "spatial context", map.GetMapSRS(), fs);
                featSvc.CreateFeatureSource(fsId, sdfParams);
            }

            //Add line
            var insertProps = Utils.MakeLine("Line A", model.x0, model.y0, model.x1, model.y1);
            var insertResult = featSvc.InsertFeatures(fsId, className, insertProps);
            insertResult.Close();

            var layers = map.GetLayers();
            if (!layers.Contains(layerName))
            {
                // Create a new layer which uses that feature source

                // Create a line rule to stylize the lines
                String ruleLegendLabel = "Lines Rule";
                String filter = "";
                String color = "FF0000FF";
                LayerDefinitionFactory factory = new LayerDefinitionFactory(_hostEnv);
                String lineRule = factory.CreateLineRule(ruleLegendLabel, filter, color);

                // Create a line type style
                String lineTypeStyle = factory.CreateLineTypeStyle(lineRule);

                // Create a scale range
                String minScale = "0";
                String maxScale = "1000000000000";
                String lineScaleRange = factory.CreateScaleRange(minScale, maxScale, lineTypeStyle);

                // Create the layer definiton
                String featureName = "SHP_Schema:Lines";
                String geometry = "SHPGEOM";
                String layerDefinition = factory.CreateLayerDefinition(fsId.ToString(), featureName, geometry, lineScaleRange);

                //---------------------------------------------------//
                // Add the layer to the map
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(layerDefinition);
                MgLayer newLayer = Utils.AddLayerDefinitionToMap(doc, layerName, layerLegendLabel, model.Session, resSvc, map);
                // Add the layer to a layer group
                Utils.AddLayerToGroup(newLayer, groupName, groupLegendLabel, map);
            }

            // --------------------------------------------------//
            // Turn on the visibility of this layer.
            // (If the layer does not already exist in the map, it will be visible by default when it is added.
            // But if the user has already run this script, he or she may have set the layer to be invisible.)
            MgLayerCollection layerCollection = map.GetLayers();
            if (layerCollection.Contains(layerName))
            {
                MgLayer linesLayer = (MgLayer)layerCollection.GetItem(layerName);
                linesLayer.SetVisible(true);
            }

            MgLayerGroupCollection groupCollection = map.GetLayerGroups();
            if (groupCollection.Contains(groupName))
            {
                MgLayerGroup analysisGroup = groupCollection.GetItem(groupName);
                analysisGroup.SetVisible(true);
            }

            //---------------------------------------------------//
            //  Save the map back to the session repository
            map.Save();

            //---------------------------------------------------//

            return View(model);
        }

        public IActionResult ClearLines(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resSvc = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var fsId = new MgResourceIdentifier("Session:{model.Session}//TemporaryLines.FeatureSource");
            resSvc.DeleteResource(fsId);
            return View(model);
        }

        public IActionResult Digitizing(MapGuideCommandModel model) => View(model);

        public IActionResult Redlining(MapGuideCommandModel model) => View(model);

        public IActionResult PropertyImage(PropertyReportInputModel model)
        {
            var (conn, map) = OpenMap(model);
            var wkt = map.GetMapSRS();
            var renderSvc = (MgRenderingService)conn.CreateService(MgServiceType.RenderingService);
            var csFactory = new MgCoordinateSystemFactory();
            var srs = csFactory.Create(wkt);
            MgSelection sel = null;
            if (!string.IsNullOrEmpty(model.Selection))
            {
                sel = new MgSelection(map, model.Selection);
            }
            else
            {
                sel = new MgSelection(map);
            }
            var color = new MgColor(205, 189, 156);

            var geometryFactory = new MgGeometryFactory();
            var mapCenterCoordinate = geometryFactory.CreateCoordinateXY(model.CenterX, model.CenterY);

            // Convert the height in pixels to map units.
            // Create an envelope that contains the image area to display.

            var displayInInches = model.Height / 96;
            var displayInMeters = displayInInches * .0254;
            var mapHeightInMeters = displayInMeters * model.Scale;
            var mapHeightInMapUnits = srs.ConvertMetersToCoordinateSystemUnits(mapHeightInMeters);
            var envelopeOffsetY = mapHeightInMapUnits / 2;
            var envelopeOffsetX = model.Width / model.Height * envelopeOffsetY;
            var envelope = new MgEnvelope(model.CenterX - envelopeOffsetX,
              model.CenterY - envelopeOffsetY, model.CenterX + envelopeOffsetX,
              model.CenterY + envelopeOffsetY);
            // Render the image and send it to the browser.

            var byteReader = renderSvc.RenderMap(map, sel, envelope, model.Width, model.Height, color, MgImageFormats.Png);
            var st = new MgReadOnlyStream(byteReader);

            return File(st, MgMimeType.Png);
        }

        public IActionResult PropertyReport(PropertyReportInputModel model)
        {
            var (conn, map) = OpenMap(model);
            var sel = new MgSelection(map, model.Selection);
            var layers = sel.GetLayers();
            var parcelsLayer = layers?.FirstOrDefault(layer => layer.GetName() == "Parcels");
            var vm = new PropertyReportModel
            {
                Session = model.Session,
                MapName = model.MapName,
                NoProperties = true
            };
            if (parcelsLayer != null)
            {
                var fr = sel.GetSelectedFeatures(parcelsLayer, parcelsLayer.GetFeatureClassName(), false);
                if (fr.ReadNext())
                {
                    vm.NoProperties = false;
                    var agf = fr.GetGeometry("SHPGEOM");
                    var agfRw = new MgAgfReaderWriter();
                    var geom = agfRw.Read(agf);
                    var centroid = geom.GetCentroid();
                    var cCoord = centroid.GetCoordinate();
                    var x = cCoord.X;
                    var y = cCoord.Y;

                    vm.Owner = fr.GetString("RNAME");
                    vm.Address = fr.GetString("RPROPAD");
                    vm.BillingAddress = fr.GetString("RBILAD");
                    vm.Description = new[]
                    {
                        fr.GetString("RLDESCR1"),
                        fr.GetString("RLDESCR2"),
                        fr.GetString("RLDESCR3")
                    };
                    vm.Image = (Url.Action(nameof(PropertyImage), model), model.Width, model.Height);
                }
            }
            return View(vm);
        }

        public IActionResult EPlot(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var mappingSvc = (MgMappingService)conn.CreateService(MgServiceType.MappingService);
            var dwfVersion = new MgDwfVersion("6.01", "1.2");

            var plotSpec = new MgPlotSpecification(8.5f, 11.0f, MgPageUnitsType.Inches);
            plotSpec.SetMargins(0.5f, 0.5f, 0.5f, 0.5f);

            var layoutRes = new MgResourceIdentifier("Library://Samples/Sheboygan/Layouts/SheboyganMap.PrintLayout");
            var layout = new MgLayout(layoutRes, "City of Sheboygan", MgPageUnitsType.Inches);

            var byteReader = mappingSvc.GeneratePlot(map, plotSpec, layout, dwfVersion);
            var st = new MgReadOnlyStream(byteReader);

            return File(st, MgMimeType.Dwf);
        }

        public IActionResult MultiPlot(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var mappingSvc = (MgMappingService)conn.CreateService(MgServiceType.MappingService);
            var dwfVersion = new MgDwfVersion("6.01", "1.2");

            var plotSpec = new MgPlotSpecification(8.5f, 11.0f, MgPageUnitsType.Inches);
            plotSpec.SetMargins(0.5f, 0.5f, 0.5f, 0.5f);

            var layoutRes = new MgResourceIdentifier("Library://Samples/Sheboygan/Layouts/SheboyganMap.PrintLayout");
            var layout = new MgLayout(layoutRes, "City of Sheboygan", MgPageUnitsType.Inches);

            var plotCollection = new MgMapPlotCollection();

            var plot1 = new MgMapPlot(map, plotSpec, layout);
            plot1.SetCenterAndScale(map.GetViewCenter().GetCoordinate(), map.GetViewScale() * 2);
            plotCollection.Add(plot1);

            // Create a second map for the second sheet in the DWF. This second sheet uses the print layout
            // to display a page title and legend.

            var map2 = new MgMap(conn);
            map2.Create(map.GetMapDefinition(), "Sheet 2");
            var plot2 = new MgMapPlot(map2, plotSpec, layout);
            plot2.SetCenterAndScale(map.GetViewCenter().GetCoordinate(), map.GetViewScale());
            // plot2 = new MgMapPlot(map2, map.GetViewCenter().GetCoordinate(), map.GetViewScale(), plotSpec, layout);
            plotCollection.Add(plot2);

            var byteReader = mappingSvc.GenerateMultiPlot(plotCollection, dwfVersion);
            var st = new MgReadOnlyStream(byteReader);

            return File(st, MgMimeType.Dwf);
        }
    }
}