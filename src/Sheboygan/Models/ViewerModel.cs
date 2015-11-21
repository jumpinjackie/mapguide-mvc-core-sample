using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class ViewerModel
    {
        public ViewerModel(string mapDefinition, string homeUrl)
        {
            this.MapDefinition = mapDefinition;
            this.HomeUrl = homeUrl;
        }

        public string HomeUrl { get; }
        
        public string MapDefinition { get; }
    }
}
