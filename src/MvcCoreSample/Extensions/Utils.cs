using System;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Hosting;
using OSGeo.MapGuide;

namespace MvcCoreSample.Extensions
{
    public static class Utils
    {
        public static string ResolveDataPath(this IHostingEnvironment hostEnv, string name)
        {
            var path = Path.Combine(hostEnv.ContentRootPath, "Data/", name);
            return path;
        }

        public static MgPropertyCollection MakeLine(string name, double x0, double y0, double x1, double y1)
        {
            var props = new MgPropertyCollection();
            var nameProp = new MgStringProperty("NAME", name);
            props.Add(nameProp);
            var wktRw = new MgWktReaderWriter();
            var agfRw = new MgAgfReaderWriter();
            var geom = wktRw.Read($"LINESTRING XY ({x0} {y0}, {x1} {y1})");
            var agf = agfRw.Write(geom);
            var geomProp = new MgGeometryProperty("SHPGEOM", agf);
            props.Add(geomProp);

            return props;
        }

        // Adds the layer definition (XML) to the map.
        // Returns the layer.
        public static MgLayer AddLayerDefinitionToMap(XmlDocument domDocument, String layerName, String layerLegendLabel, String sessionId, MgResourceService resourceService, MgMap map)
        {
            // TODO: Should probably validate this XML content
            using (MemoryStream ms = new MemoryStream())
            {
                domDocument.Save(ms);
                ms.Position = 0L;
                //Note we do this to ensure our XML content is free of any BOM characters
                byte[] layerDefinition = ms.ToArray();
                Encoding utf8 = Encoding.UTF8;
                String layerDefStr = new String(utf8.GetChars(layerDefinition));
                layerDefinition = new byte[layerDefStr.Length - 1];
                int byteCount = utf8.GetBytes(layerDefStr, 1, layerDefStr.Length - 1, layerDefinition, 0);
                // Save the new layer definition to the session repository  
                MgByteSource byteSource = new MgByteSource(layerDefinition, layerDefinition.Length);
                MgResourceIdentifier resourceID = new MgResourceIdentifier("Session:" + sessionId + "//" + layerName + ".LayerDefinition");
                resourceService.SetResource(resourceID, byteSource.GetReader(), null);

                MgLayer newLayer = AddLayerResourceToMap(resourceID, resourceService, layerName, layerLegendLabel, map);
                return newLayer;
            }
        }

        //////////////////////////////////////////////////////////////
        // Adds a layer to a layer group. If necessary, it creates the layer group.
        public static void AddLayerToGroup(MgLayer layer, String layerGroupName, String layerGroupLegendLabel, MgMap map)
        {
            // Get the layer group
            MgLayerGroupCollection layerGroupCollection = map.GetLayerGroups();
            MgLayerGroup layerGroup = null;
            if (layerGroupCollection.Contains(layerGroupName))
            {
                layerGroup = layerGroupCollection.GetItem(layerGroupName);
            }
            else
            {
                // It does not exist, so create it
                layerGroup = new MgLayerGroup(layerGroupName);
                layerGroup.SetVisible(true);
                layerGroup.SetDisplayInLegend(true);
                layerGroup.SetLegendLabel(layerGroupLegendLabel);
                layerGroupCollection.Add(layerGroup);
            }

            // Add the layer to the group
            layer.SetGroup(layerGroup);
        }

        //////////////////////////////////////////////////////////////
        // Adds a layer defition (which can be stored either in the Library or a session
        // repository) to the map.
        // Returns the layer.
        public static MgLayer AddLayerResourceToMap(MgResourceIdentifier layerResourceID, MgResourceService resourceService, String layerName, String layerLegendLabel, MgMap map)
        {
            MgLayer newLayer = new MgLayer(layerResourceID, resourceService);

            // Add the new layer to the map's layer collection
            newLayer.SetName(layerName);
            newLayer.SetVisible(true);
            newLayer.SetLegendLabel(layerLegendLabel);
            newLayer.SetDisplayInLegend(true);
            MgLayerCollection layerCollection = map.GetLayers();
            if (!layerCollection.Contains(layerName))
            {
                // Insert the new layer at position 0 so it is at the top
                // of the drawing order
                layerCollection.Insert(0, newLayer);
            }

            return newLayer;
        }
    }

    public class LayerDefinitionFactory
    {
        private IHostingEnvironment _server;

        public LayerDefinitionFactory(IHostingEnvironment server)
        {
            _server = server;
        }

        //Creates Area Rule
        //Parameters:
        // legendLabel - string for the legend label
        // filterText - filter string
        // fillColor - fill color
        public String CreateAreaRule(String legendLabel, String filterText, String fillColor)
        {
            String areaRule = File.ReadAllText(_server.ResolveDataPath("arearule.templ"));
            areaRule = TemplateUtil.Substitute(areaRule, legendLabel, filterText, fillColor);
            return areaRule;
        }

        //Creates AreaTypeStyle.
        //Parameters:
        //areaRules - call CreateAreaRule to create area rules
        public String CreateAreaTypeStyle(String areaRules)
        {
            String style = File.ReadAllText(_server.ResolveDataPath("areatypestyle.templ"));
            style = TemplateUtil.Substitute(style, areaRules);
            return style;
        }

        //Creates line rule
        //Parameters:
        //color - color code for the line
        //legendLabel - string for the legend label
        //filter - filter string
        public String CreateLineRule(String legendLabel, String filter, String color)
        {
            String lineRule = File.ReadAllText(_server.ResolveDataPath("linerule.templ"));
            lineRule = TemplateUtil.Substitute(lineRule, legendLabel, filter, color);
            return lineRule;
        }

        //Creates LineTypeStyle
        //Parameters:
        //lineRules - call CreateLineRule to create line rules
        public String CreateLineTypeStyle(String lineRules)
        {
            String lineStyle = File.ReadAllText(_server.ResolveDataPath("linetypestyle.templ"));
            lineStyle = TemplateUtil.Substitute(lineStyle, lineRules);
            return lineStyle;
        }

        //Creates mark symbol
        //Parameters:
        //resourceId - resource identifier for the resource to be used
        //symbolName - the name of the symbol
        //width - the width of the symbol
        //height - the height of the symbol
        //color - color code for the symbol color
        public String CreateMarkSymbol(String resourceId, String symbolName, String width, String height, String color)
        {
            String markSymbol = File.ReadAllText(_server.ResolveDataPath("marksymbol.templ"));
            markSymbol = TemplateUtil.Substitute(markSymbol, width, height, resourceId, symbolName, color);
            return markSymbol;
        }

        //Creates text symbol
        //Parameters:
        //text - string for the text
        //fontHeight - the height for the font
        //TODO:Can we pass it as a integer (ex. 10) or string (ex"10")
        //foregroundColor - color code for the foreground color
        public String CreateTextSymbol(String text, String fontHeight, String foregroundColor)
        {
            String textSymbol = File.ReadAllText(_server.ResolveDataPath("textsymbol.templ"));
            textSymbol = TemplateUtil.Substitute(textSymbol, fontHeight, fontHeight, text, foregroundColor);
            return textSymbol;
        }

        //Creates a point rule
        //Parameters:
        //pointSym - point symbolization. Use CreateMarkSymbol to create it
        //legendlabel - string for the legend label
        //filter - string for the filter
        //label - use CreateTextSymbol to create it
        public String CreatePointRule(String legendLabel, String filter, String label, String pointSym)
        {
            String pointRule = File.ReadAllText(_server.ResolveDataPath("pointrule.templ"));
            pointRule = TemplateUtil.Substitute(pointRule, legendLabel, filter, label, pointSym);
            return pointRule;
        }

        //Creates PointTypeStyle
        //Parameters:
        //pointRule - use CreatePointRule to define rules
        public String CreatePointTypeStyle(String pointRule)
        {
            String pointTypeStyle = File.ReadAllText(_server.ResolveDataPath("pointtypestyle.templ"));
            pointTypeStyle = TemplateUtil.Substitute(pointTypeStyle, pointRule);
            return pointTypeStyle;
        }

        //Creates ScaleRange
        //Parameterss
        //minScale - minimum scale
        //maxScale - maximum scale
        //typeStyle - use one CreateAreaTypeStyle, CreateLineTypeStyle, or CreatePointTypeStyle
        public String CreateScaleRange(String minScale, String maxScale, String typeStyle)
        {
            String scaleRange = File.ReadAllText(_server.ResolveDataPath("scalerange.templ"));
            scaleRange = TemplateUtil.Substitute(scaleRange, minScale, maxScale, typeStyle);
            return scaleRange;
        }

        //Creates a layer definition
        //resourceId - resource identifier for the new layer
        //featureClass - the name of the feature class
        //geometry - the name of the geometry
        //featureClassRange - use CreateScaleRange to define it.
        public String CreateLayerDefinition(String resourceId, String featureClass, String geometry, String featureClassRange)
        {
            String layerDef = File.ReadAllText(_server.ResolveDataPath("layerdefinition.templ"));
            layerDef = TemplateUtil.Substitute(layerDef, resourceId, featureClass, geometry, featureClassRange);
            return layerDef;
        }
    }

    public class TemplateUtil
    {
        public static String Substitute(String templ, params String[] vals)
        {
            StringBuilder res = new StringBuilder();
            int index = 0, val = 0;
            bool found;
            do
            {
                found = false;
                int i = templ.IndexOf('%', index);
                if (i != -1)
                {
                    found = true;
                    res.Append(templ.Substring(index, i - index));
                    if (i < templ.Length - 1)
                    {
                        if (templ[i + 1] == '%')
                            res.Append('%');
                        else if (templ[i + 1] == 's')
                            res.Append(vals[val++]);
                        else
                            res.Append('@');    //add a character illegal in jscript so we know the template was incorrect
                        index = i + 2;
                    }
                }
            } while (found);
            res.Append(templ.Substring(index));
            return res.ToString();
        }
    }
}