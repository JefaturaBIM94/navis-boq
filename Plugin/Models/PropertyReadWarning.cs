using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class PropertyReadWarning
    {
        public string ElementId { get; set; } = "";
        public string Category { get; set; } = "";
        public string PropertyGroup { get; set; } = "";
        public string PropertyInternalName { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
