using System;
using System.Collections.Generic;
using System.Linq;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class Preconstruccion2Service : IPreconstruccion2Service
    {
        private static readonly string[] AllowedCategories =
        {
            "Structural Columns", "Columnas estructurales", "Pilares estructurales",
            "Structural Framing", "Armazón estructural", "Marcos estructurales", "Vigas estructurales",
            "Structural Foundations", "Cimentaciones estructurales", "Cimentaciones",
            "Walls", "Muros",
            "Floors", "Suelos", "Losas",
            "Roofs", "Cubiertas"
        };

        private readonly ISelectionScopeService _scopeService;
        private readonly IQuantityExtractionService _quantityExtractionService;
        private readonly IQuantityMapperService _quantityMapperService;
        private readonly IBoqAggregationService _aggregationService;
        private readonly IExecutionModePolicyService _executionPolicy;

        public Preconstruccion2Service(
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
            // Para corrida 2, si el scope no es selección manual,
            // evaluamos política ANTES del preflight pesado.
            if (scope == "selection_set" || scope == "all" || scope == "level")
            {
                var earlyDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_2", options, null);

                if (!earlyDecision.AllowAutoRun)
                {
                    return new ToolEnvelope<object>
                    {
                        Ok = false,
                        Tool = "run_preconstruccion_2",
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

            var budget = BudgetProfiles.Corrida2;
            var preflight = _scopeService.BuildPreflight(options, budget);

            var execDecision = _executionPolicy.EvaluateForRun("run_preconstruccion_2", options, preflight);

            if (!execDecision.AllowAutoRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_2",
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
            int descartadosSinGeometria = 0;
            int descartadosMapeoDebil = 0;
            int candidatosValidos = 0;

            var snapshots = _quantityExtractionService.ExtractSnapshotsByCategories(options, AllowedCategories);

            foreach (var snap in snapshots)
            {
                if (snap == null) continue;

                bool hasMeasures =
                    snap.AreaM2 > 0 ||
                    snap.VolumeM3 > 0 ||
                    snap.LengthM > 0;

                if (!hasMeasures)
                {
                    descartadosSinGeometria++;
                    continue;
                }

                var row = _quantityMapperService.ToBoqRow(snap);
                if (row == null) continue;

                bool unidadEstructural =
                    string.Equals(row.Unidad, "ml", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(row.Unidad, "m2", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(row.Unidad, "m3", StringComparison.OrdinalIgnoreCase);

                bool weakPieceFallback =
                    string.Equals(row.Unidad, "pza", StringComparison.OrdinalIgnoreCase) &&
                    (
                        snap.Category.IndexOf("Structural", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Floor", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Roof", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Muro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Losa", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        snap.Category.IndexOf("Cubierta", StringComparison.OrdinalIgnoreCase) >= 0
                    );

                if (weakPieceFallback && !unidadEstructural)
                {
                    descartadosMapeoDebil++;
                    continue;
                }

                candidatosValidos++;
                boqRows.Add(row);

                if (boqRows.Count >= budget.MaxDetailRows && returnDetail)
                {
                    warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");
                    break;
                }
            }

            var resumen = _aggregationService.AggregateBoqRows(boqRows);

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_2",
                ScopeMode = options.ScopeMode ?? "all",
                OutputMode = options.OutputMode ?? "summary",
                Preflight = preflight,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(preflight),
                Data = new
                {
                    rutina = "Preconstruccion 2 - Estructura",
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
                        candidatos_validos = candidatosValidos,
                        descartados_sin_geometria = descartadosSinGeometria,
                        descartados_mapeo_debil = descartadosMapeoDebil,
                        modo = "nueva_arquitectura_fase1"
                    },
                    resumen,
                    detalle = returnDetail ? boqRows : null,
                    nota = resumen.Count == 0
                        ? "No se encontraron elementos válidos para la corrida estructural."
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
