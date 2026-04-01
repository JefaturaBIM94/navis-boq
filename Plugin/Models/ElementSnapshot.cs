namespace NavisBOQ.Plugin.Models
{
    public class ElementSnapshot
    {
        public string CanonicalId { get; set; } = "";
        public string ElementId { get; set; } = "";
        public string Level { get; set; } = "Sin nivel";
        public string Category { get; set; } = "";
        public string CategoryId { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string Material { get; set; } = "";
        public string Mark { get; set; } = "";

        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double VolumeM3 { get; set; }
        public double CutLengthM { get; set; }

        public string TypeDesc { get; set; } = "";
        public string TypeMaterial { get; set; } = "";
        public double TypeWidth { get; set; }
        public double TypeThickness { get; set; }

        public double NominalWeightKgm { get; set; }
        public double LinearWeightKgm { get; set; }
        public double DepthM { get; set; }
        public double WidthXM { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        /// <summary>
        /// 
        /// 
        /// 
        /// </summary>
        /// 
        ///nuevos campos electricos
        public string SystemName { get; set; } = "Sin sistema MEP";
        public string SystemType { get; set; } = "";
        public string SystemClassification { get; set; } = "";
        public string ElectricalData { get; set; } = "";
        public string PanelName { get; set; } = "";
        public string MainBreakerPower { get; set; } = "";
        public string CustomPartida { get; set; } = "";
        public string OmniClassTitle { get; set; } = "";
        public string PieceType { get; set; } = "";
        public string SizeText { get; set; } = "";
        public double LengthByInstanceM { get; set; }
        public string FamilyTypeName { get; set; } = "";
        public string TypeNodeName { get; set; } = "";
        public string CategoryDisplay { get; set; } = "";
        public string LoadClassification { get; set; } = "";
        public string KeynoteNote { get; set; } = "";
        public string TypeComments { get; set; } = "";
        public string Url { get; set; } = "";
        public string PanelInstance { get; set; } = "";
        public double DimensionA { get; set; }
        public double DimensionB { get; set; }

        public string SourceSystem { get; set; } = "";

        // Hotfix: textual VDC_WEIGHT fallback parsed by SteelWeightService
        public string CustomWeightRaw { get; set; } = "";

        public string ResolvedFrom { get; set; } = "";
        public string LevelSource { get; set; } = "";
        public string GeometryConfidence { get; set; } = "";
        public bool NestedFamilyDetected { get; set; }
        public bool PartialData { get; set; }
    }
}
