using System.Collections.Generic;

namespace MvcCoreSample.Models
{
    public class SelectedParcel
    {
        public string Name { get; set; }

        public string Address { get; set; }
    }

    public class SelectedFeaturesModel : MapGuideCommandModel
    {
        public bool HasSelectedLayers { get; set; }
        public List<SelectedParcel> Results { get; set; }
    }
}