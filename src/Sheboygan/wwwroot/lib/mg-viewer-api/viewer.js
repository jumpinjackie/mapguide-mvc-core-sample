function cleanJson(data) {
    var PARSE_INT = 1;
    var PARSE_FLOAT = 2;
    var PARSE_BOOL = 3;

    var deArrayify = function (arr, type) {
        if (arr == null)
            return null;
        if (arr.length > 0) {
            var val = arr[0];
            if (type == PARSE_INT)
                return parseInt(val, 10);
            else if (type == PARSE_FLOAT)
                return parseFloat(val);
            else if (type == PARSE_BOOL)
                return (val == "true");
            else
                return val;
        }

        return null;
    };

    var cleanLayer = function (layer) {
        var cl = {
            ActuallyVisible: deArrayify(layer.ActuallyVisible, PARSE_BOOL),
            DisplayInLegend: deArrayify(layer.DisplayInLegend, PARSE_BOOL),
            ExpandInLegend: deArrayify(layer.ExpandInLegend, PARSE_BOOL),
            LayerDefinition: deArrayify(layer.LayerDefinition),
            LegendLabel: deArrayify(layer.LegendLabel),
            Name: deArrayify(layer.Name),
            ObjectId: deArrayify(layer.ObjectId),
            ParentId: deArrayify(layer.ParentId),
            Selectable: deArrayify(layer.Selectable, PARSE_BOOL),
            Type: deArrayify(layer.Type, PARSE_INT),
            Visible: deArrayify(layer.Visible, PARSE_BOOL)
        };
        if (layer.FeatureSource) {
            cl.FeatureSource = {
                ClassName: deArrayify(layer.FeatureSource[0].ClassName),
                Geometry: deArrayify(layer.FeatureSource[0].Geometry),
                ResourceId: deArrayify(layer.FeatureSource[0].ResourceId),
            };
        }
        if (layer.ScaleRange) {
            cl.ScaleRange = [];
            for (var i = 0; i < layer.ScaleRange.length; i++) {
                var scaleR = layer.ScaleRange[i];
                var sr = {
                    MinScale: deArrayify(scaleR.MinScale, (scaleR.MinScale[0] == "infinity") ? null : PARSE_FLOAT),
                    MaxScale: deArrayify(scaleR.MaxScale, (scaleR.MaxScale[0] == "infinity") ? null : PARSE_FLOAT)
                };
                sr.FeatureStyle = [];

                for (var j = 0; j < scaleR.FeatureStyle.length; j++) {
                    var featureTS = scaleR.FeatureStyle[j];
                    var fts = {
                        Rule: [],
                        Type: deArrayify(featureTS.Type, PARSE_INT)
                    };
                    for (var k = 0; k < featureTS.Rule.length; k++) {
                        fts.Rule.push({
                            Filter: deArrayify(featureTS.Rule[k].Filter),
                            Icon: deArrayify(featureTS.Rule[k].Icon),
                            LegendLabel: deArrayify(featureTS.Rule[k].LegendLabel)
                        });
                    }
                    sr.FeatureStyle.push(fts);
                }
                cl.ScaleRange.push(sr);
            }
        }
        return cl;
    };
    var cleanGroup = function (group) {
        return {
            ActuallyVisible: deArrayify(group.ActuallyVisible, PARSE_BOOL),
            DisplayInLegend: deArrayify(group.DisplayInLegend, PARSE_BOOL),
            ExpandInLegend: deArrayify(group.ExpandInLegend, PARSE_BOOL),
            LegendLabel: deArrayify(group.LegendLabel),
            Name: deArrayify(group.Name),
            ObjectId: deArrayify(group.ObjectId),
            Type: deArrayify(group.Type, PARSE_INT),
            Visible: deArrayify(group.Visible, PARSE_BOOL)
        };
    };

    var clean = {};
    clean.RuntimeMap = {};
    var rtm = data.RuntimeMap;

    clean.RuntimeMap.BackgroundColor = deArrayify(rtm.BackgroundColor);

    var cs = rtm.CoordinateSystem[0];
    clean.RuntimeMap.CoordinateSystem = {
        EpsgCode: deArrayify(cs.EpsgCode, PARSE_INT),
        MentorCode: deArrayify(cs.MentorCode),
        MetersPerUnit: deArrayify(cs.MetersPerUnit, PARSE_FLOAT),
        Wkt: deArrayify(cs.Wkt)
    };
    clean.RuntimeMap.DisplayDpi = deArrayify(rtm.DisplayDpi, PARSE_INT);
    clean.RuntimeMap.Extents = {
        LowerLeftCoordinate: {
            X: deArrayify(rtm.Extents[0].LowerLeftCoordinate[0].X, PARSE_FLOAT),
            Y: deArrayify(rtm.Extents[0].LowerLeftCoordinate[0].Y, PARSE_FLOAT)
        },
        UpperRightCoordinate: {
            X: deArrayify(rtm.Extents[0].UpperRightCoordinate[0].X, PARSE_FLOAT),
            Y: deArrayify(rtm.Extents[0].UpperRightCoordinate[0].Y, PARSE_FLOAT)
        }
    };
    clean.RuntimeMap.IconMimeType = deArrayify(rtm.IconMimeType);
    clean.RuntimeMap.MapDefinition = deArrayify(rtm.MapDefinition);
    clean.RuntimeMap.Name = deArrayify(rtm.Name);
    clean.RuntimeMap.SessionId = deArrayify(rtm.SessionId);
    clean.RuntimeMap.SiteVersion = deArrayify(rtm.SiteVersion);

    if (rtm.Group) {
        clean.RuntimeMap.Group = [];
        for (var i = 0; i < rtm.Group.length; i++) {
            clean.RuntimeMap.Group.push(cleanGroup(rtm.Group[i]));
        }
    }
    if (rtm.Layer) {
        clean.RuntimeMap.Layer = [];
        for (var i = 0; i < rtm.Layer.length; i++) {
            clean.RuntimeMap.Layer.push(cleanLayer(rtm.Layer[i]));
        }
    }

    return clean;
}

// ------------------- AJAX Viewer API ---------------------

//This shims the AJAX viewer API
function ViewerShim() {
    this._map = null;
    this._dpi = null;
    this._inPerUnit = null;
    this._mapname = null;
    this._session = null;
    this.Init = function (map, session, mapname, dpi, inPerUnit) {
        this._map = map;
        this._dpi = dpi;
        this._inPerUnit = inPerUnit;
        this._session = session;
        this._mapname = mapname;
    };
    this.ResolutionToScale = function (res) {
        return res * this._dpi * this._inPerUnit;
    };
    this.ScaleToResolution = function (scale) {
        return scale / this._dpi / this._inPerUnit;
    };
    this.ZoomToView = function (x, y, scale) {
        var view = this._map.getView();
        view.setCenter([x, y]);
        view.setResolution(this.ScaleToResolution(scale));
    };
    this.Refresh = function () {
        var layers = this._map.getLayers();
        layers.forEach(function (lyr) {
            if (lyr instanceof ol.layer.Image) {
                var source = lyr.getSource();
                if (source instanceof ol.source.ImageMapGuide) {
                    source.updateParams({
                        seq: (new Date()).getTime()
                    });
                }
            }
        });
    };
    this.GetMapFrame = function () { return this; };
    this.DigitizePoint = function (handler) {
        console.log("DigitizePoint");
    };
    this.DigitizeLine = function (handler) {
        console.log("DigitizeLine");
    };
    this.DigitizeLineString = function (handler) {
        console.log("DigitizeLineString");
    };
    this.DigitizeRectangle = function (handler) {
        console.log("DigitizeRectangle");
    };
    this.DigitizePolygon = function (handler) {
        console.log("DigitizePolygon");
    };
    this.DigitizeCircle = function (handler) {
        console.log("DigitizeCircle");
    };
    this.SetSelectionXML = function (xml) {
        $.post(agentUrl, {
            "OPERATION": "QUERYMAPFEATURES",
            "VERSION": "2.6.0",
            "SESSION": this._session,
            "MAPNAME": this._mapname,
            "FEATUREFILTER": xml,
            "PERSIST": "1"
        });
    };
}

// ---------------- Task Pane --------------------- //

function TaskPane(taskPaneSelector) {
    this.frame = $(taskPaneSelector);
}

TaskPane.prototype.setHomeUrl = function (homeUrl) {
    this.homeUrl = homeUrl;
    this.navigate(this.homeUrl);
};

TaskPane.prototype.navigate = function (url) {
    this.frame.attr("src", url);
};

TaskPane.prototype.back = function () {
    this.frame[0].contentWindow.history.go(-1);
};

TaskPane.prototype.forward = function () {
    this.frame[0].contentWindow.history.go(1);
};

TaskPane.prototype.home = function () {
    this.navigate(this.homeUrl);
};

function Viewer(options) {
    var _this = this;

    var selectors = options.selectors || {};
    var legendIconRoot = options.legendIconRoot || "/stdicons";
    var onScaleUpdated = options.scaleUpdated || function () { };
    var map = null;
    var mglayers = [];
    var homeUrl = options.homeUrl;

    var activeTool = null;

    this.TaskPane = new TaskPane(selectors.taskPane);
    this.Legend = null;
    this.Sidebar = null;
    this.Shim = new ViewerShim();

    var syncTaskPaneHeight = function () {
        _this.TaskPane.frame.height(_this.Sidebar.height() - 55);
    }

    $(window).on("resize", syncTaskPaneHeight);

    var onMapLoaded = function (rtMapInfo) {
        _this.TaskPane.setHomeUrl(homeUrl + "?session=" + rtMapInfo.RuntimeMap.SessionId + "&mapname=" + rtMapInfo.RuntimeMap.Name);
        var extent = [
            rtMapInfo.RuntimeMap.Extents.LowerLeftCoordinate.X,
            rtMapInfo.RuntimeMap.Extents.LowerLeftCoordinate.Y,
            rtMapInfo.RuntimeMap.Extents.UpperRightCoordinate.X,
            rtMapInfo.RuntimeMap.Extents.UpperRightCoordinate.Y
        ];
        var finiteScales = [];
        if (rtMapInfo.RuntimeMap.FiniteDisplayScale) {
            for (var i = rtMapInfo.RuntimeMap.FiniteDisplayScale.length - 1; i >= 0; i--) {
                finiteScales.push(rtMapInfo.RuntimeMap.FiniteDisplayScale[i]);
            }
        }

        //If a tile set definition is defined it takes precedence over the map definition, this enables
        //this example to work with older releases of MapGuide where no such resource type exists.
        var resourceId = rtMapInfo.RuntimeMap.TileSetDefinition || rtMapInfo.RuntimeMap.MapDefinition;
        //On MGOS 2.6 or older, tile width/height is never returned, so default to 300x300
        var tileWidth = rtMapInfo.RuntimeMap.TileWidth || 300;
        var tileHeight = rtMapInfo.RuntimeMap.TileHeight || 300;
        var metersPerUnit = rtMapInfo.RuntimeMap.CoordinateSystem.MetersPerUnit;
        var dpi = rtMapInfo.RuntimeMap.DisplayDpi;
        var zOrigin = finiteScales.length - 1;
        var inPerUnit = 39.37 * metersPerUnit;
        var resolutions = new Array(finiteScales.length);
        for (var i = 0; i < finiteScales.length; ++i) {
            resolutions[i] = finiteScales[i] / inPerUnit / dpi;
        }

        var projection = "EPSG:" + rtMapInfo.RuntimeMap.CoordinateSystem.EpsgCode;

        var tileGrid = new ol.tilegrid.TileGrid({
            origin: ol.extent.getTopLeft(extent),
            resolutions: resolutions,
            tileSize: [tileWidth, tileHeight]
        });

        var groupLayers = [];
        for (var i = 0; i < rtMapInfo.RuntimeMap.Group.length; i++) {
            var group = rtMapInfo.RuntimeMap.Group[i];
            if (group.Type != 2 && group.Type != 3) { //BaseMap or LinkedTileSet
                continue;
            }
            groupLayers.push(
                new ol.layer.Tile({
                    name: group.Name,
                    source: new ol.source.TileImage({
                        tileGrid: tileGrid,
                        projection: projection,
                        tileUrlFunction: getTileUrlFunctionForGroup(resourceId, group.Name, zOrigin),
                        wrapX: false
                    })
                })
            );
        }

        /*
        if (groupLayers.length > 0) {
            groupLayers.push(
                new ol.layer.Tile({
                    source: new ol.source.TileDebug({
                        tileGrid: tileGrid,
                        projection: projection,
                        tileUrlFunction: function(tileCoord) {
                            return urlTemplate.replace('{z}', (zOrigin - tileCoord[0]).toString())
                                .replace('{x}', tileCoord[1].toString())
                                .replace('{y}', (-tileCoord[2] - 1).toString());
                        },
                        wrapX: false
                    })
                })
            );
        }
        */

        var overlay = new ol.layer.Image({
            name: "MapGuide Dynamic Overlay",
            extent: extent,
            source: new ol.source.ImageMapGuide({
                projection: projection,
                url: agentUrl,
                useOverlay: true,
                metersPerUnit: metersPerUnit,
                params: {
                    MAPNAME: rtMapInfo.RuntimeMap.Name,
                    FORMAT: 'PNG',
                    SESSION: rtMapInfo.RuntimeMap.SessionId,
                    BEHAVIOR: 1 | 2, //Selection and layers
                    SELECTIONCOLOR: "0xFF000080" //Red
                },
                ratio: 2
            })
        });

        for (var i = groupLayers.length - 1; i >= 0; i--) {
            mglayers.push(groupLayers[i]);
        }
        mglayers.push(overlay);
        /*
        console.log("Draw Order:");
        for (var i = 0; i < mglayers.length; i++) {
            console.log(" " + mglayers[i].get("name"));
        }
        */
        var view = null;
        if (resolutions.length == 0) {
            view = new ol.View({
                projection: projection
            });
        } else {
            view = new ol.View({
                projection: projection,
                resolutions: resolutions
            });
        }
        map = new ol.Map({
            target: "map",
            layers: mglayers,
            view: view
        });
        _this.Shim.Init(map, rtMapInfo.RuntimeMap.SessionId, rtMapInfo.RuntimeMap.Name, dpi, inPerUnit);

        var mgTiledLayers = {};
        for (var i = 0; i < groupLayers.length; i++) {
            var grp = groupLayers[i];
            mgTiledLayers[grp.get("name")] = grp;
        }
        _this.Legend = new Legend({
            legendSelector: selectors.legend,
            stdIconRoot: legendIconRoot,
            runtimeMap: rtMapInfo,
            map: map,
            mgLayerOL: overlay,
            mgTiledLayers: mgTiledLayers
        });
        _this.Legend.update();

        view.fit(extent, map.getSize());
        view.on("change:resolution", function (e) {
            onScaleUpdated(_this.Shim.ResolutionToScale(view.getResolution()));
            _this.Legend.update();
        });
        onScaleUpdated(_this.Shim.ResolutionToScale(view.getResolution()));

        _this.Sidebar = $(selectors.sidebar).sidebar();
        syncTaskPaneHeight();

        $(".active-tool").on("click", function (e) {
            e.preventDefault();
            var el = $(e.currentTarget);
            $(".active-tool").removeClass("active");
            el.addClass("active");
            activeTool = el.data("tool");
            console.log("Active tool: " + activeTool);
            return false;
        });
    };

    this.Init = function (mapDef, reqFeatures) {
        $.getJSON(agentUrl, {
            "OPERATION": "CREATERUNTIMEMAP",
            "VERSION": "3.0.0",
            "MAPDEFINITION": mapDef,
            "USERNAME": "Anonymous",
            "REQUESTEDFEATURES": reqFeatures,
            "FORMAT": "application/json"
        }, function (data, textStatus, jqXHR) {
            onMapLoaded(cleanJson(data));
        }).error(function (jqXHR, textStatus, errorThrow) {
            alert(jqXHR.responseText);
        });
    }
}