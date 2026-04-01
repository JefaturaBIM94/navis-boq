using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class PropertyFieldRequest
    {
        public string SourceNode { get; set; } = "";
        public string CategoryInternalName { get; set; } = "";
        public string PropertyInternalName { get; set; } = "";
        public string OutputField { get; set; } = "";
        public bool Required { get; set; }
    }
}
