using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class SelectionScopeService : ISelectionScopeService
    {
        private readonly IPropertyReaderService _propertyReader;
        private readonly ISnapshotService _snapshotService;

        public SelectionScopeService(
            IPropertyReaderService propertyReader,
            ISnapshotService snapshotService)
        {
            _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        }

        public IEnumerable<ModelItem> ResolveScopeItems(RunOptions options)
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (doc == null) yield break;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<ModelItem> items = EnumerateRawScope(doc, options);

            foreach (var item in items)
            {
                if (item == null) continue;

                var inst = _propertyReader.ResolveInstanceNode(item) ?? item;
                var key = SafeCanonicalId(inst);

                if (!seen.Add(key))
                    continue;

                yield return inst;
            }
        }

        public ScopePreflight BuildPreflight(RunOptions options, ExecutionBudget budget)
        {
            var pre = new ScopePreflight
            {
                ScopeResolved = ResolveScopeLabel(options)
            };

            var levels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int visited = 0;
            int geometric = 0;
            int candidates = 0;

            foreach (var item in ResolveScopeItems(options))
            {
                visited++;

                bool hasGeometry = false;
                try { hasGeometry = item.HasGeometry; } catch { }
                if (hasGeometry) geometric++;

                var snap = _snapshotService.TryBuildSnapshot(item);
                if (snap == null) continue;

                candidates++;

                if (!string.IsNullOrWhiteSpace(snap.Level))
                    levels.Add(snap.Level);

                if (!string.IsNullOrWhiteSpace(snap.Category))
                    categories.Add(snap.Category);

                if (visited >= budget.MaxNodesToVisit)
                    break;
            }

            pre.VisitedNodes = visited;
            pre.GeometricItems = geometric;
            pre.CandidateItems = candidates;
            pre.DistinctLevels = levels.Count;
            pre.DistinctCategories = categories.Count;

            if (candidates <= budget.GreenCandidateLimit)
            {
                pre.RiskBand = "green";
                pre.AllowRun = true;
                pre.ForceSummary = false;
                pre.Message = "El alcance es seguro para corrida completa.";
            }
            else if (candidates <= budget.YellowCandidateLimit)
            {
                pre.RiskBand = "yellow";
                pre.AllowRun = true;
                pre.ForceSummary = true;
                pre.Message = "El alcance es grande; por estabilidad se recomienda solo resumen.";
                pre.SuggestedSegmentation.Add("Segmenta por nivel.");
                pre.SuggestedSegmentation.Add("Segmenta por Selection Set.");
            }
            else
            {
                pre.RiskBand = "red";
                pre.AllowRun = !options.StrictLimits;
                pre.ForceSummary = true;
                pre.Message = "El alcance excede el umbral seguro. Segmenta el modelo antes de correr.";
                pre.SuggestedSegmentation.Add("Usa Selection Sets más pequeños.");
                pre.SuggestedSegmentation.Add("Corre por nivel.");
            }

            return pre;
        }

        private IEnumerable<ModelItem> EnumerateRawScope(Document doc, RunOptions options)
        {
            string mode = (options?.ScopeMode ?? "all").Trim().ToLowerInvariant();

            switch (mode)
            {
                case "selection":
                    return EnumerateCurrentSelection(doc);

                case "selection_set":
                    return EnumerateSelectionSet(doc, options?.SelectionSet);

                case "level":
                    return EnumerateByLevel(doc, options?.Level);

                case "all":
                default:
                    return EnumerateWholeModel(doc);
            }
        }

        private IEnumerable<ModelItem> EnumerateWholeModel(Document doc)
        {
            foreach (var model in doc.Models)
            {
                if (model?.RootItem == null) continue;

                foreach (var item in model.RootItem.DescendantsAndSelf)
                    yield return item;
            }
        }

        private IEnumerable<ModelItem> EnumerateCurrentSelection(Document doc)
        {
            var selection = doc.CurrentSelection?.SelectedItems;
            if (selection == null) yield break;

            foreach (ModelItem selected in selection)
            {
                if (selected == null) continue;

                IEnumerable<ModelItem> nodes;
                try { nodes = selected.DescendantsAndSelf; }
                catch { nodes = new[] { selected }; }

                foreach (var item in nodes)
                    yield return item;
            }
        }

        private IEnumerable<ModelItem> EnumerateSelectionSet(Document doc, string selectionSetName)
        {
            if (string.IsNullOrWhiteSpace(selectionSetName))
                yield break;

            var set = FindSelectionSetByName(doc.SelectionSets, selectionSetName);
            if (set == null) yield break;

            ModelItemCollection items;
            try { items = set.GetSelectedItems(); }
            catch { yield break; }

            foreach (ModelItem selected in items)
            {
                if (selected == null) continue;

                IEnumerable<ModelItem> nodes;
                try { nodes = selected.DescendantsAndSelf; }
                catch { nodes = new[] { selected }; }

                foreach (var item in nodes)
                    yield return item;
            }
        }

        private IEnumerable<ModelItem> EnumerateByLevel(Document doc, string level)
        {
            if (string.IsNullOrWhiteSpace(level))
                yield break;

            foreach (var item in EnumerateWholeModel(doc))
            {
                var snap = _snapshotService.TryBuildSnapshot(item);
                if (snap == null) continue;

                if (string.Equals(snap.Level, level, StringComparison.OrdinalIgnoreCase))
                    yield return item;
            }
        }

        private SelectionSet FindSelectionSetByName(SavedItemCollection items, string name)
        {
            foreach (SavedItem item in items)
            {
                if (item == null) continue;

                if (item is SelectionSet ss &&
                    string.Equals(ss.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                    return ss;

                if (item is FolderItem folder)
                {
                    var nested = FindSelectionSetByName(folder.Children, name);
                    if (nested != null) return nested;
                }
            }

            return null;
        }

        private static string ResolveScopeLabel(RunOptions options)
        {
            if (options == null) return "all";

            if (string.Equals(options.ScopeMode, "selection_set", StringComparison.OrdinalIgnoreCase))
                return $"selection_set:{options.SelectionSet}";

            if (string.Equals(options.ScopeMode, "level", StringComparison.OrdinalIgnoreCase))
                return $"level:{options.Level}";

            return options.ScopeMode ?? "all";
        }

        private static string SafeCanonicalId(ModelItem item)
        {
            try
            {
                var guid = item.InstanceGuid;
                if (guid != Guid.Empty)
                    return guid.ToString();
            }
            catch { }

            try
            {
                return item.DisplayName ?? Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}