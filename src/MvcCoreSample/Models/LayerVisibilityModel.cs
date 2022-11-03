using System.Collections.Generic;

namespace MvcCoreSample.Models;

public class LayerVisiblity
{
    public string LayerName { get; set; }

    public bool GetVisibleResult { get; set; }

    public bool IsVisibleResult { get; set; }
}

public class LayerVisibilityModel : MapGuideCommandModel
{
    public IEnumerable<LayerVisiblity> Layers { get; set; }
}