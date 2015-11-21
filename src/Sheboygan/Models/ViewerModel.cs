using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class ViewerModel
    {
        public ViewerModel(string mapDefinition)
        {
            this.MapDefinition = mapDefinition;
        }
        
        public string MapDefinition { get; }
    }
}
