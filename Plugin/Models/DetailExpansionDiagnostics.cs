using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class DetailExpansionDiagnostics
    {
        public int RequestedItems { get; set; }
        public int ExpandedItems { get; set; }
        public int SkippedByFilter { get; set; }

        public int MissingTypeNode { get; set; }
        public int MissingFamilyNode { get; set; }
        public int MissingInstanceProps { get; set; }

        public bool Truncated { get; set; }
        public string Mode { get; set; } = "lazy_detail";
    }
}
