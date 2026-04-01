using System;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class ExecutionModePolicyService : IExecutionModePolicyService
    {
        public ExecutionModeDecision EvaluateForRun(string runName, RunOptions options, ScopePreflight preflight)
        {
            var result = new ExecutionModeDecision();

            string tool = (runName ?? "").Trim().ToLowerInvariant();
            string scope = (options?.ScopeMode ?? "all").Trim().ToLowerInvariant();
            bool strict = options?.StrictLimits ?? true;

            if (scope == "selection")
            {
                result.Mode = "auto_safe";
                result.AllowAutoRun = true;
                result.ForceSummary = false;
                result.Reason = "Selección manual: alcance controlado.";
                return result;
            }

            string riskBand = (preflight?.RiskBand ?? "green").Trim().ToLowerInvariant();
            int candidates = preflight?.CandidateItems ?? 0;

            // ===== CORRIDA 2 =====
            if (tool == "run_preconstruccion_2")
            {
                if (riskBand == "red")
                {
                    result.Mode = strict ? "manual_required" : "auto_summary_only";
                    result.AllowAutoRun = !strict;
                    result.ForceSummary = true;
                    result.Reason = strict
                        ? "Corrida 2: el alcance excede el umbral seguro para autoejecución."
                        : "Corrida 2: alcance rojo; se permite solo resumen por StrictLimits=false.";
                    result.Warnings.Add("La corrida estructural puede incluir nodos pesados o estructura federada compleja.");
                    result.SuggestedActions.Add("Segmenta por nivel, zona o Selection Set.");
                    result.SuggestedActions.Add("Si necesitas detalle fino, usa selección manual.");
                    return result;
                }

                if (riskBand == "yellow" || scope == "selection_set" || scope == "all" || scope == "level")
                {
                    result.Mode = "auto_summary_only";
                    result.AllowAutoRun = true;
                    result.ForceSummary = true;
                    result.Reason = "Corrida 2 permitida en automático, pero limitada a resumen por seguridad.";
                    result.Warnings.Add("Si el resumen sale parcial, usa selección manual.");
                    return result;
                }

                result.Mode = "auto_safe";
                result.AllowAutoRun = true;
                result.ForceSummary = false;
                result.Reason = "Corrida 2 segura para ejecución completa.";
                return result;
            }

            // ===== CORRIDA 3 =====
            if (tool == "run_preconstruccion_3")
            {
                if (riskBand == "red" || candidates > 1500)
                {
                    result.Mode = strict ? "manual_required" : "auto_summary_only";
                    result.AllowAutoRun = !strict;
                    result.ForceSummary = true;
                    result.Reason = strict
                        ? "Corrida 3: alcance demasiado grande para lectura estable de acero."
                        : "Corrida 3: alcance muy grande; se permite solo resumen por StrictLimits=false.";
                    result.Warnings.Add("Si necesitas detalle completo, usa selección manual.");
                    result.SuggestedActions.Add("Segmenta el acero por nivel, bloque o set menor.");
                    return result;
                }

                if (riskBand == "yellow" || scope == "selection_set" || scope == "all" || scope == "level")
                {
                    result.Mode = "auto_summary_only";
                    result.AllowAutoRun = true;
                    result.ForceSummary = true;
                    result.Reason = "Corrida 3 fuera de selección manual: permitir solo resumen por seguridad.";
                    result.Warnings.Add("Si el resultado depende mucho de fallbacks, usa selección manual.");
                    return result;
                }

                result.Mode = "auto_safe";
                result.AllowAutoRun = true;
                result.ForceSummary = false;
                result.Reason = "Corrida 3 segura para ejecución completa.";
                return result;
            }

            // ===== CORRIDA 4 =====
            if (tool == "run_preconstruccion_4")
            {
                if (riskBand == "red")
                {
                    result.Mode = strict ? "manual_required" : "auto_summary_only";
                    result.AllowAutoRun = !strict;
                    result.ForceSummary = true;
                    result.Reason = strict
                        ? "Corrida 4: el alcance eléctrico excede el umbral seguro."
                        : "Corrida 4: alcance rojo; se permite solo resumen por StrictLimits=false.";
                    result.Warnings.Add("El alcance eléctrico puede incluir demasiados conduits, familias genéricas o mezcla disciplinaria.");
                    result.SuggestedActions.Add("Segmenta por nivel.");
                    result.SuggestedActions.Add("Usa Selection Sets eléctricos más pequeños.");
                    result.SuggestedActions.Add("Si necesitas mucho detalle, usa selección manual.");
                    return result;
                }

                if (riskBand == "yellow" || scope == "selection_set" || scope == "all" || scope == "level")
                {
                    result.Mode = "auto_summary_only";
                    result.AllowAutoRun = true;
                    result.ForceSummary = true;
                    result.Reason = "Corrida 4 permitida en automático, pero limitada a resumen por seguridad.";
                    result.Warnings.Add("Si el set mezcla MEP o Generic Models ambiguos, valida el resultado por selección manual.");
                    return result;
                }

                result.Mode = "auto_safe";
                result.AllowAutoRun = true;
                result.ForceSummary = false;
                result.Reason = "Corrida 4 segura para ejecución completa.";
                return result;
            }

            // ===== CORRIDA 1 =====
            if (tool == "run_preconstruccion_1")
            {
                if (riskBand == "red")
                {
                    result.Mode = strict ? "manual_required" : "auto_summary_only";
                    result.AllowAutoRun = !strict;
                    result.ForceSummary = true;
                    result.Reason = strict
                        ? "Corrida 1: el alcance excede el umbral seguro."
                        : "Corrida 1: alcance rojo; se permite solo resumen por StrictLimits=false.";
                    result.Warnings.Add("Si Generic Models sale parcial o ambiguo, usa selección manual.");
                    return result;
                }

                if (riskBand == "yellow" || scope == "selection_set" || scope == "all")
                {
                    result.Mode = "auto_summary_only";
                    result.AllowAutoRun = true;
                    result.ForceSummary = true;
                    result.Reason = "Corrida 1 puede ejecutarse en automático, con salida resumida por seguridad.";
                    result.Warnings.Add("Si necesitas detalle completo, segmenta más el alcance.");
                    return result;
                }

                result.Mode = "auto_safe";
                result.AllowAutoRun = true;
                result.ForceSummary = false;
                result.Reason = "Sin señales de riesgo especial.";
                return result;
            }

            result.Mode = "auto_safe";
            result.AllowAutoRun = true;
            result.ForceSummary = false;
            result.Reason = "Sin señales de riesgo especial.";
            return result;
        }
    }
}
