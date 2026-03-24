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
                var node = JObject.Parse(string.IsNullOrWhiteSpace(jsonBody) ? "{}" : jsonBody);
                var tool = node["tool"]?.Value<string>() ?? node["command"]?.Value<string>() ?? "";
                var p = (node["params"] as JObject) ?? new JObject();

                object result = tool switch
                {
                    "ping" => BoqTools.Ping(),
                    "list_source_files" => BoqTools.ListSourceFiles(),
                    "get_model_summary" => BoqTools.GetModelSummary(ParseRunOptions(p)),
                    "list_selection_sets" => BoqTools.ListSelectionSets(),

                    "extract_from_set" => BoqTools.ExtractFromSelectionSet(
                        GetStr(p, "set_name"),
                        ParseRunOptions(p, forceSelectionSetName: GetStr(p, "set_name"))
                    ),

                    "extract_from_current_selection" => BoqTools.ExtractFromCurrentSelection(ParseRunOptions(p)),

                    "extract_quantities" => BoqTools.ExtractQuantities(
                        GetStrList(p, "categories"),
                        ParseRunOptions(p)
                    ),

                    "run_preconstruccion_1" => BoqTools.RunPreConstruccion1(ParseRunOptions(p)),
                    "run_preconstruccion_2" => BoqTools.RunPreConstruccion2(ParseRunOptions(p)),
                    "run_preconstruccion_2_manual" => BoqTools.RunPreConstruccion2Manual(ParseRunOptions(p)),
                    "run_preconstruccion_3" => BoqTools.RunPreConstruccion3(ParseRunOptions(p)),
                    "run_preconstruccion_3_manual" => BoqTools.RunPreConstruccion3Manual(ParseRunOptions(p)),

                    "run_preconstruccion_3_probe" => BoqTools.RunPreConstruccion3Probe(
                        ParseRunOptions(p),
                        GetInt(p, "max_probe", 10)
                    ),

                    "highlight_elements" => BoqTools.HighlightByCategory(
                        GetStrList(p, "categories"),
                        GetStr(p, "level")
                    ),

                    "clear_selection" => BoqTools.ClearSelection(),

                    "dump_properties" => BoqTools.DumpProperties(GetInt(p, "max_items", 3)),
                    "dump_geo_set" => BoqTools.DumpGeoSet(GetStr(p, "set_name"), GetInt(p, "max_items", 3)),
                    "dump_instancia" => BoqTools.DumpInstancia(GetStr(p, "categoria", "Walls"), GetInt(p, "max_items", 3)),
                    "get_parameters" => BoqTools.GetParametersForCategory(GetStr(p, "category")),
                    "export_json" => BoqTools.ExportJson(GetStr(p, "output_path")),

                    _ => throw new Exception("Herramienta desconocida: '" + tool + "'.")
                };

                return JsonConvert.SerializeObject(new
                {
                    ok = true,
                    data = result
                }, Formatting.None);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new
                {
                    ok = false,
                    error = ex.Message
                }, Formatting.None);
            }
        }

        private static RunOptions ParseRunOptions(JObject p, string forceSelectionSetName = "")
        {
            var opt = new RunOptions
            {
                ScopeMode = GetStr(p, "scope_mode", "all"),
                SelectionSet = !string.IsNullOrWhiteSpace(forceSelectionSetName)
                    ? forceSelectionSetName
                    : GetStr(p, "selection_set", ""),
                Level = GetStr(p, "level", ""),
                OutputMode = GetStr(p, "output_mode", "auto"),
                MaxItems = GetInt(p, "max_items", 12000),
                MaxNodes = GetInt(p, "max_nodes", 50000),
                StrictLimits = GetBool(p, "strict_limits", true)
            };

            if (!string.IsNullOrWhiteSpace(opt.SelectionSet) && opt.ScopeMode == "all")
                opt.ScopeMode = "selection_set";

            if (!string.IsNullOrWhiteSpace(opt.Level) && opt.ScopeMode == "all")
                opt.ScopeMode = "level";

            return opt;
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
                if (!string.IsNullOrWhiteSpace(s))
                    list.Add(s);
            }

            return list;
        }
    }
}