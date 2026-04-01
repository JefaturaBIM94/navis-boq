using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class Preconstruccion1Service : IPreconstruccion1Service
    {
        private static readonly string[] AllowedCategories =
        {
            "Walls", "Muros",
            "Floors", "Suelos", "Losas",
            "Roofs", "Cubiertas",
            "Ceilings", "Techos", "Plafones",
            "Doors", "Puertas",
            "Windows", "Ventanas",
            "Curtain Wall Panels", "Fachada",
            "Plumbing Fixtures", "Aparatos sanitarios",
            "Generic Models", "Modelos genéricos"
        };

        private readonly ISelectionScopeService _scopeService;
        private readonly IQuantityExtractionService _quantityExtractionService;
        private readonly IQuantityMapperService _quantityMapperService;
        private readonly IBoqAggregationService _aggregationService;
        private readonly IExecutionModePolicyService _executionPolicy;

        public Preconstruccion1Service(
            ISelectionScopeService scopeService,
            IQuantityExtractionService quantityExtractionService,
            IQuantityMapperService quantityMapperService,
            IBoqAggregationService aggregationService,
            IExecutionModePolicyService executionPolicy)
        {
            _scopeService = scopeService ?? throw new ArgumentNullException(nameof(scopeService));
            _quantityExtractionService = quantityExtractionService ?? throw new ArgumentNullException(nameof(quantityExtractionService));
            _quantityMapperService = quantityMapperService ?? throw new ArgumentNullException(nameof(quantityMapperService));
            _aggregationService = aggregationService ?? throw new ArgumentNullException(nameof(aggregationService));
            _executionPolicy = executionPolicy ?? throw new ArgumentNullException(nameof(executionPolicy));
        }

        public ToolEnvelope<object> Run(RunOptions options)
        {
            options = options ?? new RunOptions();

            string scope = (options.ScopeMode ?? "all").Trim().ToLowerInvariant();

            // HOTFIX:
            // Para corrida 1, política ligera ANTES del preflight pesado.
            // Aunque permitimos más casos que en 2 y 3, no debemos entrar a recorrer
            // pesado si ya sabemos que hay que degradar o forzar summary.
            if (scope == "selection_set" || scope == "all" || scope == "level")
            {
                var earlyDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_1", options, null);

                if (!earlyDecision.AllowAutoRun)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_1",
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

            var budget = BudgetProfiles.Corrida1;
            var preflight = _scopeService.BuildPreflight(options, budget);

            var execDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_1", options, preflight);

            if (!execDecision.AllowAutoRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_1",
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

            var boqRows = new List<BoqRow>();
            int partialDataCount = 0;
            int nestedCount = 0;
            int genericModelsCount = 0;
            int genericModelsAmbiguousLevel = 0;

            var snapshots = _quantityExtractionService.ExtractSnapshotsByCategories(options, AllowedCategories);

            foreach (var snap in snapshots)
            {
                if (snap == null) continue;

                if (snap.PartialData)
                    partialDataCount++;

                if (snap.NestedFamilyDetected)
                    nestedCount++;

                if (string.Equals(snap.Category, "Generic Models", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(snap.Category, "Modelos genéricos", StringComparison.OrdinalIgnoreCase))
                {
                    genericModelsCount++;

                    if (string.Equals(snap.Level, "Sin nivel", StringComparison.OrdinalIgnoreCase))
                        genericModelsAmbiguousLevel++;
                }

                var row = _quantityMapperService.ToBoqRow(snap);
                if (row == null) continue;

                boqRows.Add(row);

                if (boqRows.Count >= budget.MaxDetailRows && returnDetail)
                {
                    warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");
                    break;
                }
            }

            if (genericModelsCount > 0)
            {
                warnings.Add("La corrida incluye Generic Models. Si notas cantidades parciales o niveles ambiguos, usa selección manual.");
            }

            if (genericModelsAmbiguousLevel > 0)
            {
                warnings.Add("Se detectaron Generic Models con nivel ambiguo o 'Sin nivel'. Puede tratarse de nested families o subcomponentes.");
            }

            if (nestedCount > 0)
            {
                warnings.Add("Se detectaron elementos potencialmente nested/subcomponent. Revisa host y nivel si el resultado parece parcial.");
            }

            if (partialDataCount > 0)
            {
                warnings.Add("Se detectaron elementos con datos parciales. Considera correr por selección manual para mayor precisión.");
            }

            var resumen = _aggregationService.AggregateBoqRows(boqRows);

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_1",
                ScopeMode = options.ScopeMode ?? "all",
                OutputMode = options.OutputMode ?? "summary",
                Preflight = preflight,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(preflight),
                Data = new
                {
                    rutina = "Preconstruccion 1 - Arquitectura",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = resumen.Sum(r => r.N),
                    total_tipos = resumen.Count,
                    politica_ejecucion = new
                    {
                        modo = execDecision.Mode,
                        razon = execDecision.Reason,
                        acciones = execDecision.SuggestedActions
                    },
                    diagnostico = new
                    {
                        partial_data = partialDataCount,
                        nested_detected = nestedCount,
                        generic_models = genericModelsCount,
                        generic_models_nivel_ambiguo = genericModelsAmbiguousLevel,
                        modo = "nueva_arquitectura"
                    },
                    resumen,
                    detalle = returnDetail ? boqRows : null,
                    nota = resumen.Count == 0
                        ? "No se encontraron elementos válidos para la corrida arquitectónica."
                        : $"OK — {resumen.Sum(r => r.N)} elementos procesados"
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