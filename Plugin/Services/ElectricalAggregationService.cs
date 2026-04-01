using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class ElectricalAggregationService : IElectricalAggregationService
    {
        public List<ElectricalSummaryRow> Aggregate(List<ElectricalRunRow> rows)
        {
            return rows
                .GroupBy(x => new
                {
                    x.Nivel,
                    x.Sistema,
                    x.CategoriaBoq,
                    x.Familia,
                    x.Tipo,
                    x.Unidad
                })
                .Select(g => new ElectricalSummaryRow
                {
                    Nivel = g.Key.Nivel,
                    Sistema = g.Key.Sistema,
                    CategoriaBoq = g.Key.CategoriaBoq,
                    Familia = g.Key.Familia,
                    Tipo = g.Key.Tipo,
                    Unidad = g.Key.Unidad,
                    NumElementos = g.Count(),
                    CantidadTotal = g.Sum(x => x.Cantidad),
                    LongitudTotalMl = g.Sum(x => x.LongitudTotalMl),
                    NumTramos = g.Sum(x => x.NumTramos)
                })
                .ToList();
        }
    }
}