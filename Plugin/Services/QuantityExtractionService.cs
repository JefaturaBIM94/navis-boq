using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class QuantityExtractionService : IQuantityExtractionService
    {
        private readonly ISelectionScopeService _scopeService;
        private readonly ISnapshotService _snapshotService;

        public QuantityExtractionService(
            ISelectionScopeService scopeService,
            ISnapshotService snapshotService)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        }

        public List<ElementSnapshot> ExtractSnapshots(RunOptions options)
        {
            var result = new List<ElementSnapshot>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _scopeService.ResolveScopeItems(options))
            {
                var snap = _snapshotService.TryBuildSnapshot(item);
                if (snap == null) continue;
                if (!seen.Add(snap.CanonicalId ?? "")) continue;

                result.Add(snap);
            }

            return result;
        }

        public List<ElementSnapshot> ExtractSnapshotsByCategories(RunOptions options, IEnumerable<string> categories)
        {
            var catSet = new HashSet<string>(
                (categories ?? Enumerable.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x)),
                StringComparer.OrdinalIgnoreCase);

            var result = new List<ElementSnapshot>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _scopeService.ResolveScopeItems(options))
            {
                var snap = _snapshotService.TryBuildSnapshot(item);
                if (snap == null) continue;
                if (!seen.Add(snap.CanonicalId ?? "")) continue;

                if (catSet.Count > 0 && !catSet.Contains(snap.Category ?? ""))
                    continue;

                result.Add(snap);
            }

            return result;
        }
    }
}