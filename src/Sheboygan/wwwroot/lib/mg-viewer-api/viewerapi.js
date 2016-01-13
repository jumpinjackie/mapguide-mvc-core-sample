//common.js - utility functions

function getFusionInstance() {
    if (opener)
        return opener.Fusion;
    if (parent)
        return parent.Fusion;

    throw "Cannot find Fusion instance!";
}

function getViewerInstance() {
    var viewerFrame = parent.parent;
    if (typeof (viewerFrame.ZoomToView) == 'function')
        return viewerFrame;
    else if (typeof (viewerFrame.viewer) != 'undefined') {
        var shim = viewerFrame.viewer.Shim;
        if (typeof (shim) != 'undefined' && typeof (shim.ZoomToView) == 'function')
            return shim;
    }
    throw "Cannot find viewer instance!";
}

function isFusion() {
    if (opener) {
        return typeof (opener.Fusion) != 'undefined';
    }
    if (parent) {
        return typeof (parent.Fusion) != 'undefined';
    }
    return false;
}

//Polygon circle approximation borrowed from the AJAX viewer
var simulateCirclePoints = [];
var simulateCircleHalfPointNumber = 40;
(function () {
    for (var index = 0; index < 2 * simulateCircleHalfPointNumber + 1; index++) {
        simulateCirclePoints[2 * index] = Math.cos(Math.PI * index / simulateCircleHalfPointNumber);
        simulateCirclePoints[2 * index + 1] = Math.sin(Math.PI * index / simulateCircleHalfPointNumber);
    }
})();

//Theses are the Geometry classes used in the MapGuide Viewer API
function Point(x, y) {
    this.X = x;
    this.Y = y;
}

function LineString() {
    this.points = new Array();
    this.Count = 0;

    this.AddPoint = function (pt) {
        this.points.push(pt);
        this.Count++;
    }

    this.Point = function (i) {
        if (i < 0 || i >= this.points.length)
            return null;
        return this.points[i];
    }
}

function Circle() {
    this.Center = null;
    this.Radius = 0;

    this.SetRadius = function (pt) {
        dx = pt.X - this.Center.X;
        dy = pt.Y - this.Center.Y;
        this.Radius = Math.sqrt(dx * dx + dy * dy);
    }
}

function Rectangle(p1, p2) {
    this.Point1 = p1;
    this.Point2 = p2;
}

function Polygon() {
    this.LineStringInfo = LineString;
    this.LineStringInfo();
}

//This function converts the digitized OL geometry into the AJAX viewer geometry model
function mgApiCallHandler(geom, geomType, handler) {
    var apiGeom = null;
    if (geomType == 'rect') {
        var v = geom.getVertices();
        apiGeom = new Rectangle(new Point(v[0].x, v[0].y), new Point(v[2].x, v[2].y));
    } else if (geomType == 'circle') {
        apiGeom = new Circle();
        apiGeom.Center = new Point(geom.x, geom.y);
        apiGeom.Radius = geom.r;
    } else {
        switch (geom.CLASS_NAME) {
            case 'OpenLayers.Geometry.Point':
                apiGeom = new Point(geom.x, geom.y);
                break;
            case 'OpenLayers.Geometry.LineString':
                apiGeom = new LineString();
                var v = geom.getVertices();
                for (var i = 0; i < v.length; ++i) {
                    apiGeom.AddPoint(new Point(v[i].x, v[i].y));
                }
                break;
            case 'OpenLayers.Geometry.Polygon':
                apiGeom = new LineString();
                var v = geom.getVertices();
                for (var i = 0; i < v.length; ++i) {
                    apiGeom.AddPoint(new Point(v[i].x, v[i].y));
                }
                break;
        }
    }
    handler(apiGeom);
    return false;
}

//This is a shim that allows access to common viewer functionality in both AJAX and Fusion
//viewer contexts
function ViewerAPI() {
    var _this = this;
    var _viewer = null;
    this.isFusion = isFusion();

    //Depending on our viewer context, _viewer will either be the AJAX Viewer main frame
    //or the Fusion map widget
    if (this.isFusion) {
        _viewer = getFusionInstance().getMapByIndice(0);
    } else {
        _viewer = getViewerInstance();
    }

    //Zoom
    if (this.isFusion) {
        this.zoomToView = function (x, y, scale) {
            var extent = _viewer.getExtentFromPoint(x, y, scale);
            _viewer.setExtents(extent);
        };
    } else {
        this.zoomToView = function (x, y, scale) {
            _viewer.ZoomToView(x, y, scale);
        };
    }

    //Refresh
    if (this.isFusion) {
        this.refresh = function () {
            _viewer.redraw();
        };
    } else {
        this.refresh = function () {
            _viewer.Refresh();
        };
    }

    //DigitizePoint
    if (this.isFusion) {
        this.digitizePoint = function (handler) {
            _viewer.digitizePoint({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'point', handler);
            });
        }
    } else {
        this.digitizePoint = function (handler) {
            _viewer.GetMapFrame().DigitizePoint(handler);
        }
    }

    //DigitizeLine
    if (this.isFusion) {
        this.digitizeLine = function (handler) {
            _viewer.digitizeLine({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'line', handler);
            });
        }
    } else {
        this.digitizeLine = function (handler) {
            _viewer.GetMapFrame().DigitizeLine(handler);
        }
    }

    //DigitizeLineString
    if (this.isFusion) {
        this.digitizeLineString = function (handler) {
            _viewer.digitizeLineString({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'linestr', handler);
            });
        }
    } else {
        this.digitizeLineString = function (handler) {
            _viewer.GetMapFrame().DigitizeLineString(handler);
        }
    }

    //DigitizeRectangle
    if (this.isFusion) {
        this.digitizeRectangle = function (handler) {
            _viewer.digitizeRectangle({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'rect', handler);
            });
        }
    } else {
        this.digitizeRectangle = function (handler) {
            _viewer.GetMapFrame().DigitizeRectangle(handler);
        }
    }

    //DigitizePolygon
    if (this.isFusion) {
        this.digitizePolygon = function (handler) {
            _viewer.digitizePolygon({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'polygon', handler);
            });
        }
    } else {
        this.digitizePolygon = function (handler) {
            _viewer.GetMapFrame().DigitizePolygon(handler);
        }
    }

    //DigitizeCircle
    if (this.isFusion) {
        this.digitizeCircle = function (handler) {
            _viewer.digitizeCircle({}, function (olGeom) {
                mgApiCallHandler(olGeom, 'circle', handler);
            });
        }
    } else {
        this.digitizeCircle = function (handler) {
            _viewer.GetMapFrame().DigitizeCircle(handler);
        }
    }

    //SetSelectionXML
    if (this.isFusion) {
        this.setSelectionXml = function (xml) {
            if (xml)
                _viewer.setSelection(xml, false);
        };
    } else {
        this.setSelectionXml = function (xml) {
            if (xml)
                _viewer.SetSelectionXML(xml);
        }
    }
}