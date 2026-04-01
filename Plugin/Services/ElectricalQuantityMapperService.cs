using System;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class ElectricalQuantityMapperService : IElectricalQuantityMapperService
    {
        public ElectricalRunRow Map(ElementSnapshot snap, string boqCategory, string unit)
        {
            var row = new ElectricalRunRow
            {
                Nivel = snap.Level ?? "Sin nivel",
                Sistema = string.IsNullOrWhiteSpace(snap.SystemName) ? "Sin sistema MEP" : snap.SystemName,
                CategoriaBoq = boqCategory,
                CategoriaRevit = snap.Category ?? "",
                Familia = snap.Family ?? "",
                Tipo = snap.Type ?? "",
                Descripcion = snap.TypeDesc ?? "",
                OmniClassTitle = snap.OmniClassTitle ?? "",
                PanelName = snap.PanelName ?? "",
                ElectricalData = snap.ElectricalData ?? "",
                PartidaCustom = snap.CustomPartida ?? "",
                ElemId = snap.ElementId ?? "",

                FamilyTypeName = snap.FamilyTypeName ?? "",
                TypeNodeName = snap.TypeNodeName ?? "",
                CategoryDisplay = snap.CategoryDisplay ?? "",
                LoadClassification = snap.LoadClassification ?? "",
                KeynoteNote = snap.KeynoteNote ?? "",
                TypeComments = snap.TypeComments ?? "",
                Url = snap.Url ?? "",
                PieceType = snap.PieceType ?? "",
                MainBreakerPower = snap.MainBreakerPower ?? "",
                PanelInstance = snap.PanelInstance ?? "",
                SizeText = snap.SizeText ?? "",
                LengthByInstanceMl = snap.LengthByInstanceM,
                DimensionA = snap.DimensionA,
                DimensionB = snap.DimensionB,

                Unidad = unit
            };

            if (IsTubeCategory(snap.Category))
            {
                double length = snap.LengthByInstanceM > 0 ? snap.LengthByInstanceM : snap.LengthM;

                row.Cantidad = length;
                row.LongitudTotalMl = length;
                row.NumTramos = 1;
                row.Unidad = "ml";
            }
            else
            {
                row.Cantidad = 1;
                row.Unidad = "pza";
                row.LongitudTotalMl = 0;
                row.NumTramos = 0;
            }

            return row;
        }

        private static bool IsTubeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;

            var c = category.Trim();

            return string.Equals(c, "Conduits", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(c, "Conduit", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(c, "Tubos", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(c, "Tubo", StringComparison.OrdinalIgnoreCase);
        }
    }
}
