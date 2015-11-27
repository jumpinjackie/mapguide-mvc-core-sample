using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class MapGuideViewerInputModel
    {
        public string Session { get; set; }

        public string MapName { get; set; }
    }

    public class MapGuideViewModel
    {
        public string Session { get; set; }

        public string MapName { get; set; }
    }

    public class SearchInputModel : MapGuideViewerInputModel
    {
        public string By { get; set; }

        public string Query { get; set; }
    }

    public class SelectInputModel : MapGuideViewerInputModel
    {
        public int id { get; set; }
    }
}
