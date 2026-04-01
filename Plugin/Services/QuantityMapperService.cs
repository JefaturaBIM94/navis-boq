using System;
using System.Linq;
using NavisBOQ.Plugin.Constants;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class QuantityMapperService : IQuantityMapperService
    {
        public BoqRow ToBoqRow(ElementSnapshot snap)
        {
            var row = new BoqRow
            {
                Nivel = snap.Level ?? "Sin nivel",
                Familia = Clean(snap.Family),
                Tipo = Clean(snap.Type),
                TipoDesc = snap.TypeDesc ?? "",
                TipoMaterial = !string.IsNullOrWhiteSpace(snap.TypeMaterial) ? snap.TypeMaterial : (snap.Material ?? ""),
                TipoAncho = Math.Round(snap.TypeWidth, 4),
                TipoEspesor = Math.Round(snap.TypeThickness, 4),
                Area = Math.Round(snap.AreaM2, 4),
                Volumen = Math.Round(snap.VolumeM3, 4),
                Longitud = Math.Round(snap.LengthM, 4),
                ElemId = snap.ElementId ?? "",
                UbicacionEstructural = ""
            };

            string boq = snap.Category ?? "";
            string unit = "";

            var mapped = CategoryConstants.Mapa.FirstOrDefault(x =>
                string.Equals(x.Key, snap.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(mapped.Key))
            {
                boq = mapped.Value.Name;
                unit = mapped.Value.Unit;
            }

            row.Categoria = boq;

            if (unit == "pza" || CategoryConstants.EsPza.Contains(snap.Category ?? "") || CategoryConstants.EsPza.Contains(boq))
            {
                row.Cantidad = 1;
                row.Unidad = "pza";
            }
            else if (unit == "m2" && snap.AreaM2 > 0)
            {
                row.Cantidad = Math.Round(snap.AreaM2, 3);
                row.Unidad = "m2";
            }
            else if (unit == "m3" && snap.VolumeM3 > 0)
            {
                row.Cantidad = Math.Round(snap.VolumeM3, 3);
                row.Unidad = "m3";
            }
            else if (unit == "ml" && snap.LengthM > 0)
            {
                row.Cantidad = Math.Round(snap.LengthM, 3);
                row.Unidad = "ml";
            }
            else if (snap.AreaM2 > 0)
            {
                row.Cantidad = Math.Round(snap.AreaM2, 3);
                row.Unidad = "m2";
            }
            else if (snap.VolumeM3 > 0)
            {
                row.Cantidad = Math.Round(snap.VolumeM3, 3);
                row.Unidad = "m3";
            }
            else if (snap.LengthM > 0)
            {
                row.Cantidad = Math.Round(snap.LengthM, 3);
                row.Unidad = "ml";
            }
            else
            {
                row.Cantidad = 1;
                row.Unidad = "pza";
            }

            return row;
        }

        private static string Clean(string s)
        {
            return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
        }
    }
}