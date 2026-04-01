using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class Preconstruccion4Service : IPreconstruccion4Service
    {
        private readonly ISelectionScopeService _scopeService;
        private readonly IQuantityExtractionService _quantityExtractionService;
        private readonly IElectricalCategoryClassifierService _classifier;
        private readonly IElectricalQuantityMapperService _mapper;
        private readonly IElectricalAggregationService _aggregation;
        private readonly IExecutionModePolicyService _executionPolicy;
        private readonly ISelectionSetValidationService _selectionSetValidationService;

        public Preconstruccion4Service(
            ISelectionScopeService scopeService,
            IQuantityExtractionService quantityExtractionService,
            IElectricalCategoryClassifierService classifier,
            IElectricalQuantityMapperService mapper,
            IElectricalAggregationService aggregation,
            IExecutionModePolicyService executionPolicy,
            ISelectionSetValidationService selectionSetValidationService)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _quantityExtractionService = quantityExtractionService ?? throw new ArgumentNullException(nameof(quantityExtractionService));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _aggregation = aggregation ?? throw new ArgumentNullException(nameof(aggregation));
            _executionPolicy = executionPolicy ?? throw new ArgumentNullException(nameof(executionPolicy));
            _selectionSetValidationService = selectionSetValidationService ?? throw new ArgumentNullException(nameof(selectionSetValidationService));
        }

        public ToolEnvelope<object> Run(RunOptions options)
        {
            options = options ?? new RunOptions();

            string scope = (options.ScopeMode ?? "all").Trim().ToLowerInvariant();

            if (scope == "selection_set")
            {
                var validation = _selectionSetValidationService.ValidateForElectrical(options);

                if (!validation.ContainsElectricalCandidates)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_4",
                        ScopeMode = options.ScopeMode ?? "all",
                        OutputMode = "summary",
                        UserMessage = validation.Message,
                        Warnings = new List<string>
                        {
                            "El set no contiene categorías reconocidas por Corrida 4."
                        },
                        Data = new
                        {
                            validacion_set = new
                            {
                                total_visitados = validation.TotalVisited,
                                nodos_elemento = validation.ElementNodes,
                                nodos_geometricos = validation.GeometricNodes,
                                contiene_electricos = validation.ContainsElectricalCandidates,
                                contiene_plumbing = validation.ContainsForbiddenPlumbing,
                                contiene_genericos = validation.ContainsGenericOrAnnotations,
                                top_categorias = validation.TopCategories,
                                modo_recomendado = validation.RecommendedMode
                            }
                        }
                    };
                }
            }

            if (scope == "selection_set" || scope == "all" || scope == "level")
            {
                var earlyDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_4", options, null);

                if (!earlyDecision.AllowAutoRun)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_4",
                        ScopeMode = options.ScopeMode ?? "all",
                        OutputMode = "summary",
                        Warnings = earlyDecision.Warnings,
                        UserMessage = earlyDecision.Reason,
                        Data = new
                        {
                            politica_ejecucion = new
                            {
                                modo = earlyDecision.Mode,
                                razon = earlyDecision.Reason,
                                acciones = earlyDecision.SuggestedActions
                            }
                        }
                    };
                }

                if (earlyDecision.ForceSummary)
                    options.OutputMode = "summary";
            }

            var budget = BudgetProfiles.Corrida4;
            var preflight = _scopeService.BuildPreflight(options, budget);

            var execDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_4", options, preflight);

            if (!execDecision.AllowAutoRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_4",
                    ScopeMode = options.ScopeMode ?? "all",
                    OutputMode = "summary",
                    Preflight = preflight,
                    Warnings = execDecision.Warnings,
                    UserMessage = execDecision.Reason,
                    Data = new
                    {
                        politica_ejecucion = new
                        {
                            modo = execDecision.Mode,
                            razon = execDecision.Reason,
                            acciones = execDecision.SuggestedActions
                        }
                    }
                };
            }

            if (string.Equals(options.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                options.OutputMode = preflight.ForceSummary ? "summary" : "detail";

            if (execDecision.ForceSummary)
                options.OutputMode = "summary";

            if (preflight.ForceSummary && string.Equals(options.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                options.OutputMode = "summary";

            bool returnDetail = string.Equals(options.OutputMode, "detail", StringComparison.OrdinalIgnoreCase);

            var warnings = new List<string>();
            if (execDecision.Warnings != null && execDecision.Warnings.Count > 0)
                warnings.AddRange(execDecision.Warnings);

            var rows = new List<ElectricalRunRow>();
            int candidatesValidos = 0;

            var snapshots = _quantityExtractionService.ExtractSnapshots(options);
            int missingCustomPartida = 0;
            int missingPanelName = 0;
            int missingElectricalData = 0;
            int missingSizeText = 0;

            foreach (var snap in snapshots)
            {
                if (snap == null) continue;

                if (!_classifier.TryClassify(snap.Category, out var boqCategory, out var unit))
                    continue;

                candidatesValidos++;

                if (string.IsNullOrWhiteSpace(snap.CustomPartida))
                    missingCustomPartida++;

                if (string.IsNullOrWhiteSpace(snap.PanelName))
                    missingPanelName++;

                if (string.IsNullOrWhiteSpace(snap.ElectricalData))
                    missingElectricalData++;

                if (IsTubeLikeCategory(snap.Category) && string.IsNullOrWhiteSpace(snap.SizeText))
                    missingSizeText++;

                var row = _mapper.Map(snap, boqCategory, unit);
                if (row == null) continue;

                rows.Add(row);

                if (rows.Count >= budget.MaxDetailRows && returnDetail)
                {
                    warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");
                    break;
                }
            }

            var resumen = _aggregation.Aggregate(rows);

            if (missingCustomPartida > 0)
                warnings.Add($"No se encontró 'TR3Z - Partida' en {missingCustomPartida} elementos. La corrida continuó sin ese dato.");

            if (missingPanelName > 0)
                warnings.Add($"No se encontró 'Nombre del Panel' en {missingPanelName} elementos. La corrida continuó sin ese dato.");

            if (missingElectricalData > 0)
                warnings.Add($"No se encontró 'Datos eléctricos' en {missingElectricalData} elementos. La corrida continuó sin ese dato.");

            if (missingSizeText > 0)
                warnings.Add($"No se encontró 'Tamaño/Size' en {missingSizeText} tubos. La corrida continuó sin ese dato.");

            warnings.Add("R4 Detalle Pro está temporalmente en SAFE MODE: se prioriza estabilidad. El detalle extendido de propiedades eléctricas avanzadas fue desactivado para evitar crash de Navisworks.");
            warnings.Add("SAFE MODE actual: se reportan de forma confiable longitud y tamaño de tubos, además del desglose base. Las propiedades avanzadas se reactivarán por lectura diferida en la siguiente iteración.");

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_4",
                ScopeMode = options.ScopeMode ?? "all",
                OutputMode = options.OutputMode ?? "summary",
                Preflight = preflight,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(preflight),
                Data = new
                {
                    rutina = "Preconstruccion 4 - Electrica",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = rows.Count,
                    total_tipos = resumen.Count,
                    politica_ejecucion = new
                    {
                        modo = execDecision.Mode,
                        razon = execDecision.Reason,
                        acciones = execDecision.SuggestedActions
                    },
                    diagnostico = new
                    {
                        candidatos_validos = candidatesValidos,
                        modo = "corrida_4_fase1"
                    },
                    resumen,
                    detalle = returnDetail ? rows : null,
                    nota = resumen.Count == 0
                        ? "No se encontraron elementos eléctricos válidos para la corrida."
                        : $"OK — {rows.Count} elementos eléctricos procesados"
                }
            };
        }

        private static string BuildUserScopeMessage(ScopePreflight preflight)
        {
            if (preflight == null) return "";

            if (!string.IsNullOrWhiteSpace(preflight.Message))
                return preflight.Message;

            return "Corrida ejecutada correctamente.";
        }

        private static bool IsTubeLikeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;

            return string.Equals(category, "Conduits", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Conduit", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Tubos", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Tubo", StringComparison.OrdinalIgnoreCase);
        }
    }
}
