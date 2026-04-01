using System.Collections.Generic;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IQuantityExtractionService
    {
        List<ElementSnapshot> ExtractSnapshots(RunOptions options);
        List<ElementSnapshot> ExtractSnapshotsByCategories(RunOptions options, IEnumerable<string> categories);
    }
}