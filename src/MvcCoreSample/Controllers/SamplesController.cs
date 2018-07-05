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

            return View("FrameAutoRefresh", model);
        }

        public IActionResult ClearLines(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resSvc = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var fsId = new MgResourceIdentifier($"Session:{model.Session}//TemporaryLines.FeatureSource");
            resSvc.DeleteResource(fsId);
            return View("FrameAutoRefresh", model);
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

        public IActionResult ShowBuildingsAfter1980(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);

            // ...
            // --------------------------------------------------//
            // Load a layer from XML, and use the DOM to change it

            // Load the prototype layer definition into
            // a PHP DOM object.
            XmlDocument domDocument = new XmlDocument();
            String layerDefPath = _hostEnv.ResolveDataPath("RecentlyBuilt.LayerDefinition");
            if (!System.IO.File.Exists(layerDefPath))
            {
                return View("UserError", "The layer definition 'RecentlyBuilt.LayerDefinition' could not be found");
            }
            domDocument.Load(layerDefPath);

            // Get a list of all the <AreaRule><Filter> elements in
            // the XML.
            XmlNodeList nodes = domDocument.SelectNodes("//AreaRule/Filter");
            // Find the correct node and change it
            foreach (XmlNode node in nodes)
            {
                if (node.InnerText == "YRBUILT > 1950")
                {
                    node.InnerText = "YRBUILT > 1980";
                }
            }

            // Get a list of all the <LegendLabel> elements in the
            // XML.
            nodes = domDocument.SelectNodes("//LegendLabel");
            // Find the correct node and change it
            foreach (XmlNode node in nodes)
            {
                if (node.InnerText == "Built after 1950")
                {
                    node.InnerText = "Built after 1980";
                }
            }

            // --------------------------------------------------//
            // ...

            // Add the layer to the map
            MgLayer newLayer = Utils.AddLayerDefinitionToMap(domDocument, "RecentlyBuilt", "Built after 1980", model.Session, resourceService, map);
            Utils.AddLayerToGroup(newLayer, "Analysis", "Analysis", map);

            // --------------------------------------------------//
            // Turn off the "Square Footage" themed layer (if it
            // exists) so it does not hide this layer.
            MgLayerCollection layerCollection = map.GetLayers();
            if (layerCollection.Contains("SquareFootage"))
            {
                MgLayerBase squareFootageLayer = layerCollection.GetItem("SquareFootage");
                squareFootageLayer.SetVisible(false);
            }

            // --------------------------------------------------//
            // Turn on the visibility of this layer.
            // (If the layer does not already exist in the map, it will be visible by default when it is added.
            // But if the user has already run this script, he or she may have set the layer to be invisible.)
            layerCollection = map.GetLayers();
            if (layerCollection.Contains("RecentlyBuilt"))
            {
                MgLayerBase recentlyBuiltLayer = layerCollection.GetItem("RecentlyBuilt");
                recentlyBuiltLayer.SetVisible(true);
            }

            // --------------------------------------------------//
            //  Save the map back to the session repository
            map.Save();

            // --------------------------------------------------//
            ViewData["ZoomToScale"] = 9999;
            return View("FrameAutoRefresh", model);
        }

        public IActionResult ShowSquareFootage(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);

            // ...
            //---------------------------------------------------//
            // Create a new layer
            LayerDefinitionFactory factory = new LayerDefinitionFactory(_hostEnv);

            /// Create three area rules for three different
            // scale ranges.
            String areaRule1 = factory.CreateAreaRule("1 to 800",
              "SQFT &gt;= 1 AND SQFT &lt; 800", "FFFFFF00");
            String areaRule2 = factory.CreateAreaRule("800 to 1600",
              "SQFT &gt;= 800 AND SQFT &lt; 1600", "FFFFBF20");
            String areaRule3 = factory.CreateAreaRule("1600 to 2400",
              "SQFT &gt;= 1600 AND SQFT &lt; 2400", "FFFF8040");

            // Create an area type style.
            String areaTypeStyle = factory.CreateAreaTypeStyle(areaRule1 + areaRule2 + areaRule3);

            // Create a scale range.
            String minScale = "0";
            String maxScale = "10000";
            String areaScaleRange = factory.CreateScaleRange(minScale, maxScale, areaTypeStyle);

            // Create the layer definiton.
            String featureClass = "Library://Samples/Sheboygan/Data/Parcels.FeatureSource";
            String featureName = "SHP_Schema:Parcels";
            String geometry = "SHPGEOM";
            String layerDefinition = factory.CreateLayerDefinition(featureClass, featureName, geometry, areaScaleRange);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(layerDefinition);

            //---------------------------------------------------//
            // ...

            // Add the layer to the map
            MgLayer newLayer = Utils.AddLayerDefinitionToMap(doc, "SquareFootage", "Square Footage", model.Session, resourceService, map);
            Utils.AddLayerToGroup(newLayer, "Analysis", "Analysis", map);

            //---------------------------------------------------//
            // Turn off the "Recently Built" themed layer (if it exists) so it does not hide this layer.
            MgLayerCollection layerCollection = map.GetLayers();
            if (layerCollection.Contains("RecentlyBuilt"))
            {
                MgLayerBase recentlyBuiltLayer = layerCollection.GetItem("RecentlyBuilt");
                recentlyBuiltLayer.SetVisible(false);
            }

            // --------------------------------------------------//
            // Turn on the visibility of this layer.
            // (If the layer does not already exist in the map, it will be visible by default when it is added.
            // But if the user has already run this script, he or she may have set the layer to be invisible.)
            layerCollection = map.GetLayers();
            if (layerCollection.Contains("SquareFootage"))
            {
                MgLayerBase squareFootageLayer = layerCollection.GetItem("SquareFootage");
                squareFootageLayer.SetVisible(true);
            }

            //---------------------------------------------------//
            //  Save the map back to the session repository
            map.Save();

            //---------------------------------------------------//
            ViewData["ZoomToScale"] = 9999;
            return View("FrameAutoRefresh", model);
        }

        public IActionResult ShowHydroLine(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);

            // ...
            //---------------------------------------------------//
            // Create a new layer

            LayerDefinitionFactory factory = new LayerDefinitionFactory(_hostEnv);

            // Create a line rule.
            String legendLabel = "";
            String filter = "";
            String color = "FF0000FF";
            String lineRule = factory.CreateLineRule(legendLabel, filter, color);

            // Create a line type style.
            String lineTypeStyle = factory.CreateLineTypeStyle(lineRule);

            // Create a scale range.
            String minScale = "0";
            String maxScale = "1000000000000";
            String lineScaleRange = factory.CreateScaleRange(minScale, maxScale, lineTypeStyle);

            // Create the layer definiton.
            String featureClass = "Library://Samples/Sheboygan/Data/HydrographicLines.FeatureSource";
            String featureName = "SHP_Schema:HydrographicLines";
            String geometry = "SHPGEOM";
            String layerDefinition = factory.CreateLayerDefinition(featureClass, featureName, geometry, lineScaleRange);

            //---------------------------------------------------//
            // ...

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(layerDefinition);
            // Add the layer to the map
            MgLayer newLayer = Utils.AddLayerDefinitionToMap(doc, "Hydro", "Hydro", model.Session, resourceService, map);
            Utils.AddLayerToGroup(newLayer, "Analysis", "Analysis", map);

            // --------------------------------------------------//
            // Turn on the visibility of this layer.
            // (If the layer does not already exist in the map, it will be visible by default when it is added.
            // But if the user has already run this script, he or she may have set the layer to be invisible.)
            MgLayerCollection layerCollection = map.GetLayers();
            if (layerCollection.Contains("Hydro"))
            {
                MgLayerBase squareFootageLayer = layerCollection.GetItem("Hydro");
                squareFootageLayer.SetVisible(true);
            }

            //---------------------------------------------------//
            //  Save the map back to the session repository
            map.Save();

            //---------------------------------------------------//
            ViewData["ZoomToScale"] = 40000;
            return View("FrameAutoRefresh", model);
        }

        public IActionResult ShowPointsOfInterest(MapGuideCommandModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var featureService = (MgFeatureService)conn.CreateService(MgServiceType.FeatureService);

            //---------------------------------------------------//
            // Create a feature source with point data.
            // (The Sheboygan sample data does not contain such data,
            // so we"ll create it.)

            // Create a feature class definition for the new feature source
            MgClassDefinition classDefinition = new MgClassDefinition();
            classDefinition.SetName("Points");
            classDefinition.SetDescription("Feature class with point data.");
            classDefinition.SetDefaultGeometryPropertyName("GEOM");

            MgPropertyDefinitionCollection idProps = classDefinition.GetIdentityProperties();
            MgPropertyDefinitionCollection clsProps = classDefinition.GetProperties();

            // Create an identify property
            MgDataPropertyDefinition identityProperty = new MgDataPropertyDefinition("KEY");
            identityProperty.SetDataType(MgPropertyType.Int32);
            identityProperty.SetAutoGeneration(true);
            identityProperty.SetReadOnly(true);
            // Add the identity property to the class definition
            clsProps.Add(identityProperty);
            idProps.Add(identityProperty);

            // Create a name property
            MgDataPropertyDefinition nameProperty = new MgDataPropertyDefinition("NAME");
            nameProperty.SetDataType(MgPropertyType.String);
            // Add the name property to the class definition
            clsProps.Add(nameProperty);

            // Create a geometry property
            MgGeometricPropertyDefinition geometryProperty = new MgGeometricPropertyDefinition("GEOM");
            geometryProperty.SetGeometryTypes(MgFeatureGeometricType.Point);
            // Add the geometry property to the class definition
            clsProps.Add(geometryProperty);

            // Create a feature schema
            MgFeatureSchema featureSchema = new MgFeatureSchema("PointSchema", "Point schema");
            MgClassDefinitionCollection classes = featureSchema.GetClasses();
            // Add the feature schema to the class definition
            classes.Add(classDefinition);

            // Create the feature source
            String featureSourceName = $"Session:{model.Session}//PointsOfInterest.FeatureSource";
            MgResourceIdentifier resourceIdentifier = new MgResourceIdentifier(featureSourceName);
            //wkt = "LOCALCS[\"*XY-MT*\",LOCAL_DATUM[\"*X-Y*\",10000],UNIT[\"Meter\", 1],AXIS[\"X\",EAST],AXIS[\"Y\",NORTH]]";
            String wkt = "GEOGCS[\"LL84\",DATUM[\"WGS84\",SPHEROID[\"WGS84\",6378137.000,298.25722293]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.01745329251994]]";
            MgFileFeatureSourceParams sdfParams = new MgFileFeatureSourceParams("OSGeo.SDF", "LatLong", wkt, featureSchema);
            featureService.CreateFeatureSource(resourceIdentifier, sdfParams);

            // We need to add some data to the sdf before using it.  The spatial context
            // reader must have an extent.
            MgBatchPropertyCollection batchPropertyCollection = new MgBatchPropertyCollection();
            MgWktReaderWriter wktReaderWriter = new MgWktReaderWriter();
            MgAgfReaderWriter agfReaderWriter = new MgAgfReaderWriter();
            MgGeometryFactory geometryFactory = new MgGeometryFactory();

            // Make four points
            batchPropertyCollection.Add(Utils.MakePoint("Point A", -87.727, 43.748));
            batchPropertyCollection.Add(Utils.MakePoint("Point B", -87.728, 43.730));
            batchPropertyCollection.Add(Utils.MakePoint("Point C", -87.726, 43.750));
            batchPropertyCollection.Add(Utils.MakePoint("Point D", -87.728, 43.750));

            // Add the batch property collection to the feature source
            MgInsertFeatures cmd = new MgInsertFeatures("Points", batchPropertyCollection);
            MgFeatureCommandCollection featureCommandCollection = new MgFeatureCommandCollection();
            featureCommandCollection.Add(cmd);

            // Execute the "add" commands
            featureService.UpdateFeatures(resourceIdentifier, featureCommandCollection, false);

            // ...
            //---------------------------------------------------//
            // Create a new layer

            LayerDefinitionFactory factory = new LayerDefinitionFactory(_hostEnv);

            // Create a mark symbol
            String resourceId = "Library://Samples/Sheboygan/Symbols/BasicSymbols.SymbolLibrary";
            String symbolName = "PushPin";
            String width = "24";  // unit = points
            String height = "24"; // unit = points
            String color = "FFFF0000";
            String markSymbol = factory.CreateMarkSymbol(resourceId, symbolName, width, height, color);

            // Create a text symbol
            String text = "ID";
            String fontHeight = "12";
            String foregroundColor = "FF000000";
            String textSymbol = factory.CreateTextSymbol(text, fontHeight, foregroundColor);

            // Create a point rule.
            String legendLabel = "trees";
            String filter = "";
            String pointRule = factory.CreatePointRule(legendLabel, filter, textSymbol, markSymbol);

            // Create a point type style.
            String pointTypeStyle = factory.CreatePointTypeStyle(pointRule);

            // Create a scale range.
            String minScale = "0";
            String maxScale = "1000000000000";
            String pointScaleRange = factory.CreateScaleRange(minScale, maxScale, pointTypeStyle);

            // Create the layer definiton.
            String featureName = "PointSchema:Points";
            String geometry = "GEOM";
            String layerDefinition = factory.CreateLayerDefinition(featureSourceName, featureName, geometry, pointScaleRange);
            //---------------------------------------------------//
            // ...

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(layerDefinition);
            // Add the layer to the map
            MgLayer newLayer = Utils.AddLayerDefinitionToMap(doc, "Points", "Points of Interest", model.Session, resourceService, map);
            Utils.AddLayerToGroup(newLayer, "Analysis", "Analysis", map);

            // --------------------------------------------------//
            // Turn on the visibility of this layer.
            // (If the layer does not already exist in the map, it will be visible by default when it is added.
            // But if the user has already run this script, he or she may have set the layer to be invisible.)
            MgLayerCollection layerCollection = map.GetLayers();
            if (layerCollection.Contains("Points"))
            {
                MgLayerBase pointsLayer = layerCollection.GetItem("Points");
                pointsLayer.SetVisible(true);
            }

            //---------------------------------------------------//
            //  Save the map back to the session repository
            map.Save();

            //---------------------------------------------------//
            ViewData["ZoomToScale"] = 40000;
            return View("FrameAutoRefresh", model);
        }

        [HttpPost]
        public IActionResult CreateBuffer(BufferModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var featureService = (MgFeatureService)conn.CreateService(MgServiceType.FeatureService);

            // Check for selection data passed via HTTP POST

            MgSelection selection = null;
            MgReadOnlyLayerCollection selectedLayers = null;
            if (!string.IsNullOrEmpty(model.Selection))
            {
                selection = new MgSelection(map, model.Selection);
                selectedLayers = selection.GetLayers();
            }

            if (selectedLayers != null)
            {
                int bufferRingSize = 100; // measured in metres
                int bufferRingCount = 5;

                // Set up some objects for coordinate conversion

                String mapWktSrs = map.GetMapSRS();
                MgAgfReaderWriter agfReaderWriter = new MgAgfReaderWriter();
                MgWktReaderWriter wktReaderWriter = new MgWktReaderWriter();
                MgCoordinateSystemFactory coordinateSystemFactory = new MgCoordinateSystemFactory();
                MgCoordinateSystem srs = coordinateSystemFactory.Create(mapWktSrs);
                MgMeasure srsMeasure = srs.GetMeasure();

                BufferHelper helper = new BufferHelper(_hostEnv);

                // Check for a buffer layer. If it exists, delete
                // the current features.
                // If it does not exist, create a feature source and
                // a layer to hold the buffer.

                var mapLayers = map.GetLayers();
                MgLayer bufferLayer = null;
                int layerIndex = mapLayers.IndexOf("Buffer");
                if (layerIndex < 0)
                {
                    // The layer does not exist and must be created.

                    MgResourceIdentifier bufferFeatureResId = new MgResourceIdentifier($"Session:{model.Session}//Buffer.FeatureSource");
                    helper.CreateBufferFeatureSource(featureService, mapWktSrs, bufferFeatureResId);
                    bufferLayer = helper.CreateBufferLayer(resourceService, bufferFeatureResId, model.Session);
                    mapLayers.Insert(0, bufferLayer);
                }
                else
                {
                    bufferLayer = (MgLayer)mapLayers.GetItem(layerIndex);
                    MgFeatureCommandCollection commands = new MgFeatureCommandCollection();
                    commands.Add(new MgDeleteFeatures("BufferClass", "ID like '%'"));

                    var result = bufferLayer.UpdateFeatures(commands);
                    Utils.HandleUpdateFeaturesResults(result);
                }

                for (int i = 0; i < selectedLayers.GetCount(); i++)
                {
                    // Only check selected features in the Parcels layer.

                    MgLayer layer = (MgLayer)selectedLayers.GetItem(i);

                    if (layer.GetName() == "Parcels")
                    {
                        // Get the selected features from the MgSelection object
                        MgFeatureReader featureReader = selection.GetSelectedFeatures(layer, layer.GetFeatureClassName(), false);

                        // Process each item in the MgFeatureReader. Get the
                        // geometries from all the selected features and
                        // merge them into a single geometry.

                        MgGeometryCollection inputGeometries = new MgGeometryCollection();
                        while (featureReader.ReadNext())
                        {
                            MgByteReader featureGeometryData = featureReader.GetGeometry("SHPGEOM");
                            MgGeometry featureGeometry = agfReaderWriter.Read(featureGeometryData);

                            inputGeometries.Add(featureGeometry);
                        }

                        MgGeometryFactory geometryFactory = new MgGeometryFactory();
                        MgGeometry mergedGeometries = geometryFactory.CreateMultiGeometry(inputGeometries);

                        // Add buffer features to the temporary feature source.
                        // Create multiple concentric buffers to show area.
                        // If the stylization for the layer draws the features
                        // partially transparent, the concentric rings will be
                        // progressively darker towards the center.
                        // The stylization is set in the layer template file, which
                        // is used in function CreateBufferLayer().

                        MgFeatureCommandCollection commands = new MgFeatureCommandCollection();
                        for (int bufferRing = 0; bufferRing < bufferRingCount; bufferRing++)
                        {
                            double bufferDist = srs.ConvertMetersToCoordinateSystemUnits(bufferRingSize * (bufferRing + 1));
                            MgGeometry bufferGeometry = mergedGeometries.Buffer(bufferDist, srsMeasure);

                            MgPropertyCollection properties = new MgPropertyCollection();
                            properties.Add(new MgGeometryProperty(bufferLayer.FeatureGeometryName, agfReaderWriter.Write(bufferGeometry)));

                            commands.Add(new MgInsertFeatures(bufferLayer.FeatureClassName, properties));
                        }
                        var result = bufferLayer.UpdateFeatures(commands);
                        Utils.HandleUpdateFeaturesResults(result);

                        bufferLayer.SetVisible(true);
                        bufferLayer.ForceRefresh();
                        bufferLayer.SetDisplayInLegend(true);
                        map.Save();

                        ViewData["FrameStartupMessage"] = "Buffer created";
                    }
                }
            }

            return View("FrameAutoRefresh", model);
        }

        [HttpPost]
        public IActionResult FindFeaturesInBuffer(BufferModel model)
        {
            var (conn, map) = OpenMap(model);
            var resourceService = (MgResourceService)conn.CreateService(MgServiceType.ResourceService);
            var featureService = (MgFeatureService)conn.CreateService(MgServiceType.FeatureService);
            MgFeatureQueryOptions queryOptions = new MgFeatureQueryOptions();

            // Check for selection data passed via HTTP POST

            MgSelection selection = null;
            MgReadOnlyLayerCollection selectedLayers = null;
            if (!string.IsNullOrEmpty(model.Selection))
            {
                selection = new MgSelection(map, model.Selection);
                selectedLayers = selection.GetLayers();
            }

            if (selectedLayers != null)
            {
                int bufferRingSize = 500; // measured in metres

                // Set up some objects for coordinate conversion

                String mapWktSrs = map.GetMapSRS();
                MgAgfReaderWriter agfReaderWriter = new MgAgfReaderWriter();
                MgWktReaderWriter wktReaderWriter = new MgWktReaderWriter();
                MgCoordinateSystemFactory coordinateSystemFactory = new MgCoordinateSystemFactory();
                MgCoordinateSystem srs = coordinateSystemFactory.Create(mapWktSrs);
                MgMeasure srsMeasure = srs.GetMeasure();

                // Check for a buffer layer. If it exists, delete
                // the current features.
                // If it does not exist, create a feature source and
                // a layer to hold the buffer.

                var mapLayers = map.GetLayers();
                BufferHelper helper = new BufferHelper(_hostEnv);
                MgLayer bufferLayer = null;
                int layerIndex = mapLayers.IndexOf("Buffer");
                if (layerIndex < 0)
                {
                    // The layer does not exist and must be created.

                    MgResourceIdentifier bufferFeatureResId = new MgResourceIdentifier($"Session:{model.Session}//Buffer.FeatureSource");
                    helper.CreateBufferFeatureSource(featureService, mapWktSrs, bufferFeatureResId);
                    bufferLayer = helper.CreateBufferLayer(resourceService, bufferFeatureResId, model.Session);
                    mapLayers.Insert(0, bufferLayer);
                }
                else
                {
                    bufferLayer = (MgLayer)mapLayers.GetItem(layerIndex);
                    MgFeatureCommandCollection commands = new MgFeatureCommandCollection();
                    commands.Add(new MgDeleteFeatures(bufferLayer.FeatureClassName, "ID like '%'"));

                    var result = bufferLayer.UpdateFeatures(commands);
                    Utils.HandleUpdateFeaturesResults(result);
                }

                // Check for a parcel marker layer. If it exists, delete
                // the current features.
                // If it does not exist, create a feature source and
                // a layer to hold the parcel markers.

                MgLayer parcelMarkerLayer = null;
                layerIndex = mapLayers.IndexOf("ParcelMarker");
                if (layerIndex < 0)
                {
                    MgResourceIdentifier parcelFeatureResId = new MgResourceIdentifier($"Session:{model.Session}//Buffer.FeatureSource");
                    helper.CreateParcelMarkerFeatureSource(featureService, mapWktSrs, parcelFeatureResId);
                    parcelMarkerLayer = helper.CreateParcelMarkerLayer(resourceService, parcelFeatureResId, model.Session);
                    mapLayers.Insert(0, parcelMarkerLayer);
                }
                else
                {
                    parcelMarkerLayer = (MgLayer)mapLayers.GetItem(layerIndex);
                    MgFeatureCommandCollection commands = new MgFeatureCommandCollection();
                    commands.Add(new MgDeleteFeatures("ParcelMarkerClass", "ID like '%'"));

                    var result = parcelMarkerLayer.UpdateFeatures(commands);
                    Utils.HandleUpdateFeaturesResults(result);
                }

                // Check each layer in the selection.

                for (int i = 0; i < selectedLayers.GetCount(); i++)
                {
                    // Only check selected features in the Parcels layer.

                    MgLayer layer = (MgLayer)selectedLayers.GetItem(i);

                    if (layer.GetName() == "Parcels")
                    {

                        //Response.Write("Marking all parcels inside the buffer that are of type 'MFG'");

                        MgFeatureReader featureReader = selection.GetSelectedFeatures(layer, layer.GetFeatureClassName(), false);


                        // Process each item in the MgFeatureReader. Get the
                        // geometries from all the selected features and
                        // merge them into a single geometry.

                        MgGeometryCollection inputGeometries = new MgGeometryCollection();
                        while (featureReader.ReadNext())
                        {
                            MgByteReader featureGeometryData = featureReader.GetGeometry("SHPGEOM");
                            MgGeometry featureGeometry = agfReaderWriter.Read(featureGeometryData);

                            inputGeometries.Add(featureGeometry);
                        }

                        MgGeometryFactory geometryFactory = new MgGeometryFactory();
                        MgGeometry mergedGeometries = geometryFactory.CreateMultiGeometry(inputGeometries);

                        // Create a buffer from the merged geometries

                        double bufferDist = srs.ConvertMetersToCoordinateSystemUnits(bufferRingSize);
                        MgGeometry bufferGeometry = mergedGeometries.Buffer(bufferDist, srsMeasure);

                        // Create a filter to select parcels within the buffer. Combine
                        // a basic filter and a spatial filter to select all parcels
                        // within the buffer that are of type "MFG".

                        queryOptions = new MgFeatureQueryOptions();
                        queryOptions.SetFilter("RTYPE = 'MFG'");
                        queryOptions.SetSpatialFilter("SHPGEOM", bufferGeometry, MgFeatureSpatialOperations.Inside);

                        featureReader = layer.SelectFeatures(queryOptions);

                        // Get the features from the feature source,
                        // determine the centroid of each selected feature, and
                        // add a point to the ParcelMarker layer to mark the
                        // centroid.
                        // Collect all the points into an MgFeatureCommandCollection,
                        // so they can all be added in one operation.

                        MgFeatureCommandCollection parcelMarkerCommands = new MgFeatureCommandCollection();
                        while (featureReader.ReadNext())
                        {
                            MgByteReader byteReader = featureReader.GetGeometry("SHPGEOM");
                            MgGeometry geometry = agfReaderWriter.Read(byteReader);
                            MgPoint point = geometry.GetCentroid();

                            // Create an insert command for this parcel.
                            MgPropertyCollection properties = new MgPropertyCollection();
                            properties.Add(new MgGeometryProperty("ParcelLocation", agfReaderWriter.Write(point)));
                            parcelMarkerCommands.Add(new MgInsertFeatures("ParcelMarkerClass", properties));
                        }
                        featureReader.Close();

                        if (parcelMarkerCommands.GetCount() > 0)
                        {
                            var pr = parcelMarkerLayer.UpdateFeatures(parcelMarkerCommands);
                            Utils.HandleUpdateFeaturesResults(pr);
                        }
                        else
                        {
                            ViewData["FrameStartupMessage"] = "No parcels within the buffer area match.";
                        }

                        // Create a feature in the buffer feature source to show the area covered by the buffer.

                        MgPropertyCollection props = new MgPropertyCollection();
                        props.Add(new MgGeometryProperty(bufferLayer.FeatureGeometryName, agfReaderWriter.Write(bufferGeometry)));
                        MgFeatureCommandCollection commands = new MgFeatureCommandCollection();
                        commands.Add(new MgInsertFeatures(bufferLayer.FeatureClassName, props));

                        var result = bufferLayer.UpdateFeatures(commands);
                        Utils.HandleUpdateFeaturesResults(result);
                        // Ensure that the buffer layer is visible and in the legend.

                        bufferLayer.SetVisible(true);
                        bufferLayer.ForceRefresh();
                        bufferLayer.SetDisplayInLegend(true);
                        parcelMarkerLayer.SetVisible(true);
                        parcelMarkerLayer.ForceRefresh();

                        map.Save();
                    }
                }
            }
            else
            {
                ViewData["FrameStartupMessage"] = "No selected layers";
            }

            return View("FrameAutoRefresh", model);
        }
    }
}