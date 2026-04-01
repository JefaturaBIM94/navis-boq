using System;
using System.Linq;
using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin.Services
{
    public class NodeResolutionPolicyService : INodeResolutionPolicyService
    {
        private readonly IPropertyReaderService _propertyReader;
        private readonly IModelCategoryAliasService _aliasService;
        private readonly IModelStructurePolicyService _structurePolicy;

        public NodeResolutionPolicyService(
            IPropertyReaderService propertyReader,
            IModelCategoryAliasService aliasService,
            IModelStructurePolicyService structurePolicy)
        {
            _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
            _structurePolicy = structurePolicy ?? throw new ArgumentNullException(nameof(structurePolicy));
        }

        public ModelItem ResolvePreferredInstanceNode(ModelItem item, string category, string categoryId = "")
        {
            if (item == null) return null;

            var inst = _propertyReader.ResolveInstanceNode(item);
            if (inst == null) return null;

            if (_aliasService.IsStructuralFoundation(category, categoryId))
                return ResolveHostByCategory(inst, "Structural Foundations");

            if (_aliasService.IsGenericModel(category, categoryId))
                return ResolveHostByCategory(inst, "Generic Models");

            return inst;
        }

        public string ResolvePreferredLevel(ModelItem item, string category)
        {
            if (item == null) return "Sin nivel";

            var preferred = ResolvePreferredInstanceNode(item, category);
            var lvl = ReadLevelFromTree(preferred);
            if (!string.IsNullOrWhiteSpace(lvl))
                return lvl;

            return ReadLevelFromTree(item);
        }

        public bool IsNestedOrSubcomponent(ModelItem item, string category)
        {
            if (item == null) return false;
            if (!_aliasService.IsGenericModel(category)) return false;

            try
            {
                var inst = _propertyReader.ResolveInstanceNode(item);
                if (inst == null) return false;

                var fam = _propertyReader.ReadFamily(inst) ?? "";
                var type = _propertyReader.ReadType(inst) ?? "";

                if (string.IsNullOrWhiteSpace(fam) && string.IsNullOrWhiteSpace(type))
                    return true;

                var name = (item.DisplayName ?? "").Trim();
                if (name.IndexOf("nested", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            catch { }

            return false;
        }

        public string GetResolvedFrom(string category)
        {
            if (_aliasService.IsStructuralFoundation(category)) return "host";
            if (_aliasService.IsGenericModel(category)) return "host";
            return "instance";
        }

        public string GetLevelSource(ModelItem item, string category)
        {
            if (_aliasService.IsGenericModel(category) || _aliasService.IsStructuralFoundation(category))
                return "host";

            return "tree";
        }

        public string GetGeometryConfidence(ModelItem item, string category)
        {
            if (item == null) return "low";

            if (_aliasService.IsGenericModel(category) || _aliasService.IsStructuralFoundation(category))
                return "medium";

            try
            {
                return item.HasGeometry ? "high" : "medium";
            }
            catch
            {
                return "low";
            }
        }

        private ModelItem ResolveHostByCategory(ModelItem inst, string canonicalCategory)
        {
            ModelItem best = inst;
            int bestScore = int.MinValue;

            foreach (var a in inst.AncestorsAndSelf)
            {
                var cat = _propertyReader.ReadCategory(a) ?? "";
                var catId = _propertyReader.ReadCategoryId(a) ?? "";
                var normalized = _aliasService.NormalizeStructuralCategory(cat, catId);

                if (!string.Equals(normalized, canonicalCategory, StringComparison.OrdinalIgnoreCase))
                    continue;

                int score = _structurePolicy.ScoreElementCandidate(a);
                if (score > bestScore)
                {
                    best = a;
                    bestScore = score;
                }
            }

            return best;
        }

        private string ReadLevelFromTree(ModelItem item)
        {
            if (item == null) return "Sin nivel";

            try
            {
                var chain = item.AncestorsAndSelf.Reverse().ToList();
                string fallbackNoLevel = "";

                for (int i = 0; i < chain.Count; i++)
                {
                    var current = chain[i];
                    var currName = (current?.DisplayName ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(currName))
                        continue;

                    if (_structurePolicy.IsNoLevelNode(currName))
                    {
                        fallbackNoLevel = currName;
                        continue;
                    }

                    if (_structurePolicy.IsIgnorableContainer(currName))
                        continue;

                    string prevName = i > 0 ? (chain[i - 1]?.DisplayName ?? "").Trim() : "";

                    if (_structurePolicy.IsWrapperNode(prevName) || _structurePolicy.IsNoLevelNode(prevName))
                        return currName;

                    if (_structurePolicy.LooksLikeLevelNode(currName))
                        return currName;
                }

                if (!string.IsNullOrWhiteSpace(fallbackNoLevel))
                    return fallbackNoLevel;
            }
            catch { }

            return "Sin nivel";
        }
    }
}