using System.Collections.Generic;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IElectricalAggregationService
    {
        List<ElectricalSummaryRow> Aggregate(List<ElectricalRunRow> rows);
    }
}
