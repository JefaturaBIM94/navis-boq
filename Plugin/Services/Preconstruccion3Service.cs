using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class Preconstruccion3Service : IPreconstruccion3Service
    {
        private readonly ISelectionScopeService _scopeService;
        private readonly IQuantityExtractionService _quantityExtractionService;
        private readonly ISteelWeightService _steelWeightService;
        private readonly IBoqAggregationService _aggregationService;
        private readonly IExecutionModePolicyService _executionPolicy;
        private readonly ISelectionSetValidationService _selectionSetValidationService;


        public Preconstruccion3Service(
            ISelectionScopeService scopeService,
            IQuantityExtractionService quantityExtractionService,
            ISteelWeightService steelWeightService,
            IBoqAggregationService aggregationService,
            IExecutionModePolicyService executionPolicy,
            ISelectionSetValidationService selectionSetValidationService)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _quantityExtractionService = quantityExtractionService ?? throw new ArgumentNullException(nameof(quantityExtractionService));
            _steelWeightService = steelWeightService ?? throw new ArgumentNullException(nameof(steelWeightService));
            _aggregationService = aggregationService ?? throw new ArgumentNullException(nameof(aggregationService));
            _executionPolicy = executionPolicy ?? throw new ArgumentNullException(nameof(executionPolicy));
            _selectionSetValidationService = selectionSetValidationService ?? throw new ArgumentNullException(nameof(selectionSetValidationService));
        }

        public ToolEnvelope<object> Run(RunOptions options)
        {
            options = options ?? new RunOptions();

            string scope = (options.ScopeMode ?? "all").Trim().ToLowerInvariant();

            if (scope == "selection_set")
            {
                var validation = _selectionSetValidationService.ValidateForSteel(options);

                if (!validation.ContainsSteelCandidates)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_3",
                        ScopeMode = options.ScopeMode ?? "all",
                        OutputMode = "summary",
                        UserMessage = validation.Message,
                        Warnings = new List<string>
                        {
                            "El set no contiene acero estructural reconocido o está compuesto por categorías no válidas para Corrida 3."
                        },
                        Data = new
                        {
                            validacion_set = new
                            {
                                total_visitados = validation.TotalVisited,
                                nodos_elemento = validation.ElementNodes,
                                nodos_geometricos = validation.GeometricNodes,
                                contiene_acero = validation.ContainsSteelCandidates,
                                contiene_genericos = validation.ContainsGenericOrAnnotations,
                                top_categorias = validation.TopCategories,
                                modo_recomendado = validation.RecommendedMode
                            },
                            sugerencia = "Puedo enlistarte las categorías disponibles del conjunto para que elijas una y correr por categoría, o puedes seleccionar manualmente el acero real."
                        }
                    };
                }
            }

            // HOTFIX:
            // Para corrida 3, política ligera ANTES del preflight pesado.
            if (scope == "selection_set" || scope == "all" || scope == "level")
            {
                var earlyDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_3", options, null);

                if (!earlyDecision.AllowAutoRun)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_3",
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

            var budget = BudgetProfiles.Corrida3;
            var preflight = _scopeService.BuildPreflight(options, budget);

            var execDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_3", options, preflight);

            if (!execDecision.AllowAutoRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_3",
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

            var steelRows = new List<SteelRow>();

            int excludedConcrete = 0;
            int metodo2025 = 0;
            int metodoFallback = 0;
            int sinDatos = 0;

            var snapshots = _quantityExtractionService.ExtractSnapshots(options);

            foreach (var snap in snapshots)
            {
                if (snap == null) continue;

                if (!_steelWeightService.IsSteelCandidate(snap))
                {
                    var mat = (snap.TypeMaterial ?? snap.Material ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(mat) &&
                        (mat.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         mat.IndexOf("concreto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         mat.IndexOf("hormigon", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        excludedConcrete++;
                    }

                    continue;
                }

                var row = _steelWeightService.BuildSteelRow(snap);
                if (row == null) continue;

                steelRows.Add(row);

                if (string.Equals(row.Metodo, "2025+", StringComparison.OrdinalIgnoreCase))
                {
                    metodo2025++;
                }
                else if (
                    string.Equals(row.Metodo, "Vol×ρ", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(row.Metodo, "CustomWeight", StringComparison.OrdinalIgnoreCase))
                {
                    metodoFallback++;
                }
                else
                {
                    sinDatos++;
                }

                if (steelRows.Count >= budget.MaxDetailRows && returnDetail)
                {
                    warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");
                    break;
                }
            }

            if (metodo2025 == 0 && metodoFallback > 0)
            {
                warnings.Add("Toda la corrida fue resuelta por fallback (Vol×ρ o peso custom). Revisa lectura de parámetros de tipo y custom properties.");
            }

            var resumen = _aggregationService.AggregateSteelRows(steelRows);
            var pesoTotalKg = Math.Round(resumen.Sum(r => r.PesoKg), 2);

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_3",
                ScopeMode = options.ScopeMode ?? "all",
                OutputMode = options.OutputMode ?? "summary",
                Preflight = preflight,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(preflight),
                Data = new
                {
                    rutina = "Preconstruccion 3 - Estructura Metalica",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = resumen.Sum(r => r.NumPiezas),
                    total_tipos = resumen.Count,
                    peso_total_kg = pesoTotalKg,
                    peso_total_ton = pesoTotalKg >= 1000
                        ? (double?)Math.Round(pesoTotalKg / 1000.0, 3)
                        : null,
                    politica_ejecucion = new
                    {
                        modo = execDecision.Mode,
                        razon = execDecision.Reason,
                        acciones = execDecision.SuggestedActions
                    },
                    diagnostico = new
                    {
                        metodo_2025 = metodo2025,
                        metodo_fallback = metodoFallback,
                        sin_datos = sinDatos,
                        excluidos_concreto = excludedConcrete,
                        modo = "nueva_arquitectura"
                    },
                    resumen,
                    detalle = returnDetail ? steelRows : null,
                    nota = resumen.Count == 0
                        ? "0 elementos de acero encontrados. Verifica material, category alias, DirectShapes IFC y segmentación."
                        : $"OK — {resumen.Sum(r => r.NumPiezas)} piezas | {pesoTotalKg} kg total"
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
    }
}