using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class BoqAggregationService : IBoqAggregationService
    {
        public List<BoqSummaryRow> AggregateBoqRows(IEnumerable<BoqRow> rows)
        {
            var result = new Dictionary<string, AggregateBucket>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows ?? Enumerable.Empty<BoqRow>())
            {
                if (row == null) continue;

                var key = string.Join("|",
                    row.Nivel ?? "",
                    row.Categoria ?? "",
                    row.Familia ?? "",
                    row.Tipo ?? "",
                    row.Unidad ?? "",
                    row.UbicacionEstructural ?? "");

                if (!result.TryGetValue(key, out var bucket))
                {
                    bucket = new AggregateBucket
                    {
                        Level = row.Nivel ?? "",
                        Category = row.Categoria ?? "",
                        Family = row.Familia ?? "",
                        Type = row.Tipo ?? "",
                        Unit = row.Unidad ?? "",
                        TypeDesc = row.TipoDesc ?? "",
                        TypeMaterial = row.TipoMaterial ?? "",
                        TypeWidth = row.TipoAncho,
                        TypeThickness = row.TipoEspesor,
                        StructuralLocation = row.UbicacionEstructural ?? ""
                    };
                    result[key] = bucket;
                }

                bucket.Count++;
                bucket.LengthTotal += row.Longitud;
                bucket.AreaTotal += row.Area;
                bucket.VolumeTotal += row.Volumen;
                bucket.QuantityTotal += row.Cantidad;
            }

            return result.Values
                .Select(b => new BoqSummaryRow
                {
                    Nivel = b.Level,
                    Cat = b.Category,
                    Familia = b.Family,
                    Tipo = b.Type,
                    TipoDesc = b.TypeDesc,
                    TipoMaterial = b.TypeMaterial,
                    TipoAncho = Math.Round(b.TypeWidth, 4),
                    TipoEspesor = Math.Round(b.TypeThickness, 4),
                    Area = Math.Round(b.AreaTotal, 2),
                    Vol = Math.Round(b.VolumeTotal, 2),
                    Long_ = Math.Round(b.LengthTotal, 2),
                    Cantidad = Math.Round(b.QuantityTotal, 2),
                    Unidad = b.Unit,
                    N = b.Count,
                    UbicacionEstructural = b.StructuralLocation
                })
                .OrderBy(r => r.Cat)
                .ThenBy(r => r.Nivel)
                .ThenBy(r => r.Tipo)
                .ToList();
        }

        public List<SteelSummaryRow> AggregateSteelRows(IEnumerable<SteelRow> rows)
        {
            var result = new Dictionary<string, SteelAggregateBucket>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows ?? Enumerable.Empty<SteelRow>())
            {
                if (row == null) continue;

                var key = string.Join("|",
                    row.Nivel ?? "",
                    row.Categoria ?? "",
                    row.Familia ?? "",
                    row.Tipo ?? "",
                    row.SectionName ?? "",
                    row.SectionShape ?? "",
                    row.CodeName ?? "");

                if (!result.TryGetValue(key, out var bucket))
                {
                    bucket = new SteelAggregateBucket
                    {
                        Level = row.Nivel ?? "",
                        Category = row.Categoria ?? "",
                        Family = row.Familia ?? "",
                        Type = row.Tipo ?? "",
                        SectionName = row.SectionName ?? "",
                        SectionShape = row.SectionShape ?? "",
                        CodeName = row.CodeName ?? "",
                        NominalWeightKgm = row.NominalWeight
                    };
                    result[key] = bucket;
                }

                bucket.NumPieces++;
                bucket.LengthTotalM += row.Length;
                bucket.VolumeTotalM3 += row.Volume;
                bucket.PesoTotalKg += row.PesoKg;

                if (string.Equals(row.Metodo, "2025+", StringComparison.OrdinalIgnoreCase))
                {
                    bucket.Metodo2025Count++;
                }
                else if (
                    string.Equals(row.Metodo, "Vol×ρ", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(row.Metodo, "CustomWeight", StringComparison.OrdinalIgnoreCase))
                {
                    bucket.MetodoFallbackCount++;
                }
                else
                {
                    bucket.MetodoNDCount++;
                }
            }

            return result.Values
                .Select(b =>
                {
                    string metodo =
                        b.Metodo2025Count > 0 && b.MetodoFallbackCount > 0 ? "Mixto" :
                        b.Metodo2025Count > 0 ? "2025+" :
                        b.MetodoFallbackCount > 0 ? "Fallback" : "N/D";

                    string advertencia =
                        b.MetodoFallbackCount > 0 ? "Incluye piezas calculadas por fallback (Vol×ρ o CustomWeight)." :
                        b.MetodoNDCount > 0 ? "Incluye piezas sin datos de peso." :
                        "";

                    return new SteelSummaryRow
                    {
                        Nivel = b.Level,
                        Categoria = b.Category,
                        Familia = b.Family,
                        Tipo = b.Type,
                        SectionName = b.SectionName,
                        SectionShape = b.SectionShape,
                        CodeName = b.CodeName,
                        NominalWeight = Math.Round(b.NominalWeightKgm, 4),
                        NumPiezas = b.NumPieces,
                        LengthTotal = Math.Round(b.LengthTotalM, 3),
                        VolumeTotal = Math.Round(b.VolumeTotalM3, 4),
                        PesoKg = Math.Round(b.PesoTotalKg, 2),
                        PesoTonRef = b.PesoTotalKg >= 1000
                            ? (double?)Math.Round(b.PesoTotalKg / 1000.0, 3)
                            : null,
                        Metodo = metodo,
                        Advertencia = advertencia
                    };
                })
                .OrderBy(x => x.Nivel)
                .ThenBy(x => x.Categoria)
                .ThenBy(x => x.Tipo)
                .ToList();
        }
    }
}
