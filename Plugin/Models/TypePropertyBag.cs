using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class TypePropertyBag
    {
        public string TypeDescription { get; set; } = "";
        public string StructuralMaterial { get; set; } = "";
        public double? WidthM { get; set; }
        public double? ThicknessM { get; set; }
        public double? NominalWeightKgm { get; set; }
        public double? LinearWeightKgm { get; set; }
        public double? DepthM { get; set; }
        public double? WidthXM { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string CustomWeightRaw { get; set; } = "";
        public string FamilyTypeName { get; set; } = "";
        public string TypeNodeName { get; set; } = "";
        public string CategoryDisplay { get; set; } = "";
        public string LoadClassification { get; set; } = "";
        public string KeynoteNote { get; set; } = "";
        public string TypeComments { get; set; } = "";
        public string Url { get; set; } = "";
        public double? DimensionA { get; set; }
        public double? DimensionB { get; set; }
    }
}
