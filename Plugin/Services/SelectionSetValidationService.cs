using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class SelectionSetValidationService : ISelectionSetValidationService
    {
        private readonly ISelectionScopeService _scopeService;
        private readonly IPropertyReaderService _propertyReader;
        private readonly IModelCategoryAliasService _aliasService;

        public SelectionSetValidationService(
            ISelectionScopeService scopeService,
            IPropertyReaderService propertyReader,
            IModelCategoryAliasService aliasService)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
        }

        public SelectionSetValidationResult ValidateForSteel(RunOptions options)
        {
            var result = new SelectionSetValidationResult
            {
                ScopeName = options?.SelectionSet ?? options?.ScopeMode ?? "unknown"
            };

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _scopeService.ResolveScopeItems(options))
            {
                result.TotalVisited++;

                bool hasGeometry = false;
                try { hasGeometry = item.HasGeometry; } catch { }
                if (hasGeometry) result.GeometricNodes++;

                var inst = _propertyReader.ResolveInstanceNode(item) ?? item;
                var cat = _propertyReader.ReadCategory(inst) ?? "";
                var catId = _propertyReader.ReadCategoryId(inst) ?? "";
                var normalized = _aliasService.NormalizeStructuralCategory(cat, catId);

                if (string.IsNullOrWhiteSpace(normalized))
                    continue;

                result.ElementNodes++;

                if (!counts.ContainsKey(normalized))
                    counts[normalized] = 0;
                counts[normalized]++;

                if (_aliasService.IsStructuralFraming(normalized, catId) || _aliasService.IsStructuralColumn(normalized, catId))
                    result.ContainsSteelCandidates = true;

                if (_aliasService.IsGenericModel(normalized, catId) ||
                    normalized.IndexOf("texto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalized.IndexOf("annotation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.ContainsGenericOrAnnotations = true;
                }
            }

            result.TopCategories = counts
                .OrderByDescending(x => x.Value)
                .Take(10)
                .Select(x => $"{x.Key} ({x.Value})")
                .ToList();

            if (result.ContainsSteelCandidates)
            {
                result.RecommendedMode = "auto_summary_only";
                result.Message = "El set contiene acero estructural reconocido. Se recomienda permitir la corrida automática y degradar a resumen si el alcance es pesado.";
            }
            else
            {
                result.RecommendedMode = "blocked_wrong_category";
                result.Message = "El set no contiene categorías de acero reconocidas o está compuesto por nodos padre / wrappers / anotación.";
            }

            return result;
        }

        public SelectionSetValidationResult ValidateForElectrical(RunOptions options)
        {
            var result = new SelectionSetValidationResult
            {
                ScopeName = options?.SelectionSet ?? options?.ScopeMode ?? "unknown"
            };

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var classifier = new ElectricalCategoryClassifierService();

            foreach (var item in _scopeService.ResolveScopeItems(options))
            {
                result.TotalVisited++;

                bool hasGeometry = false;
                try { hasGeometry = item.HasGeometry; } catch { }
                if (hasGeometry) result.GeometricNodes++;

                var inst = _propertyReader.ResolveInstanceNode(item) ?? item;
                var cat = _propertyReader.ReadCategory(inst) ?? "";
                var catId = _propertyReader.ReadCategoryId(inst) ?? "";
                var normalized = _aliasService.NormalizeStructuralCategory(cat, catId);

                var effectiveCategory = string.IsNullOrWhiteSpace(normalized) ? (cat ?? "").Trim() : normalized;

                if (string.IsNullOrWhiteSpace(effectiveCategory))
                    continue;

                result.ElementNodes++;

                if (!counts.ContainsKey(effectiveCategory))
                    counts[effectiveCategory] = 0;
                counts[effectiveCategory]++;

                if (classifier.TryClassify(effectiveCategory, out _, out _))
                {
                    result.ContainsElectricalCandidates = true;
                }

                if (string.Equals(effectiveCategory, "Pipes", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(effectiveCategory, "Pipe Fittings", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(effectiveCategory, "Plumbing Fixtures", StringComparison.OrdinalIgnoreCase))
                {
                    result.ContainsForbiddenPlumbing = true;
                }

                if (_aliasService.IsGenericModel(effectiveCategory, catId) ||
                    effectiveCategory.IndexOf("texto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    effectiveCategory.IndexOf("annotation", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    result.ContainsGenericOrAnnotations = true;
                }
            }

            result.TopCategories = counts
                .OrderByDescending(x => x.Value)
                .Take(10)
                .Select(x => $"{x.Key} ({x.Value})")
                .ToList();

            if (result.ContainsElectricalCandidates)
            {
                result.RecommendedMode = "auto_summary_only";

                if (result.ContainsForbiddenPlumbing)
                {
                    result.Message = "El set contiene categorías válidas de Corrida 4 pero también mezcla plumbing. Se permite correr con advertencia.";
                }
                else
                {
                    result.Message = "El set contiene categorías válidas de Corrida 4. Se permite la ejecución.";
                }

                return result;
            }

            result.RecommendedMode = "blocked_wrong_category";
            result.Message = "El set no contiene categorías reconocidas por Corrida 4.";
            return result;
        }
    }
}
