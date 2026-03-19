// CommandDispatcher.cs — v4 (actualizado para BoqTools v7)
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NavisBOQ.Plugin
{
    public static class CommandDispatcher
    {
        public static string Dispatch(string jsonBody)
        {
            try
            {
                var node   = JObject.Parse(string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody);
                var tool   = node["tool"]?.Value<string>() ?? node["command"]?.Value<string>() ?? "";
                var p      = (node["params"] as JObject) ?? new JObject();

                object result;
                switch (tool)
                {
                    case "list_source_files":
                        result = BoqTools.ListSourceFiles();
                        break;
                    case "ping":
                        result = BoqTools.Ping();
                        break;
                    case "get_model_summary":
                        result = BoqTools.GetModelSummary();
                        break;
                    case "get_categories":
                        result = BoqTools.GetModelSummary(); // resumen incluye categorías
                        break;
                    case "dump_properties":
                        result = BoqTools.DumpProperties(GetInt(p, "max_items", 3));
                        break;
                    case "dump_geo_set":
                        result = BoqTools.DumpGeoSet(GetStr(p, "set_name"), GetInt(p, "max_items", 3));
                        break;
                    case "dump_instancia":
                        result = BoqTools.DumpInstancia(GetStr(p, "categoria", "Walls"), GetInt(p, "max_items", 3));
                        break;
                    case "get_parameters":
                        result = BoqTools.GetParametersForCategory(GetStr(p, "category"));
                        break;
                    case "extract_quantities":
                        result = BoqTools.ExtractQuantities(
                                     GetStrList(p, "categories"),
                                     GetStr(p, "level"),
                                     GetBool(p, "summary_only", true));
                        break;
                    case "highlight_elements":
                        result = BoqTools.HighlightByCategory(
                                     GetStrList(p, "categories"),
                                     GetStr(p, "level"));
                        break;
                    case "clear_selection":
                        result = BoqTools.ClearSelection();
                        break;
                    case "list_selection_sets":
                        result = BoqTools.ListSelectionSets();
                        break;
                    case "extract_from_set":
                        result = BoqTools.ExtractFromSelectionSet(GetStr(p, "set_name"));
                        break;
                    case "extract_from_current_selection":
                        result = BoqTools.ExtractFromCurrentSelection();
                        break;
                    case "run_preconstruccion_1":
                        result = BoqTools.RunPreConstruccion1();
                        break;
                    case "run_preconstruccion_2":
                        result = BoqTools.RunPreConstruccion2();
                        break;
                    case "export_json":
                        result = BoqTools.ExportJson(GetStr(p, "output_path"));
                        break;
                    default:
                        throw new Exception("Herramienta desconocida: '" + tool + "'. Usa 'ping' para verificar.");
                }

                return JsonConvert.SerializeObject(new { ok = true, data = result }, Newtonsoft.Json.Formatting.Indented);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { ok = false, error = ex.Message });
            }
        }

        private static int GetInt(JObject p, string key, int def = 0)
        {
            var v = p[key];
            return v != null ? v.Value<int>() : def;
        }

        private static string GetStr(JObject p, string key, string def = "")
        {
            var v = p[key];
            return v != null ? v.Value<string>() ?? def : def;
        }

        private static bool GetBool(JObject p, string key, bool def = false)
        {
            var v = p[key];
            return v != null ? v.Value<bool>() : def;
        }

        private static List<string> GetStrList(JObject p, string key)
        {
            var list = new List<string>();
            var v = p[key] as JArray;
            if (v == null) return list;
            foreach (var item in v)
            {
                var s = item.Value<string>();
                if (s != null) list.Add(s);
            }
            return list;
        }
    }
}
