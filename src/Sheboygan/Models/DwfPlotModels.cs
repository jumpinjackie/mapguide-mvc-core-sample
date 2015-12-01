using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class DwfPlotInputModel : MapGuideViewerInputModel
    {
        public bool UseLayout { get; set; }
    }

    public class DwfPlotAtScaleInputModel : DwfPlotInputModel
    {
        public double Scale { get; set; }
    }

    public class DwfPlotViewModel : MapGuideViewModel
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Scale { get; set; }
    }
}
