using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    /// <summary>
    /// Snapshot de una instancia del modelo: datos mínimos para cuantificar.
    /// </summary>
    public class ElementSnapshot
    {
        public string CanonicalId { get; set; } = "";
        public string ElementId { get; set; } = "";
        public string Level { get; set; } = "Sin nivel";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string Material { get; set; } = "";
        public string Mark { get; set; } = "";
        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double VolumeM3 { get; set; }
        public string TypeDesc { get; set; } = "";
        public string TypeMaterial { get; set; } = "";
        public double TypeWidth { get; set; }
        public double TypeThickness { get; set; }
        // Datos de acero (si aplican)
        public double NominalWeightKgm { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string SourceSystem { get; set; } = "Revit";
    }

    public class BoqRow
    {
        public string Nivel { get; set; } = "Sin nivel";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string TipoDesc { get; set; } = "";
        public string TipoMaterial { get; set; } = "";
        public double TipoAncho { get; set; }
        public double TipoEspesor { get; set; }
        public double Area { get; set; }
        public double Volumen { get; set; }
        public double Longitud { get; set; }
        public double Cantidad { get; set; }
        public string Unidad { get; set; } = "pza";
        public string ElemId { get; set; } = "";
        public string UbicacionEstructural { get; set; } = "";
    }

    public class BoqSummaryRow
    {
        public string Nivel { get; set; } = "";
        public string Cat { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string TipoDesc { get; set; } = "";
        public string TipoMaterial { get; set; } = "";
        public double TipoAncho { get; set; }
        public double TipoEspesor { get; set; }
        public double Area { get; set; }
        public double Vol { get; set; }
        public double Long_ { get; set; }
        public double Cantidad { get; set; }
        public string Unidad { get; set; } = "";
        public int N { get; set; }
        public string UbicacionEstructural { get; set; } = "";
    }

    public class AggregateBucket
    {
        public string Level { get; set; } = "";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string Unit { get; set; } = "";
        public string TypeDesc { get; set; } = "";
        public string TypeMaterial { get; set; } = "";
        public double TypeWidth { get; set; }
        public double TypeThickness { get; set; }
        public string StructuralLocation { get; set; } = "";
        public int Count { get; set; }
        public double LengthTotal { get; set; }
        public double AreaTotal { get; set; }
        public double VolumeTotal { get; set; }
        public double QuantityTotal { get; set; }
    }

    public class SteelRow
    {
        public string Nivel { get; set; } = "Sin nivel";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public double NominalWeight { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string MaterialEst { get; set; } = "";
        public double Length { get; set; }
        public double Volume { get; set; }
        public double PesoKg { get; set; }
        public string Metodo { get; set; } = "N/D";
        public string ElemId { get; set; } = "";
        public string Mark { get; set; } = "";
    }

    public class SteelSummaryRow
    {
        public string Nivel { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public double NominalWeight { get; set; }
        public int NumPiezas { get; set; }
        public double LengthTotal { get; set; }
        public double VolumeTotal { get; set; }
        public double PesoKg { get; set; }
        public double? PesoTonRef { get; set; }
        public string Metodo { get; set; } = "";
        public string Advertencia { get; set; } = "";
    }

    public class SteelAggregateBucket
    {
        public string Level { get; set; } = "";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public double NominalWeightKgm { get; set; }
        public int NumPieces { get; set; }
        public double LengthTotalM { get; set; }
        public double VolumeTotalM3 { get; set; }
        public double PesoTotalKg { get; set; }
        public int Metodo2025Count { get; set; }
        public int MetodoFallbackCount { get; set; }
        public int MetodoNDCount { get; set; }
    }

    public class SteelTypeCache
    {
        public string TypeKey { get; set; } = "";
        public double NominalWeightKgm { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string Material { get; set; } = "";
    }
}
