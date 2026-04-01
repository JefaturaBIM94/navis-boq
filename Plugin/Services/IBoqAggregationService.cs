using System.Collections.Generic;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IBoqAggregationService
    {
        List<BoqSummaryRow> AggregateBoqRows(IEnumerable<BoqRow> rows);
        List<SteelSummaryRow> AggregateSteelRows(IEnumerable<SteelRow> rows);
    }
}