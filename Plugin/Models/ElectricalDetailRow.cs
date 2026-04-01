using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class ElectricalDetailRow
    {
        public string ElementId { get; set; } = "";
        public string Nivel { get; set; } = "Sin nivel";
        public string Sistema { get; set; } = "Sin sistema MEP";

        public string CategoriaBoq { get; set; } = "";
        public string CategoriaRevit { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";

        public string FamilyTypeName { get; set; } = "";
        public string TypeNodeName { get; set; } = "";
        public string CategoryDisplay { get; set; } = "";
        public string LoadClassification { get; set; } = "";
        public string Description { get; set; } = "";
        public string OmniClassTitle { get; set; } = "";
        public string PieceType { get; set; } = "";
        public string TypeComments { get; set; } = "";
        public string Url { get; set; } = "";
        public string KeynoteNote { get; set; } = "";

        public string ElectricalData { get; set; } = "";
        public string PanelName { get; set; } = "";
        public string MainBreakerPower { get; set; } = "";
        public string PanelInstance { get; set; } = "";
        public string CustomPartida { get; set; } = "";

        public string SizeText { get; set; } = "";
        public double LengthByInstanceMl { get; set; }
        public double DimensionA { get; set; }
        public double DimensionB { get; set; }

        public string ResolvedFrom { get; set; } = "";
        public string DetailProfile { get; set; } = "";
    }
}
