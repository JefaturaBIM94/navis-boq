using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Constants;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class ElectricalDetailExtractionService : IElectricalDetailExtractionService
    {
        private readonly ISelectionScopeService _scopeService;
        private readonly ISnapshotService _snapshotService;
        private readonly IPropertyReaderService _propertyReader;
        private readonly IElectricalCategoryClassifierService _classifier;
        private readonly IDetailFieldProfileService _profileService;
        private readonly IDetailExpansionPolicyService _policyService;

        public ElectricalDetailExtractionService(
            ISelectionScopeService scopeService,
            ISnapshotService snapshotService,
            IPropertyReaderService propertyReader,
            IElectricalCategoryClassifierService classifier,
            IDetailFieldProfileService profileService,
            IDetailExpansionPolicyService policyService)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
            _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
            _policyService = policyService ?? throw new ArgumentNullException(nameof(policyService));
        }

        public ToolEnvelope<object> ExpandDetail(DetailExpansionRequest request)
        {
            request = _policyService.NormalizeRequest(request ?? new DetailExpansionRequest());

            var warnings = new List<string>();
            var propertyWarnings = new List<PropertyReadWarning>();
            var diagnostics = new DetailExpansionDiagnostics
            {
                Mode = "lazy_detail"
            };

            var rows = new List<ElectricalDetailRow>();
            var fields = _profileService.GetProfile(request.RunName, request.DetailProfile);

            var typeCache = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            var familyCache = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var scopeItems = _scopeService.ResolveScopeItems(request.Options).ToList();
            diagnostics.RequestedItems = scopeItems.Count;

            foreach (var item in scopeItems)
            {
                if (item == null) continue;

                var snap = SafeBuildSnapshot(item);
                if (snap == null)
                {
                    diagnostics.SkippedByFilter++;
                    continue;
                }

                if (!seen.Add(snap.CanonicalId ?? Guid.NewGuid().ToString("N")))
                    continue;

                if (!_classifier.TryClassify(snap.Category, out var boqCategory, out _))
                {
                    diagnostics.SkippedByFilter++;
                    continue;
                }

                if (request.Categories != null && request.Categories.Count > 0)
                {
                    bool categoryMatch = request.Categories.Any(x =>
                        string.Equals(x?.Trim(), snap.Category?.Trim(), StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x?.Trim(), boqCategory?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!categoryMatch)
                    {
                        diagnostics.SkippedByFilter++;
                        continue;
                    }
                }

                if (request.Types != null && request.Types.Count > 0)
                {
                    bool typeMatch = request.Types.Any(x =>
                        string.Equals(x?.Trim(), snap.Type?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!typeMatch)
                    {
                        diagnostics.SkippedByFilter++;
                        continue;
                    }
                }

                if (rows.Count >= request.MaxItems)
                {
                    diagnostics.Truncated = true;
                    warnings.Add($"La expansión lazy fue truncada en {request.MaxItems} elementos para proteger estabilidad.");
                    break;
                }

                var row = new ElectricalDetailRow
                {
                    ElementId = snap.ElementId ?? "",
                    Nivel = snap.Level ?? "Sin nivel",
                    Sistema = string.IsNullOrWhiteSpace(snap.SystemName) ? "Sin sistema MEP" : snap.SystemName,
                    CategoriaBoq = boqCategory,
                    CategoriaRevit = snap.Category ?? "",
                    Familia = snap.Family ?? "",
                    Tipo = snap.Type ?? "",
                    Description = snap.TypeDesc ?? "",
                    ResolvedFrom = snap.ResolvedFrom ?? "",
                    DetailProfile = request.DetailProfile ?? DetailProfileNames.TypeLight
                };

                foreach (var field in fields)
                {
                    ApplyField(item, snap, row, field, typeCache, familyCache, propertyWarnings, diagnostics);
                }

                rows.Add(row);
            }

            diagnostics.ExpandedItems = rows.Count;

            if (propertyWarnings.Count > 0)
            {
                warnings.Add($"Se detectaron {propertyWarnings.Count} lecturas faltantes o no disponibles. La expansión continuó sin crashear.");
            }

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "expand_electrical_detail",
                ScopeMode = request.Options?.ScopeMode ?? "all",
                OutputMode = "detail",
                Warnings = warnings,
                UserMessage = rows.Count == 0
                    ? "No se encontraron elementos eléctricos válidos para expandir detalle."
                    : $"OK - {rows.Count} elementos expandidos con perfil '{request.DetailProfile}'.",
                Data = new
                {
                    rutina = "Expand Electrical Detail",
                    perfil = request.DetailProfile,
                    total_candidatos = diagnostics.RequestedItems,
                    expandidos = diagnostics.ExpandedItems,
                    diagnostico = diagnostics,
                    warnings_propiedades = propertyWarnings.Take(100).ToList(),
                    detalle = rows
                }
            };
        }

        private ElementSnapshot SafeBuildSnapshot(ModelItem item)
        {
            try
            {
                return _snapshotService.TryBuildSnapshot(item);
            }
            catch
            {
                return null;
            }
        }

        private void ApplyField(
            ModelItem item,
            ElementSnapshot snap,
            ElectricalDetailRow row,
            PropertyFieldRequest field,
            Dictionary<string, Dictionary<string, object>> typeCache,
            Dictionary<string, Dictionary<string, object>> familyCache,
            List<PropertyReadWarning> propertyWarnings,
            DetailExpansionDiagnostics diagnostics)
        {
            if (field == null) return;

            object value = null;
            string outputField = field.OutputField ?? "";

            try
            {
                switch ((field.SourceNode ?? "").Trim().ToLowerInvariant())
                {
                    case "type":
                        value = GetTypeFieldValue(item, snap, field, typeCache, diagnostics);
                        break;

                    case "family":
                        value = GetFamilyFieldValue(item, snap, field, familyCache, diagnostics);
                        break;

                    case "instance":
                    case "custom":
                        value = GetInstanceOrCustomFieldValue(item, snap, field, diagnostics);
                        break;
                }
            }
            catch
            {
                value = null;
            }

            if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            {
                propertyWarnings.Add(new PropertyReadWarning
                {
                    ElementId = snap.ElementId ?? "",
                    Category = snap.Category ?? "",
                    PropertyGroup = field.SourceNode ?? "",
                    PropertyInternalName = field.PropertyInternalName ?? "",
                    Message = "Propiedad no disponible o vacía."
                });
                return;
            }

            AssignRowField(row, outputField, value);
        }

        private object GetTypeFieldValue(
            ModelItem item,
            ElementSnapshot snap,
            PropertyFieldRequest field,
            Dictionary<string, Dictionary<string, object>> typeCache,
            DetailExpansionDiagnostics diagnostics)
        {
            string typeKey = BuildTypeKey(snap);

            if (!typeCache.TryGetValue(typeKey, out var bag))
            {
                bag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                typeCache[typeKey] = bag;
            }

            if (bag.TryGetValue(field.OutputField, out var cached))
                return cached;

            object value = field.PropertyInternalName switch
            {
                "LcRevitPropertyElementName" => _propertyReader.ReadTypeNodeName(item),
                "LcRevitPropertyElementCategory" => _propertyReader.ReadCategoryDisplay(item),
                "lcldrevit_parameter_Clasificaci?n de carga_PG_ELECTRICAL" => _propertyReader.ReadLoadClassification(item),
                "lcldrevit_parameter_-1010103" => snap.TypeDesc ?? "",
                "lcldrevit_parameter_-1140422" => _propertyReader.ReadKeynoteNote(item),
                "lcldrevit_parameter_A_PG_DIMENSIONS" => _propertyReader.ReadDimensionA(item) ?? 0,
                "lcldrevit_parameter_B_PG_DIMENSIONS" => _propertyReader.ReadDimensionB(item) ?? 0,
                _ => null
            };

            if (value == null) diagnostics.MissingTypeNode++;
            bag[field.OutputField] = value;

            return value;
        }

        private object GetFamilyFieldValue(
            ModelItem item,
            ElementSnapshot snap,
            PropertyFieldRequest field,
            Dictionary<string, Dictionary<string, object>> familyCache,
            DetailExpansionDiagnostics diagnostics)
        {
            string familyKey = BuildFamilyKey(snap);

            if (!familyCache.TryGetValue(familyKey, out var bag))
            {
                bag = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                familyCache[familyKey] = bag;
            }

            if (bag.TryGetValue(field.OutputField, out var cached))
                return cached;

            object value = field.PropertyInternalName switch
            {
                "LcOaSceneBaseUserName" => _propertyReader.ReadFamilyTypeName(item),
                "lcldrevit_parameter_-1002503" => snap.OmniClassTitle ?? "",
                "lcldrevit_parameter_-1114206" => snap.PieceType ?? "",
                "lcldrevit_parameter_-1010105" => _propertyReader.ReadTypeComments(item),
                "lcldrevit_parameter_-1010103" => snap.TypeDesc ?? "",
                "lcldrevit_parameter_-1010104" => _propertyReader.ReadUrl(item),
                _ => null
            };

            if (value == null) diagnostics.MissingFamilyNode++;
            bag[field.OutputField] = value;

            return value;
        }

        private object GetInstanceOrCustomFieldValue(
            ModelItem item,
            ElementSnapshot snap,
            PropertyFieldRequest field,
            DetailExpansionDiagnostics diagnostics)
        {
            object value = field.PropertyInternalName switch
            {
                "lcldrevit_parameter_-1114241" => _propertyReader.ReadElectricalData(item),
                "lcldrevit_parameter_-1140078" => _propertyReader.ReadPanelName(item),
                "lcldrevit_parameter_-1140140" => _propertyReader.ReadMainBreakerPower(item),
                "lcldrevit_parameter_-1140104" => _propertyReader.ReadPanelInstance(item),
                "lcldrevit_parameter_TR3Z - Partida_PG_TEXT" => _propertyReader.ReadCustomPartida(item),
                "lcldrevit_parameter_-1114240" => _propertyReader.ReadSizeText(item),
                "lcldrevit_parameter_-1004005" => _propertyReader.ReadLengthByInstanceM(item) ?? 0,
                _ => null
            };

            if (value == null) diagnostics.MissingInstanceProps++;
            return value;
        }

        private static void AssignRowField(ElectricalDetailRow row, string outputField, object value)
        {
            switch (outputField)
            {
                case "FamilyTypeName": row.FamilyTypeName = value?.ToString() ?? ""; break;
                case "TypeNodeName": row.TypeNodeName = value?.ToString() ?? ""; break;
                case "CategoryDisplay": row.CategoryDisplay = value?.ToString() ?? ""; break;
                case "LoadClassification": row.LoadClassification = value?.ToString() ?? ""; break;
                case "Description": row.Description = value?.ToString() ?? ""; break;
                case "OmniClassTitle": row.OmniClassTitle = value?.ToString() ?? ""; break;
                case "PieceType": row.PieceType = value?.ToString() ?? ""; break;
                case "TypeComments": row.TypeComments = value?.ToString() ?? ""; break;
                case "Url": row.Url = value?.ToString() ?? ""; break;
                case "KeynoteNote": row.KeynoteNote = value?.ToString() ?? ""; break;
                case "ElectricalData": row.ElectricalData = value?.ToString() ?? ""; break;
                case "PanelName": row.PanelName = value?.ToString() ?? ""; break;
                case "MainBreakerPower": row.MainBreakerPower = value?.ToString() ?? ""; break;
                case "PanelInstance": row.PanelInstance = value?.ToString() ?? ""; break;
                case "CustomPartida": row.CustomPartida = value?.ToString() ?? ""; break;
                case "SizeText": row.SizeText = value?.ToString() ?? ""; break;
                case "LengthByInstanceMl":
                    row.LengthByInstanceMl = ToDouble(value);
                    break;
                case "DimensionA":
                    row.DimensionA = ToDouble(value);
                    break;
                case "DimensionB":
                    row.DimensionB = ToDouble(value);
                    break;
            }
        }

        private static double ToDouble(object value)
        {
            if (value == null) return 0;
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            double parsed;
            return double.TryParse(value.ToString(), out parsed) ? parsed : 0;
        }

        private static string BuildTypeKey(ElementSnapshot snap)
        {
            return $"TYPE|{snap.Category}|{snap.Family}|{snap.Type}";
        }

        private static string BuildFamilyKey(ElementSnapshot snap)
        {
            return $"FAMILY|{snap.Category}|{snap.Family}";
        }
    }
}