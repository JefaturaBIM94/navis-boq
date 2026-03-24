using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin
{
    #region DTOs

    public class RunOptions
    {
        public string ScopeMode { get; set; } = "all"; // all | selection | selection_set | level
        public string SelectionSet { get; set; } = "";
        public string Level { get; set; } = "";
        public string OutputMode { get; set; } = "auto"; // auto | summary | detail
        public int MaxItems { get; set; } = 12000;
        public int MaxNodes { get; set; } = 50000;
        public bool StrictLimits { get; set; } = true;
    }

    public class ExecutionBudget
    {
        public int GreenCandidateLimit { get; set; }
        public int YellowCandidateLimit { get; set; }
        public int MaxNodesToVisit { get; set; }
        public int MaxDetailRows { get; set; }
        public int TimeoutMs { get; set; }
    }

    public static class BudgetProfiles
    {
        public static ExecutionBudget Corrida1 => new ExecutionBudget
        {
            GreenCandidateLimit = 10000,
            YellowCandidateLimit = 25000,
            MaxNodesToVisit = 50000,
            MaxDetailRows = 6000,
            TimeoutMs = 90000
        };

        public static ExecutionBudget Corrida2 => new ExecutionBudget
        {
            GreenCandidateLimit = 10000,
            YellowCandidateLimit = 25000,
            MaxNodesToVisit = 50000,
            MaxDetailRows = 6000,
            TimeoutMs = 90000
        };

        public static ExecutionBudget Corrida3 => new ExecutionBudget
        {
            GreenCandidateLimit = 5000,
            YellowCandidateLimit = 12000,
            MaxNodesToVisit = 50000,
            MaxDetailRows = 5000,
            TimeoutMs = 120000
        };
    }

    public class ScopePreflight
    {
        public string ScopeResolved { get; set; } = "";
        public int VisitedNodes { get; set; }
        public int CandidateItems { get; set; }
        public int GeometricItems { get; set; }
        public int DistinctLevels { get; set; }
        public int DistinctCategories { get; set; }
        public string RiskBand { get; set; } = "green";
        public bool AllowRun { get; set; }
        public bool ForceSummary { get; set; }
        public string Message { get; set; } = "";
        public List<string> SuggestedSegmentation { get; set; } = new List<string>();
    }

    public class ToolEnvelope<T>
    {
        public bool Ok { get; set; }
        public string Tool { get; set; } = "";
        public string ScopeMode { get; set; } = "";
        public string OutputMode { get; set; } = "";
        public ScopePreflight Preflight { get; set; }
        public T Data { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public string UserMessage { get; set; } = "";
    }

    public class ElementSnapshot
    {
        public string CanonicalId { get; set; } = "";
        public string ElementId { get; set; } = "";
        public string Level { get; set; } = "Sin nivel";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string Material { get; set; } = "";
        public string Mark { get; set; } = "";

        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double VolumeM3 { get; set; }

        public string TypeDesc { get; set; } = "";
        public string TypeMaterial { get; set; } = "";
        public double TypeWidth { get; set; }
        public double TypeThickness { get; set; }

        public double NominalWeightKgm { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";

        public string SourceSystem { get; set; } = "Revit";
    }

    public class BoqRow
    {
        public string Nivel { get; set; } = "Sin nivel";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string TipoDesc { get; set; } = "";
        public string TipoMaterial { get; set; } = "";
        public double TipoAncho { get; set; }
        public double TipoEspesor { get; set; }
        public double Area { get; set; }
        public double Volumen { get; set; }
        public double Longitud { get; set; }
        public double Cantidad { get; set; }
        public string Unidad { get; set; } = "pza";
        public string ElemId { get; set; } = "";
        public string UbicacionEstructural { get; set; } = "";
    }

    public class BoqSummaryRow
    {
        public string Nivel { get; set; } = "";
        public string Cat { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string TipoDesc { get; set; } = "";
        public string TipoMaterial { get; set; } = "";
        public double TipoAncho { get; set; }
        public double TipoEspesor { get; set; }
        public double Area { get; set; }
        public double Vol { get; set; }
        public double Long_ { get; set; }
        public double Cantidad { get; set; }
        public string Unidad { get; set; } = "";
        public int N { get; set; }
        public string UbicacionEstructural { get; set; } = "";
    }

    public class AggregateBucket
    {
        public string Level { get; set; } = "";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string Unit { get; set; } = "";

        public string TypeDesc { get; set; } = "";
        public string TypeMaterial { get; set; } = "";
        public double TypeWidth { get; set; }
        public double TypeThickness { get; set; }

        public string StructuralLocation { get; set; } = "";

        public int Count { get; set; }
        public double LengthTotal { get; set; }
        public double AreaTotal { get; set; }
        public double VolumeTotal { get; set; }
        public double QuantityTotal { get; set; }
    }

    public class SteelRow
    {
        public string Nivel { get; set; } = "Sin nivel";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public double NominalWeight { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string MaterialEst { get; set; } = "";
        public double Length { get; set; }
        public double Volume { get; set; }
        public double PesoKg { get; set; }
        public string Metodo { get; set; } = "N/D";
        public string ElemId { get; set; } = "";
        public string Mark { get; set; } = "";
    }

    public class SteelSummaryRow
    {
        public string Nivel { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public double NominalWeight { get; set; }
        public int NumPiezas { get; set; }
        public double LengthTotal { get; set; }
        public double VolumeTotal { get; set; }
        public double PesoKg { get; set; }
        public double? PesoTonRef { get; set; }
        public string Metodo { get; set; } = "";
        public string Advertencia { get; set; } = "";
    }

    public class SteelAggregateBucket
    {
        public string Level { get; set; } = "";
        public string Category { get; set; } = "";
        public string Family { get; set; } = "";
        public string Type { get; set; } = "";
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public double NominalWeightKgm { get; set; }

        public int NumPieces { get; set; }
        public double LengthTotalM { get; set; }
        public double VolumeTotalM3 { get; set; }
        public double PesoTotalKg { get; set; }

        public int Metodo2025Count { get; set; }
        public int MetodoFallbackCount { get; set; }
        public int MetodoNDCount { get; set; }
    }

    public class SteelTypeCache
    {
        public string TypeKey { get; set; } = "";
        public double NominalWeightKgm { get; set; }
        public string SectionName { get; set; } = "";
        public string SectionShape { get; set; } = "";
        public string CodeName { get; set; } = "";
        public string Material { get; set; } = "";
    }

    #endregion

    public static class CM
    {
        public static readonly Dictionary<string, (string N, string U)> Mapa =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            {"Muros",("Muros","m2")}, {"Walls",("Muros","m2")},
            {"Suelos",("Losas","m2")}, {"Floors",("Losas","m2")},
            {"Techos",("Plafones","m2")}, {"Ceilings",("Plafones","m2")},
            {"Cubiertas",("Cubiertas","m2")}, {"Roofs",("Cubiertas","m2")},
            {"Puertas",("Puertas","pza")}, {"Doors",("Puertas","pza")},
            {"Ventanas",("Ventanas","pza")}, {"Windows",("Ventanas","pza")},
            {"Escaleras",("Escaleras","pza")}, {"Stairs",("Escaleras","pza")},
            {"Barandillas",("Barandales","ml")}, {"Railings",("Barandales","ml")},
            {"Structural Framing",("Vigas","ml")},
            {"Structural Columns",("Columnas","ml")},
            {"Columnas estructurales",("Columnas","ml")},
            {"Vigas estructurales",("Vigas","ml")},
            {"Structural Foundations",("Cimentacion","m3")},
            {"Cimentaciones",("Cimentacion","m3")},
            {"Curtain Wall Panels",("Fachada","m2")}, {"Curtain Panels",("Fachada","m2")},
            {"Curtain Wall Mullions",("Montantes","ml")},
            {"Ducts",("Ductos","ml")}, {"Duct Fittings",("Conex Ducto","pza")},
            {"Pipes",("Tuberias","ml")}, {"Pipe Fittings",("Conex Tubo","pza")},
            {"Plumbing Fixtures",("Sanitarios","pza")}, {"Aparatos sanitarios",("Sanitarios","pza")},
            {"Mechanical Equipment",("Eq Mecanico","pza")}, {"Air Terminals",("Difusores","pza")},
            {"Electrical Equipment",("Tableros","pza")}, {"Lighting Fixtures",("Luminarias","pza")},
            {"Cable Trays",("Charolas","ml")}, {"Conduits",("Conduits","ml")},
            {"Generic Models",("Generico","pza")}, {"Modelos genéricos",("Generico","pza")},
            {"Specialty Equipment",("Eq Especial","pza")}, {"Furniture",("Mobiliario","pza")},
            {"Mobiliario",("Mobiliario","pza")}, {"Casework",("Carpinteria","pza")},
        };

        public static readonly HashSet<string> EsPza = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Doors","Windows","Mechanical Equipment","Air Terminals","Plumbing Fixtures",
            "Electrical Equipment","Lighting Fixtures","Stairs","Generic Models",
            "Duct Fittings","Pipe Fittings","Railings","Specialty Equipment",
            "Furniture","Casework","Puertas","Ventanas","Escaleras","Mobiliario","Carpinteria",
            "Aparatos sanitarios","Eq Mecanico","Difusores","Sanitarios","Luminarias",
        };
    }

    public static class BoqTools
    {
        private const string CAT_REVIT_ELEMENT = "LcRevitData_Element";
        private const string PROP_AREA = "lcldrevit_parameter_-1012805";
        private const string PROP_VOL = "lcldrevit_parameter_-1012806";
        private const string PROP_LEN_DEFAULT = "lcldrevit_parameter_-1004005";
        private const string PROP_LEN_STRUCT = "lcldrevit_parameter_-1001375";

        private const string PROP_TYPE_DESC = "lcldrevit_parameter_-1010103";
        private const string PROP_STRUCT_MAT = "lcldrevit_parameter_-1005500";
        private const string PROP_TYPE_WIDTH = "lcldrevit_parameter_-1001000";
        private static readonly string[] PROP_TYPE_THICKNESS = {
            "lcldrevit_parameter_-1001902",
            "lcldrevit_parameter_-1002206",
            "lcldrevit_parameter_-1001600",
            "lcldrevit_parameter_-1001006"
        };

        private const string PROP_NOMINAL_WEIGHT = "lcldrevit_parameter_-1005512";
        private const string PROP_SECTION_NAME = "lcldrevit_parameter_-1005554";
        private const string PROP_SECTION_SHAPE = "lcldrevit_parameter_-1005501";
        private const string PROP_CODE_NAME = "lcldrevit_parameter_-1005556";
        private const string PROP_MARK = "lcldrevit_parameter_-1001203";

        static Document Doc => Application.ActiveDocument;

        private static readonly Dictionary<string, SteelTypeCache> _steelTypeCache =
            new Dictionary<string, SteelTypeCache>(StringComparer.OrdinalIgnoreCase);

        static readonly System.Text.RegularExpressions.Regex _reNumUnit =
            new System.Text.RegularExpressions.Regex(
                @"(?<![A-Za-z0-9_])(?<num>[-+]?\d[\d\s]*(?:[.,]\d+)?(?:[eE][-+]?\d+)?)\s*(?<unit>%|°[CF]?|mm|cm|m|km|in|ft|yd|kg|g|lb|N|kN|Pa|MPa|psi|L|s|ms|rad|deg|°|m²|m³|mm²|mm³|ft²|ft³|m2|m3|mm2|mm3)(?:\b|$)",
                System.Text.RegularExpressions.RegexOptions.Compiled |
                System.Text.RegularExpressions.RegexOptions.CultureInvariant |
                System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);

        static readonly System.Text.RegularExpressions.Regex _reFirstNum =
            new System.Text.RegularExpressions.Regex(
                @"(?<![A-Za-z0-9_])[-+]?\d+(?:[.,]\d+)?(?:[eE][-+]?\d+)?",
                System.Text.RegularExpressions.RegexOptions.Compiled |
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        static void OnUI(Action a)
        {
            Exception ex = null;
            System.Windows.Forms.Control ctrl = null;
            try
            {
                var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                if (h != IntPtr.Zero) ctrl = System.Windows.Forms.Control.FromHandle(h);
            }
            catch { }

            if (ctrl != null && ctrl.InvokeRequired)
            {
                ctrl.Invoke(new System.Windows.Forms.MethodInvoker(() =>
                {
                    try { a(); }
                    catch (Exception e) { ex = e; }
                }));
            }
            else
            {
                try { a(); }
                catch (Exception e) { ex = e; }
            }

            if (ex != null) throw ex;
        }

        public static object Ping() =>
            new { pong = true, version = "NavisBOQ v2", doc = Doc?.Title ?? "Sin doc" };

        public static object ListSourceFiles()
        {
            EnsureDoc();
            return new
            {
                documento = Doc.Title,
                archivos = Doc.Models.Select(m => new
                {
                    nombre = m.RootItem.DisplayName,
                    ruta = m.FileName ?? "n/a",
                    elementos = m.RootItem.DescendantsAndSelf.Count(i => SafeHasGeometry(i)),
                    tipo = Path.GetExtension(m.FileName ?? "").ToUpperInvariant().TrimStart('.')
                }).ToList()
            };
        }

        public static object GetModelSummary(RunOptions opt)
        {
            EnsureDoc();

            var levels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            OnUI(() =>
            {
                int visited = 0;
                foreach (var item in ResolveScopeItems(opt))
                {
                    if (visited++ > opt.MaxNodes) break;
                    if (!SafeHasGeometry(item)) continue;

                    var cat = CatFromTree(item);
                    var lvl = LevelFromTree(item);

                    if (!string.IsNullOrWhiteSpace(cat))
                    {
                        if (!cats.ContainsKey(cat)) cats[cat] = 0;
                        cats[cat]++;
                    }

                    if (!string.IsNullOrWhiteSpace(lvl))
                        levels.Add(lvl);
                }
            });

            return new
            {
                archivo = Doc.Title,
                scope = opt.ScopeMode,
                categorias = cats.OrderByDescending(kv => kv.Value)
                    .Select(kv => new { cat = kv.Key, n = kv.Value }).ToList(),
                niveles = levels.OrderBy(x => x).ToList()
            };
        }

        public static object ListSelectionSets()
        {
            EnsureDoc();
            var sets = new List<object>();
            EnumSets(Doc.SelectionSets.Value, sets);
            return new { sets, total = sets.Count };
        }

        public static object ExtractFromSelectionSet(string setName, RunOptions opt)
        {
            EnsureDoc();
            opt.ScopeMode = "selection_set";
            opt.SelectionSet = setName;
            return ExtractQuantities(new List<string>(), opt);
        }

        public static object ExtractFromCurrentSelection(RunOptions opt)
        {
            EnsureDoc();
            opt.ScopeMode = "selection";
            return ExtractQuantities(new List<string>(), opt);
        }

        public static object ExtractQuantities(List<string> cats, RunOptions opt)
        {
            EnsureDoc();
            var budget = BudgetProfiles.Corrida1;
            var pre = PreflightScope(opt, budget, s => SnapshotMatchesGeneralFilter(s, cats, opt.Level));

            if (!pre.AllowRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "extract_quantities",
                    ScopeMode = opt.ScopeMode,
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (pre.ForceSummary && string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = "summary";

            if (string.Equals(opt.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = pre.ForceSummary ? "summary" : "detail";

            var details = ShouldReturnDetail(opt, budget);
            var detailRows = details ? new List<BoqRow>() : null;
            var buckets = new Dictionary<string, AggregateBucket>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            OnUI(() =>
            {
                int visited = 0;

                foreach (var item in ResolveScopeItems(opt))
                {
                    if (++visited > Math.Min(opt.MaxNodes, budget.MaxNodesToVisit))
                    {
                        warnings.Add("Se alcanzó el límite de nodos visitados.");
                        break;
                    }

                    if (!SafeHasGeometry(item) && !HasElementCategory(item))
                        continue;

                    var snap = TryBuildSnapshot(item);
                    if (snap == null) continue;
                    if (!seen.Add(snap.CanonicalId)) continue;
                    if (!SnapshotMatchesGeneralFilter(snap, cats, opt.Level)) continue;

                    var row = ToBoqRow(snap);
                    var key = $"{row.Nivel}|{row.Categoria}|{row.Familia}|{row.Tipo}|{row.Unidad}";

                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        bucket = new AggregateBucket
                        {
                            Level = row.Nivel,
                            Category = row.Categoria,
                            Family = row.Familia,
                            Type = row.Tipo,
                            Unit = row.Unidad,
                            TypeDesc = row.TipoDesc,
                            TypeMaterial = row.TipoMaterial,
                            TypeWidth = row.TipoAncho,
                            TypeThickness = row.TipoEspesor
                        };
                        buckets[key] = bucket;
                    }

                    bucket.Count++;
                    bucket.LengthTotal += row.Longitud;
                    bucket.AreaTotal += row.Area;
                    bucket.VolumeTotal += row.Volumen;
                    bucket.QuantityTotal += row.Cantidad;

                    if (detailRows != null && detailRows.Count < budget.MaxDetailRows)
                        detailRows.Add(row);
                }
            });

            if (detailRows != null && detailRows.Count >= budget.MaxDetailRows)
            {
                warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");
            }

            var resumen = buckets.Values
                .Select(b => new BoqSummaryRow
                {
                    Nivel = b.Level,
                    Cat = b.Category,
                    Familia = b.Family,
                    Tipo = b.Type,
                    Unidad = b.Unit,
                    N = b.Count,
                    Long_ = Math.Round(b.LengthTotal, 2),
                    Area = Math.Round(b.AreaTotal, 2),
                    Vol = Math.Round(b.VolumeTotal, 2),
                    Cantidad = Math.Round(b.QuantityTotal, 2),
                    TipoDesc = b.TypeDesc,
                    TipoMaterial = b.TypeMaterial,
                    TipoAncho = Math.Round(b.TypeWidth, 4),
                    TipoEspesor = Math.Round(b.TypeThickness, 4)
                })
                .OrderBy(r => r.Cat).ThenBy(r => r.Nivel).ThenBy(r => r.Tipo)
                .ToList();

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "extract_quantities",
                ScopeMode = opt.ScopeMode,
                OutputMode = opt.OutputMode,
                Preflight = pre,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(pre),
                Data = new
                {
                    total = resumen.Sum(r => r.N),
                    resumen,
                    detalle = details ? detailRows : null
                }
            };
        }

        public static object RunPreConstruccion1(RunOptions opt)
        {
            var cats = new List<string> {
                "Muros","Walls","Losas","Floors","Cubiertas","Roofs",
                "Plafones","Ceilings","Puertas","Doors","Ventanas","Windows",
                "Fachada","Curtain Wall Panels"
            };

            return RunCorridaGeneral("run_preconstruccion_1", "Preconstruccion 1 - Arquitectura", cats, opt, BudgetProfiles.Corrida1);
        }

        public static object RunPreConstruccion2(RunOptions opt)
        {
            var cats = new List<string> {
                "Structural Columns","Structural Framing","Structural Foundations",
                "Walls","Muros","Floors","Suelos","Losas","Roofs","Cubiertas"
            };

            return RunCorridaGeneral("run_preconstruccion_2", "Preconstruccion 2 - Estructura", cats, opt, BudgetProfiles.Corrida2);
        }

        public static object RunPreConstruccion2Manual(RunOptions opt)
        {
            EnsureDoc();

            opt.ScopeMode = "selection";

            const int GREEN_LIMIT = 4000;
            const int YELLOW_LIMIT = 10000;
            const int HARD_LIMIT = 15000;
            const int MAX_DETAIL_ROWS = 6000;

            int selectedCount = 0;

            OnUI(() =>
            {
                selectedCount = Application.ActiveDocument.CurrentSelection.SelectedItems.Count;
            });

            var pre = new ScopePreflight
            {
                ScopeResolved = "current_selection",
                VisitedNodes = selectedCount,
                CandidateItems = selectedCount
            };

            if (selectedCount <= 0)
            {
                pre.RiskBand = "green";
                pre.AllowRun = false;
                pre.ForceSummary = false;
                pre.Message = "No hay seleccion activa en Navisworks. Selecciona manualmente los elementos y vuelve a correr la herramienta.";

                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_2_manual",
                    ScopeMode = "selection",
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (selectedCount <= GREEN_LIMIT)
            {
                pre.RiskBand = "green";
                pre.AllowRun = true;
                pre.ForceSummary = false;
                pre.Message = "La seleccion es segura para corrida completa.";
            }
            else if (selectedCount <= YELLOW_LIMIT)
            {
                pre.RiskBand = "yellow";
                pre.AllowRun = true;
                pre.ForceSummary = true;
                pre.Message = "La seleccion es grande; por estabilidad se devolvera solo resumen.";
                pre.SuggestedSegmentation.Add("Segmenta por nivel.");
                pre.SuggestedSegmentation.Add("Segmenta por zona.");
            }
            else
            {
                pre.RiskBand = "red";
                pre.AllowRun = !opt.StrictLimits;
                pre.ForceSummary = true;
                pre.Message = "La seleccion excede el umbral seguro. Segmenta mas el modelo antes de correr la cuantificacion.";
                pre.SuggestedSegmentation.Add("Oculta lo no deseado y selecciona manualmente un subconjunto menor.");
            }

            if (!pre.AllowRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_2_manual",
                    ScopeMode = "selection",
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (string.Equals(opt.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = pre.ForceSummary ? "summary" : "detail";

            if (pre.ForceSummary && string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = "summary";

            bool returnDetail = string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase);

            var detailRows = returnDetail ? new List<BoqRow>() : null;
            var buckets = new Dictionary<string, AggregateBucket>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();

            int visited = 0;
            int candidatos = 0;

            OnUI(() =>
            {
                var seenInst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ModelItem selected in Application.ActiveDocument.CurrentSelection.SelectedItems)
                {
                    if (selected == null) continue;

                    IEnumerable<ModelItem> candidates;

                    try
                    {
                        candidates = selected.DescendantsAndSelf;
                    }
                    catch
                    {
                        candidates = new[] { selected };
                    }

                    foreach (ModelItem item in candidates)
                    {
                        if (item == null) continue;

                        if (++visited > HARD_LIMIT)
                        {
                            warnings.Add("Se alcanzo el limite maximo seguro de elementos visitados en seleccion manual.");
                            return;
                        }

                        bool hasGeo = false;
                        bool hasElemPc = false;

                        try { hasGeo = item.HasGeometry; } catch { }
                        try
                        {
                            hasElemPc = item.PropertyCategories.Any(p =>
                                string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                        }
                        catch { }

                        if (!hasGeo && !hasElemPc)
                            continue;

                        PropertyCategory elemPC = null;
                        ModelItem instNode = null;

                        foreach (var a in item.AncestorsAndSelf)
                        {
                            PropertyCategory pc = null;
                            try { pc = a.PropertyCategories.FindCategoryByDisplayName("Componente"); } catch { }
                            if (pc == null) try { pc = a.PropertyCategories.FindCategoryByDisplayName("Element"); } catch { }
                            if (pc == null)
                            {
                                try
                                {
                                    pc = a.PropertyCategories.FirstOrDefault(p =>
                                        string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                                }
                                catch { }
                            }

                            if (pc == null) continue;

                            var catProp = pc.Properties.FirstOrDefault(p =>
                                string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));

                            if (catProp?.Value == null) continue;

                            var catVal = SafeDisplay(catProp.Value) ?? "";
                            if (string.IsNullOrWhiteSpace(catVal)) continue;

                            instNode = a;
                            elemPC = pc;
                            break;
                        }

                        if (instNode == null || elemPC == null)
                            continue;

                        string instKey = SafeCanonicalId(instNode);
                        if (!seenInst.Add(instKey))
                            continue;

                        string categoria = SafeDisplay(
                            elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementCategory")?.Value
                        ) ?? "";

                        bool categoriaValida =
                            OIC(categoria, "Structural Framing") ||
                            OIC(categoria, "Structural Columns") ||
                            OIC(categoria, "Walls") ||
                            OIC(categoria, "Floors") ||
                            OIC(categoria, "Suelos") ||
                            OIC(categoria, "Losas") ||
                            OIC(categoria, "Structural Foundations");

                        if (!categoriaValida)
                            continue;

                        candidatos++;

                        string elementId = SafeDisplay(
                            elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementId")?.Value
                        ) ?? "";

                        string familia = SafeDisplay(
                            elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementFamily")?.Value
                        ) ?? "";

                        string tipo = SafeDisplay(
                            elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementType")?.Value
                        ) ?? SafeDisplay(
                            elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementName")?.Value
                        ) ?? "";

                        string nivel = "Sin nivel";
                        try { nivel = LevelFromTree(item) ?? "Sin nivel"; } catch { }

                        double area = 0;
                        double volume = 0;
                        double length = 0;
                        try
                        {
                            ReadQuantities(instNode, ref area, ref volume, ref length, categoria);
                        }
                        catch { }

                        string materialInst = "";
                        try
                        {
                            var pMat = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", "lcldrevit_parameter_-1005500");
                            if (pMat?.Value != null) materialInst = SafeDisplay(pMat.Value) ?? "";
                        }
                        catch { }

                        string tipoDesc = "";
                        string tipoMat = "";
                        double tipoAncho = 0;
                        double tipoEspesor = 0;

                        try
                        {
                            ReadGenericTypeProps(instNode, out tipoDesc, out tipoMat, out tipoAncho, out tipoEspesor);
                        }
                        catch { }

                        string ubicacionEstructural = "";
                        try
                        {
                            ubicacionEstructural = ReadStructuralLocation(instNode, categoria);
                        }
                        catch { }

                        string boq = categoria;
                        string unit = "";

                        var m = CM.Mapa.FirstOrDefault(x => OIC(categoria, x.Key));
                        if (m.Key != null)
                        {
                            boq = m.Value.N;
                            unit = m.Value.U;
                        }

                        double qty;
                        string u;

                        if (unit == "pza" || CM.EsPza.Contains(categoria))
                        {
                            qty = 1;
                            u = "pza";
                        }
                        else if (unit == "m2" && area > 0)
                        {
                            qty = Math.Round(area, 3);
                            u = "m2";
                        }
                        else if (unit == "m3" && volume > 0)
                        {
                            qty = Math.Round(volume, 3);
                            u = "m3";
                        }
                        else if (unit == "ml" && length > 0)
                        {
                            qty = Math.Round(length, 3);
                            u = "ml";
                        }
                        else if (area > 0)
                        {
                            qty = Math.Round(area, 3);
                            u = "m2";
                        }
                        else if (volume > 0)
                        {
                            qty = Math.Round(volume, 3);
                            u = "m3";
                        }
                        else if (length > 0)
                        {
                            qty = Math.Round(length, 3);
                            u = "ml";
                        }
                        else
                        {
                            qty = 1;
                            u = "pza";
                        }

                        var row = new BoqRow
                        {
                            Nivel = nivel,
                            Categoria = boq,
                            Familia = Clean(familia),
                            Tipo = Clean(tipo),
                            TipoDesc = tipoDesc,
                            TipoMaterial = !string.IsNullOrWhiteSpace(tipoMat) ? tipoMat : materialInst,
                            TipoAncho = Math.Round(tipoAncho, 4),
                            TipoEspesor = Math.Round(tipoEspesor, 4),
                            Area = Math.Round(area, 4),
                            Volumen = Math.Round(volume, 4),
                            Longitud = Math.Round(length, 4),
                            Cantidad = qty,
                            Unidad = u,
                            ElemId = elementId,
                            UbicacionEstructural = ubicacionEstructural
                        };

                        string key = $"{row.Nivel}|{row.Categoria}|{row.Familia}|{row.Tipo}|{row.Unidad}|{row.UbicacionEstructural}";

                        if (!buckets.TryGetValue(key, out var bucket))
                        {
                            bucket = new AggregateBucket
                            {
                                Level = row.Nivel,
                                Category = row.Categoria,
                                Family = row.Familia,
                                Type = row.Tipo,
                                Unit = row.Unidad,
                                TypeDesc = row.TipoDesc,
                                TypeMaterial = row.TipoMaterial,
                                TypeWidth = row.TipoAncho,
                                TypeThickness = row.TipoEspesor,
                                StructuralLocation = row.UbicacionEstructural
                            };
                            buckets[key] = bucket;
                        }

                        bucket.Count++;
                        bucket.LengthTotal += row.Longitud;
                        bucket.AreaTotal += row.Area;
                        bucket.VolumeTotal += row.Volumen;
                        bucket.QuantityTotal += row.Cantidad;

                        if (detailRows != null && detailRows.Count < MAX_DETAIL_ROWS)
                            detailRows.Add(row);
                    }
                }
            });

            if (detailRows != null && detailRows.Count >= MAX_DETAIL_ROWS)
                warnings.Add("Detalle truncado por tamaño de la seleccion. Segmenta mas el alcance si necesitas detalle completo.");

            pre.CandidateItems = candidatos;

            var resumen = buckets.Values
                .Select(b => new BoqSummaryRow
                {
                    Nivel = b.Level,
                    Cat = b.Category,
                    Familia = b.Family,
                    Tipo = b.Type,
                    TipoDesc = b.TypeDesc,
                    TipoMaterial = b.TypeMaterial,
                    TipoAncho = Math.Round(b.TypeWidth, 4),
                    TipoEspesor = Math.Round(b.TypeThickness, 4),
                    Area = Math.Round(b.AreaTotal, 2),
                    Vol = Math.Round(b.VolumeTotal, 2),
                    Long_ = Math.Round(b.LengthTotal, 2),
                    Cantidad = Math.Round(b.QuantityTotal, 2),
                    Unidad = b.Unit,
                    N = b.Count,
                    UbicacionEstructural = b.StructuralLocation
                })
                .OrderBy(r => r.Cat).ThenBy(r => r.Nivel).ThenBy(r => r.Tipo)
                .ToList();

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_2_manual",
                ScopeMode = "selection",
                OutputMode = opt.OutputMode,
                Preflight = pre,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(pre),
                Data = new
                {
                    rutina = "Preconstruccion 2 Manual - Estructura",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = resumen.Sum(r => r.N),
                    total_tipos = resumen.Count,
                    diagnostico = new
                    {
                        elementos_seleccionados = selectedCount,
                        candidatos_validos = candidatos,
                        categorias = new[]
                        {
                            "Structural Framing",
                            "Structural Columns",
                            "Walls",
                            "Floors",
                            "Structural Foundations"
                        },
                        modo = "manual_selection_mvp"
                    },
                    resumen,
                    detalle = returnDetail ? detailRows : null,
                    nota = resumen.Count == 0
                        ? "No se encontraron elementos validos en la seleccion manual."
                        : $"OK - {resumen.Sum(r => r.N)} elementos procesados"
                }
            };
        }

        public static object RunPreConstruccion3(RunOptions opt)
        {
            EnsureDoc();
            var budget = BudgetProfiles.Corrida3;
            var pre = PreflightScope(opt, budget, SnapshotMatchesSteelFilter);

            if (!pre.AllowRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_3",
                    ScopeMode = opt.ScopeMode,
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (string.Equals(opt.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = pre.ForceSummary ? "summary" : "detail";

            if (pre.ForceSummary && string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = "summary";

            bool returnDetail = ShouldReturnDetail(opt, budget);
            var detailRows = returnDetail ? new List<SteelRow>() : null;
            var buckets = new Dictionary<string, SteelAggregateBucket>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            int excConcreto = 0;
            int metodo2025 = 0;
            int metodoFallback = 0;
            int sinDatos = 0;

            // IMPORTANTE:
            // En esta versión de diagnóstico NO se lee:
            // - NominalWeight
            // - SectionName
            // - SectionShape
            // - CodeName
            // - material desde tipo
            //
            // Solo:
            // - categoría
            // - material de instancia
            // - longitud
            // - volumen
            // - peso por fallback Vol × 7850

            OnUI(() =>
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int visited = 0;

                foreach (var item in ResolveScopeItems(opt))
                {
                    if (++visited > Math.Min(opt.MaxNodes, budget.MaxNodesToVisit))
                    {
                        warnings.Add("Se alcanzó el límite de nodos visitados.");
                        break;
                    }

                    if (!SafeHasGeometry(item) && !HasElementCategory(item))
                        continue;

                    ElementSnapshot snap = null;

                    try
                    {
                        snap = TryBuildSnapshot(item);
                    }
                    catch
                    {
                        snap = null;
                    }

                    if (snap == null) continue;
                    if (!seen.Add(snap.CanonicalId)) continue;

                    // FILTRO ESPECÍFICO DE ACERO SOLO CON MATERIAL DE INSTANCIA
                    bool isTargetCategory =
                        OIC(snap.Category, "Structural Framing") ||
                        OIC(snap.Category, "Structural Columns");

                    if (!isTargetCategory) continue;

                    string mat = snap.Material ?? "";

                    var concretoKeywords = new[] { "concrete", "concreto", "hormigon", "masonry" };
                    var aceroKeywords = new[] { "steel", "acero", "metal", "metalic", "metallic", "w shape", "hss", "pipe" };

                    bool esConcreto = !string.IsNullOrWhiteSpace(mat) &&
                                      concretoKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (esConcreto)
                    {
                        excConcreto++;
                        continue;
                    }

                    bool esAcero = string.IsNullOrWhiteSpace(mat) ||
                                   aceroKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!esAcero) continue;

                    // CÁLCULO TEMPORAL SOLO POR VOLUMEN
                    double pesoKg;
                    string metodo;

                    if (snap.VolumeM3 > 0)
                    {
                        pesoKg = snap.VolumeM3 * 7850.0;
                        metodo = "Vol×ρ";
                        metodoFallback++;
                    }
                    else
                    {
                        pesoKg = 0;
                        metodo = "N/D";
                        sinDatos++;
                    }

                    var steelRow = new SteelRow
                    {
                        Nivel = snap.Level,
                        Categoria = snap.Category,
                        Familia = Clean(snap.Family),
                        Tipo = Clean(snap.Type),

                        // DESACTIVADO TEMPORALMENTE
                        NominalWeight = 0,
                        SectionName = "",
                        SectionShape = "",
                        CodeName = "",

                        MaterialEst = snap.Material ?? "",
                        Length = Math.Round(snap.LengthM, 4),
                        Volume = Math.Round(snap.VolumeM3, 4),
                        PesoKg = Math.Round(pesoKg, 2),
                        Metodo = metodo,
                        ElemId = snap.ElementId,
                        Mark = snap.Mark
                    };

                    var key = $"{steelRow.Nivel}|{steelRow.Categoria}|{steelRow.Familia}|{steelRow.Tipo}";

                    if (!buckets.TryGetValue(key, out var b))
                    {
                        b = new SteelAggregateBucket
                        {
                            Level = steelRow.Nivel,
                            Category = steelRow.Categoria,
                            Family = steelRow.Familia,
                            Type = steelRow.Tipo,

                            // DESACTIVADO TEMPORALMENTE
                            SectionName = "",
                            SectionShape = "",
                            CodeName = "",
                            NominalWeightKgm = 0
                        };
                        buckets[key] = b;
                    }

                    b.NumPieces++;
                    b.LengthTotalM += steelRow.Length;
                    b.VolumeTotalM3 += steelRow.Volume;
                    b.PesoTotalKg += steelRow.PesoKg;

                    if (metodo == "2025+") b.Metodo2025Count++;
                    else if (metodo == "Vol×ρ") b.MetodoFallbackCount++;
                    else b.MetodoNDCount++;

                    if (detailRows != null && detailRows.Count < budget.MaxDetailRows)
                        detailRows.Add(steelRow);
                }
            });

            if (detailRows != null && detailRows.Count >= budget.MaxDetailRows)
                warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más con Selection Sets o por nivel.");

            var resumen = buckets.Values
                .OrderBy(x => x.Level).ThenBy(x => x.Category).ThenBy(x => x.Type)
                .Select(b =>
                {
                    var metodo = b.MetodoFallbackCount > 0 && b.Metodo2025Count > 0
                        ? "Mixto"
                        : (b.Metodo2025Count > 0 ? "2025+" : (b.MetodoFallbackCount > 0 ? "Vol×ρ" : "N/D"));

                    var advertencia = b.MetodoFallbackCount > 0
                        ? "Peso calculado temporalmente por Vol × 7850 kg/m3."
                        : (b.MetodoNDCount > 0 ? "Sin datos de peso." : "");

                    return new SteelSummaryRow
                    {
                        Nivel = b.Level,
                        Categoria = b.Category,
                        Familia = b.Family,
                        Tipo = b.Type,

                        // DESACTIVADO TEMPORALMENTE
                        SectionName = "",
                        SectionShape = "",
                        CodeName = "",
                        NominalWeight = 0,

                        NumPiezas = b.NumPieces,
                        LengthTotal = Math.Round(b.LengthTotalM, 3),
                        VolumeTotal = Math.Round(b.VolumeTotalM3, 4),
                        PesoKg = Math.Round(b.PesoTotalKg, 2),
                        PesoTonRef = b.PesoTotalKg >= 1000 ? (double?)Math.Round(b.PesoTotalKg / 1000.0, 3) : null,
                        Metodo = metodo,
                        Advertencia = advertencia
                    };
                }).ToList();

            var pesoTotalKg = Math.Round(resumen.Sum(r => r.PesoKg), 2);

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_3",
                ScopeMode = opt.ScopeMode,
                OutputMode = opt.OutputMode,
                Preflight = pre,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(pre),
                Data = new
                {
                    rutina = "Preconstruccion 3 - Estructura Metalica (modo diagnostico sin lectura de tipo)",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = resumen.Sum(r => r.NumPiezas),
                    total_tipos = resumen.Count,
                    peso_total_kg = pesoTotalKg,
                    peso_total_ton = pesoTotalKg >= 1000 ? (double?)Math.Round(pesoTotalKg / 1000.0, 3) : null,
                    diagnostico = new
                    {
                        metodo_2025 = metodo2025,
                        metodo_fallback = metodoFallback,
                        sin_datos = sinDatos,
                        excluidos_concreto = excConcreto,
                        modo = "diagnostico_sin_lectura_tipo"
                    },
                    resumen,
                    detalle = returnDetail ? detailRows : null,
                    nota = resumen.Count == 0
                        ? "0 elementos de acero encontrados. Verificar material estructural y segmentación."
                        : $"OK — {resumen.Sum(r => r.NumPiezas)} piezas | {pesoTotalKg} kg total"
                }
            };
        }

        public static object RunPreConstruccion3Probe(RunOptions opt, int maxProbe = 10)
        {
            EnsureDoc();

            if (maxProbe <= 0) maxProbe = 5;
            if (maxProbe > 10) maxProbe = 10;

            var rows = new List<object>();
            var warnings = new List<string>();
            int visited = 0;
            int candidatos = 0;
            int framingCount = 0;
            int columnsCount = 0;
            int metodo2025 = 0;
            int metodoFallback = 0;
            int sinDatos = 0;

            OnUI(() =>
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in ResolveScopeItems(opt))
                {
                    visited++;

                    if (!SafeHasGeometry(item) && !HasElementCategory(item))
                        continue;

                    string canonicalId;
                    try { canonicalId = SafeCanonicalId(item); }
                    catch { continue; }

                    if (!seen.Add(canonicalId))
                        continue;

                    // === INSTANCIA: encontrar nodo real con LcRevitData_Element ===
                    PropertyCategory elemPC = null;
                    ModelItem instNode = null;

                    foreach (var a in item.AncestorsAndSelf)
                    {
                        PropertyCategory pc = null;
                        try { pc = a.PropertyCategories.FindCategoryByDisplayName("Componente"); } catch { }
                        if (pc == null) try { pc = a.PropertyCategories.FindCategoryByDisplayName("Element"); } catch { }
                        if (pc == null)
                        {
                            try
                            {
                                pc = a.PropertyCategories.FirstOrDefault(p =>
                                    string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                            }
                            catch { }
                        }

                        if (pc == null) continue;

                        DataProperty catProp = null;
                        try
                        {
                            catProp = pc.Properties.FirstOrDefault(p =>
                                string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));
                        }
                        catch { }

                        if (catProp?.Value == null) continue;

                        var catVal = SafeDisplay(catProp.Value) ?? "";
                        if (string.IsNullOrWhiteSpace(catVal)) continue;

                        instNode = a;
                        elemPC = pc;
                        break;
                    }

                    if (instNode == null || elemPC == null)
                        continue;

                    string categoria = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementCategory")?.Value
                    ) ?? "";

                    bool isFraming = OIC(categoria, "Structural Framing");
                    bool isColumn = OIC(categoria, "Structural Columns");

                    if (!isFraming && !isColumn)
                        continue;

                    candidatos++;

                    if (isFraming) framingCount++;
                    if (isColumn) columnsCount++;

                    string elementId = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementId")?.Value
                    ) ?? "";

                    string familia = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementFamily")?.Value
                    ) ?? "";

                    string tipo = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementType")?.Value
                    ) ?? SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementName")?.Value
                    ) ?? "";

                    string nivel = "Sin nivel";
                    try { nivel = LevelFromTree(item) ?? "Sin nivel"; } catch { }

                    string mark = "";
                    try
                    {
                        var pMark = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", "lcldrevit_parameter_-1001203");
                        if (pMark?.Value != null) mark = SafeDisplay(pMark.Value) ?? "";
                    }
                    catch { }

                    string materialInst = "";
                    try
                    {
                        var pMat = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", "lcldrevit_parameter_-1005500");
                        if (pMat?.Value != null) materialInst = SafeDisplay(pMat.Value) ?? "";
                    }
                    catch { }

                    double length = 0;
                    double volume = 0;
                    try
                    {
                        double areaDummy = 0;
                        ReadQuantities(instNode, ref areaDummy, ref volume, ref length, categoria);
                    }
                    catch { }

                    // === TIPO: leer solo lo indispensable para la spec ===
                    double nominalWeight = 0;
                    string sectionName = "";
                    string sectionShape = "";
                    string codeName = "";
                    bool tipoEncontrado = false;

                    foreach (var a in instNode.AncestorsAndSelf)
                    {
                        try
                        {
                            var typePC = a.PropertyCategories.FirstOrDefault(p =>
                                string.Equals(p.Name, "lcldrevit_tab_type", StringComparison.OrdinalIgnoreCase));
                            if (typePC == null) continue;

                            tipoEncontrado = true;

                            var pNW = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005512");
                            if (pNW?.Value != null) nominalWeight = ExtractDoubleFromVariant(pNW.Value);

                            var pSN = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005554");
                            if (pSN?.Value != null) sectionName = SafeDisplay(pSN.Value) ?? "";

                            var pSS = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005501");
                            if (pSS?.Value != null) sectionShape = SafeDisplay(pSS.Value) ?? "";

                            var pCN = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005556");
                            if (pCN?.Value != null) codeName = SafeDisplay(pCN.Value) ?? "";

                            break;
                        }
                        catch (Exception ex)
                        {
                            warnings.Add("Error leyendo nodo tipo en un elemento: " + ex.Message);
                            break;
                        }
                    }

                    double pesoKg = 0;
                    string metodo = "N/D";

                    if (nominalWeight > 0 && length > 0)
                    {
                        pesoKg = nominalWeight * length;
                        metodo = "2025+";
                        metodo2025++;
                    }
                    else if (volume > 0)
                    {
                        pesoKg = volume * 7850.0;
                        metodo = "Vol×ρ";
                        metodoFallback++;
                    }
                    else
                    {
                        sinDatos++;
                    }

                    rows.Add(new
                    {
                        elemento_id = elementId,
                        categoria = categoria,
                        familia = Clean(familia),
                        tipo = Clean(tipo),
                        nivel = nivel,
                        marca = mark,
                        material_instancia = materialInst,

                        length_m = Math.Round(length, 4),
                        volume_m3 = Math.Round(volume, 4),

                        tipo_encontrado = tipoEncontrado,
                        nominal_weight_kgm = Math.Round(nominalWeight, 4),
                        section_name = sectionName,
                        section_shape = sectionShape,
                        code_name = codeName,

                        peso_kg = Math.Round(pesoKg, 2),
                        metodo = metodo
                    });

                    if (rows.Count >= maxProbe)
                        break;
                }
            });

            return new
            {
                rutina = "run_preconstruccion_3_probe",
                ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                scope = new
                {
                    scope_mode = opt.ScopeMode,
                    selection_set = opt.SelectionSet,
                    level = opt.Level,
                    max_probe = maxProbe
                },
                diagnostico = new
                {
                    visited_nodes = visited,
                    candidatos_steel = candidatos,
                    structural_framing = framingCount,
                    structural_columns = columnsCount,
                    metodo_2025 = metodo2025,
                    metodo_fallback = metodoFallback,
                    sin_datos = sinDatos
                },
                resultados = rows,
                warnings = warnings.Distinct().ToList(),
                nota = rows.Count == 0
                    ? "No se encontraron elementos válidos en la muestra."
                    : "Probe completado. Si esto funciona sin crash, el siguiente paso es escalar gradualmente."
            };
        }

        public static object RunPreConstruccion3Manual(RunOptions opt)
        {
            EnsureDoc();

            // Forzar modo manual por seleccion activa
            opt.ScopeMode = "selection";

            // Presupuestos MVP para seleccion manual
            const int GREEN_LIMIT = 3000;
            const int YELLOW_LIMIT = 8000;
            const int HARD_LIMIT = 12000;
            const int MAX_DETAIL_ROWS = 5000;
            const double DENSIDAD_ACERO = 7850.0;

            int selectedCount = 0;

            OnUI(() =>
            {
                selectedCount = Application.ActiveDocument.CurrentSelection.SelectedItems.Count;
            });

            var pre = new ScopePreflight
            {
                ScopeResolved = "current_selection",
                VisitedNodes = selectedCount,
                CandidateItems = selectedCount
            };

            if (selectedCount <= 0)
            {
                pre.RiskBand = "green";
                pre.AllowRun = false;
                pre.ForceSummary = false;
                pre.Message = "No hay seleccion activa en Navisworks. Selecciona manualmente los elementos y vuelve a correr la herramienta.";

                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_3_manual",
                    ScopeMode = "selection",
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (selectedCount <= GREEN_LIMIT)
            {
                pre.RiskBand = "green";
                pre.AllowRun = true;
                pre.ForceSummary = false;
                pre.Message = "La seleccion es segura para corrida completa.";
            }
            else if (selectedCount <= YELLOW_LIMIT)
            {
                pre.RiskBand = "yellow";
                pre.AllowRun = true;
                pre.ForceSummary = true;
                pre.Message = "La seleccion es grande; por estabilidad se devolvera solo resumen.";
                pre.SuggestedSegmentation.Add("Segmenta por nivel.");
                pre.SuggestedSegmentation.Add("Segmenta por Selection Sets visuales.");
            }
            else
            {
                pre.RiskBand = "red";
                pre.AllowRun = !opt.StrictLimits;
                pre.ForceSummary = true;
                pre.Message = "La seleccion excede el umbral seguro. Segmenta mas el modelo antes de correr la cuantificacion.";
                pre.SuggestedSegmentation.Add("Oculta lo no deseado y selecciona manualmente un subconjunto menor.");
                pre.SuggestedSegmentation.Add("Corre por nivel o por zona.");
            }

            if (!pre.AllowRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = "run_preconstruccion_3_manual",
                    ScopeMode = "selection",
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (string.Equals(opt.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = pre.ForceSummary ? "summary" : "detail";

            if (pre.ForceSummary && string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = "summary";

            bool returnDetail = string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase);

            var detailRows = returnDetail ? new List<SteelRow>() : null;
            var buckets = new Dictionary<string, SteelAggregateBucket>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();

            int visited = 0;
            int candidatos = 0;
            int framingCount = 0;
            int columnsCount = 0;
            int metodo2025 = 0;
            int metodoFallback = 0;
            int sinDatos = 0;
            int excConcreto = 0;

            OnUI(() =>
            {
                var seenInst = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (ModelItem item in Application.ActiveDocument.CurrentSelection.SelectedItems)
                {
                    if (item == null) continue;

                    if (++visited > HARD_LIMIT)
                    {
                        warnings.Add("Se alcanzo el limite maximo seguro de elementos visitados en seleccion manual.");
                        break;
                    }

                    // Encontrar nodo instancia real
                    PropertyCategory elemPC = null;
                    ModelItem instNode = null;

                    foreach (var a in item.AncestorsAndSelf)
                    {
                        PropertyCategory pc = null;
                        try { pc = a.PropertyCategories.FindCategoryByDisplayName("Componente"); } catch { }
                        if (pc == null) try { pc = a.PropertyCategories.FindCategoryByDisplayName("Element"); } catch { }
                        if (pc == null)
                        {
                            try
                            {
                                pc = a.PropertyCategories.FirstOrDefault(p =>
                                    string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                            }
                            catch { }
                        }

                        if (pc == null) continue;

                        var catProp = pc.Properties.FirstOrDefault(p =>
                            string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));

                        if (catProp?.Value == null) continue;

                        var catVal = SafeDisplay(catProp.Value) ?? "";
                        if (string.IsNullOrWhiteSpace(catVal)) continue;

                        instNode = a;
                        elemPC = pc;
                        break;
                    }

                    if (instNode == null || elemPC == null)
                        continue;

                    string instKey = SafeCanonicalId(instNode);
                    if (!seenInst.Add(instKey))
                        continue;

                    string categoria = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementCategory")?.Value
                    ) ?? "";

                    bool isFraming = OIC(categoria, "Structural Framing");
                    bool isColumn = OIC(categoria, "Structural Columns");

                    if (!isFraming && !isColumn)
                        continue;

                    candidatos++;

                    if (isFraming) framingCount++;
                    if (isColumn) columnsCount++;

                    string elementId = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementId")?.Value
                    ) ?? "";

                    string familia = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementFamily")?.Value
                    ) ?? "";

                    string tipo = SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementType")?.Value
                    ) ?? SafeDisplay(
                        elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementName")?.Value
                    ) ?? "";

                    string nivel = "Sin nivel";
                    try { nivel = LevelFromTree(item) ?? "Sin nivel"; } catch { }

                    string mark = "";
                    try
                    {
                        var pMark = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", "lcldrevit_parameter_-1001203");
                        if (pMark?.Value != null) mark = SafeDisplay(pMark.Value) ?? "";
                    }
                    catch { }

                    string materialInst = "";
                    try
                    {
                        var pMat = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", "lcldrevit_parameter_-1005500");
                        if (pMat?.Value != null) materialInst = SafeDisplay(pMat.Value) ?? "";
                    }
                    catch { }

                    var concretoKeywords = new[] { "concrete", "concreto", "hormigon", "masonry" };
                    bool esConcreto = !string.IsNullOrWhiteSpace(materialInst) &&
                                      concretoKeywords.Any(k => materialInst.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (esConcreto)
                    {
                        excConcreto++;
                        continue;
                    }

                    double length = 0;
                    double volume = 0;
                    try
                    {
                        double areaDummy = 0;
                        ReadQuantities(instNode, ref areaDummy, ref volume, ref length, categoria);
                    }
                    catch { }

                    // Lectura de TIPO segun spec
                    double nominalWeight = 0;
                    string sectionName = "";
                    string sectionShape = "";
                    string codeName = "";

                    foreach (var a in instNode.AncestorsAndSelf)
                    {
                        try
                        {
                            var typePC = a.PropertyCategories.FirstOrDefault(p =>
                                string.Equals(p.Name, "lcldrevit_tab_type", StringComparison.OrdinalIgnoreCase));
                            if (typePC == null) continue;

                            var pNW = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005512");
                            if (pNW?.Value != null) nominalWeight = ExtractDoubleFromVariant(pNW.Value);

                            var pSN = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005554");
                            if (pSN?.Value != null) sectionName = SafeDisplay(pSN.Value) ?? "";

                            var pSS = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005501");
                            if (pSS?.Value != null) sectionShape = SafeDisplay(pSS.Value) ?? "";

                            var pCN = typePC.Properties.FirstOrDefault(p => p.Name == "lcldrevit_parameter_-1005556");
                            if (pCN?.Value != null) codeName = SafeDisplay(pCN.Value) ?? "";

                            break;
                        }
                        catch
                        {
                            // No detener la corrida por un elemento
                            break;
                        }
                    }

                    double pesoKg = 0;
                    string metodo = "N/D";

                    if (nominalWeight > 0 && length > 0)
                    {
                        pesoKg = nominalWeight * length;
                        metodo = "2025+";
                        metodo2025++;
                    }
                    else if (volume > 0)
                    {
                        pesoKg = volume * DENSIDAD_ACERO;
                        metodo = "Vol×ρ";
                        metodoFallback++;
                    }
                    else
                    {
                        sinDatos++;
                    }

                    var steelRow = new SteelRow
                    {
                        Nivel = nivel,
                        Categoria = categoria,
                        Familia = Clean(familia),
                        Tipo = Clean(tipo),
                        NominalWeight = Math.Round(nominalWeight, 4),
                        SectionName = sectionName,
                        SectionShape = sectionShape,
                        CodeName = codeName,
                        MaterialEst = materialInst,
                        Length = Math.Round(length, 4),
                        Volume = Math.Round(volume, 4),
                        PesoKg = Math.Round(pesoKg, 2),
                        Metodo = metodo,
                        ElemId = elementId,
                        Mark = mark
                    };

                    string key = $"{steelRow.Nivel}|{steelRow.Categoria}|{steelRow.Familia}|{steelRow.Tipo}";

                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        bucket = new SteelAggregateBucket
                        {
                            Level = steelRow.Nivel,
                            Category = steelRow.Categoria,
                            Family = steelRow.Familia,
                            Type = steelRow.Tipo,
                            SectionName = steelRow.SectionName,
                            SectionShape = steelRow.SectionShape,
                            CodeName = steelRow.CodeName,
                            NominalWeightKgm = steelRow.NominalWeight
                        };
                        buckets[key] = bucket;
                    }

                    bucket.NumPieces++;
                    bucket.LengthTotalM += steelRow.Length;
                    bucket.VolumeTotalM3 += steelRow.Volume;
                    bucket.PesoTotalKg += steelRow.PesoKg;

                    if (metodo == "2025+") bucket.Metodo2025Count++;
                    else if (metodo == "Vol×ρ") bucket.MetodoFallbackCount++;
                    else bucket.MetodoNDCount++;

                    if (detailRows != null && detailRows.Count < MAX_DETAIL_ROWS)
                        detailRows.Add(steelRow);
                }
            });

            if (detailRows != null && detailRows.Count >= MAX_DETAIL_ROWS)
                warnings.Add("Detalle truncado por tamaño de la seleccion. Segmenta mas el alcance si necesitas detalle completo.");

            pre.CandidateItems = candidatos;

            var resumen = buckets.Values
                .OrderBy(x => x.Level).ThenBy(x => x.Category).ThenBy(x => x.Type)
                .Select(b =>
                {
                    string metodo = b.MetodoFallbackCount > 0 && b.Metodo2025Count > 0
                        ? "Mixto"
                        : (b.Metodo2025Count > 0 ? "2025+" : (b.MetodoFallbackCount > 0 ? "Vol×ρ" : "N/D"));

                    string advertencia = b.MetodoFallbackCount > 0
                        ? "Nominal Weight no disponible en algunos elementos. Se uso Vol x 7850 kg/m3."
                        : (b.MetodoNDCount > 0 ? "Sin datos de peso." : "");

                    return new SteelSummaryRow
                    {
                        Nivel = b.Level,
                        Categoria = b.Category,
                        Familia = b.Family,
                        Tipo = b.Type,
                        SectionName = b.SectionName,
                        SectionShape = b.SectionShape,
                        CodeName = b.CodeName,
                        NominalWeight = Math.Round(b.NominalWeightKgm, 4),
                        NumPiezas = b.NumPieces,
                        LengthTotal = Math.Round(b.LengthTotalM, 3),
                        VolumeTotal = Math.Round(b.VolumeTotalM3, 4),
                        PesoKg = Math.Round(b.PesoTotalKg, 2),
                        PesoTonRef = b.PesoTotalKg >= 1000 ? (double?)Math.Round(b.PesoTotalKg / 1000.0, 3) : null,
                        Metodo = metodo,
                        Advertencia = advertencia
                    };
                }).ToList();

            double pesoTotalKg = Math.Round(resumen.Sum(r => r.PesoKg), 2);

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = "run_preconstruccion_3_manual",
                ScopeMode = "selection",
                OutputMode = opt.OutputMode,
                Preflight = pre,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(pre),
                Data = new
                {
                    rutina = "Preconstruccion 3 Manual - Estructura Metalica",
                    ejecutado = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    total_elementos = resumen.Sum(r => r.NumPiezas),
                    total_tipos = resumen.Count,
                    peso_total_kg = pesoTotalKg,
                    peso_total_ton = pesoTotalKg >= 1000 ? (double?)Math.Round(pesoTotalKg / 1000.0, 3) : null,
                    diagnostico = new
                    {
                        elementos_seleccionados = selectedCount,
                        candidatos_steel = candidatos,
                        structural_framing = framingCount,
                        structural_columns = columnsCount,
                        metodo_2025 = metodo2025,
                        metodo_fallback = metodoFallback,
                        sin_datos = sinDatos,
                        excluidos_concreto = excConcreto,
                        modo = "manual_selection_mvp"
                    },
                    resumen,
                    detalle = returnDetail ? detailRows : null,
                    nota = resumen.Count == 0
                        ? "No se encontraron elementos validos en la seleccion manual."
                        : $"OK - {resumen.Sum(r => r.NumPiezas)} piezas | {pesoTotalKg} kg total"
                }
            };
        }

        public static object HighlightByCategory(List<string> cats, string nivel = null)
        {
            EnsureDoc();
            var items = new List<ModelItem>();

            OnUI(() =>
            {
                var seen = new HashSet<string>();
                int count = 0;

                foreach (var model in Doc.Models)
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!SafeHasGeometry(item)) continue;
                    if (count++ > 10000) break;

                    var guid = SafeCanonicalId(item);
                    if (!seen.Add(guid)) continue;

                    var cat = CatFromTree(item) ?? "";
                    var m = CM.Mapa.FirstOrDefault(x => OIC(cat, x.Key));
                    var nb = m.Key != null ? m.Value.N : cat;

                    if (cats?.Count > 0 && !cats.Any(f => OIC(nb, f) || OIC(cat, f))) continue;
                    if (!string.IsNullOrEmpty(nivel) && !OIC(LevelFromTree(item) ?? "", nivel)) continue;

                    items.Add(item);
                }

                var col = new ModelItemCollection();
                foreach (var mi in items) col.Add(mi);

                Application.ActiveDocument.CurrentSelection.Clear();
                Application.ActiveDocument.CurrentSelection.CopyFrom(col);
            });

            return new { resaltados = items.Count };
        }

        public static object ClearSelection()
        {
            EnsureDoc();
            OnUI(() => Application.ActiveDocument.CurrentSelection.Clear());
            return new { ok = true };
        }

        public static object DumpProperties(int maxItems = 3)
        {
            EnsureDoc();
            var result = new List<object>();

            OnUI(() =>
            {
                int n = 0;
                foreach (var model in Doc.Models)
                {
                    foreach (var item in model.RootItem.DescendantsAndSelf)
                    {
                        if (!SafeHasGeometry(item)) continue;
                        if (n++ >= maxItems) break;

                        result.Add(new
                        {
                            geo = item.DisplayName,
                            clase = item.ClassDisplayName,
                            arbol = item.AncestorsAndSelf.Reverse().Select(a => new
                            {
                                display = a.DisplayName,
                                clase = a.ClassDisplayName,
                                geo = SafeHasGeometry(a),
                                pcs = a.PropertyCategories.Where(pc => pc.Properties.Any()).Select(pc => new
                                {
                                    pc_d = pc.DisplayName,
                                    pc_n = pc.Name,
                                    props = pc.Properties.Select(p => new
                                    {
                                        d = p.DisplayName,
                                        n = p.Name,
                                        v = p.Value?.ToString() ?? ""
                                    }).ToList()
                                }).ToList()
                            }).ToList()
                        });
                    }
                    if (n >= maxItems) break;
                }
            });

            return new { nota = $"{result.Count} nodos con geometría.", items = result };
        }

        public static object DumpGeoSet(string setName, int maxItems = 3)
        {
            EnsureDoc();
            var result = new List<object>();
            int totalSel = 0;

            OnUI(() =>
            {
                var items = GetItemsFromSetInternal(setName);
                totalSel = items.Count;
                int n = 0;
                var seen = new HashSet<string>();

                foreach (var item in items)
                {
                    var geos = SafeHasGeometry(item)
                        ? new[] { item }
                        : item.DescendantsAndSelf.Where(d => SafeHasGeometry(d)).ToArray();

                    foreach (var geo in geos)
                    {
                        if (n >= maxItems) break;
                        var key = SafeCanonicalId(geo);
                        if (!seen.Add(key)) continue;
                        n++;

                        result.Add(new
                        {
                            geo = geo.DisplayName,
                            clase = geo.ClassDisplayName,
                            arbol = geo.AncestorsAndSelf.Reverse().Select(a => new
                            {
                                display = a.DisplayName,
                                clase = a.ClassDisplayName,
                                geo_a = SafeHasGeometry(a),
                                pcs = a.PropertyCategories.Where(pc => pc.Properties.Any()).Select(pc => new
                                {
                                    pc_d = pc.DisplayName,
                                    pc_n = pc.Name,
                                    props = pc.Properties.Select(p => new
                                    {
                                        d = p.DisplayName,
                                        n = p.Name,
                                        v = p.Value?.ToString() ?? ""
                                    }).ToList()
                                }).ToList()
                            }).ToList()
                        });
                    }

                    if (n >= maxItems) break;
                }
            });

            return new { set = setName, total_sel = totalSel, nota = $"{result.Count} nodos geo.", items = result };
        }

        public static object DumpInstancia(string categoria = "Walls", int maxItems = 3)
        {
            EnsureDoc();
            var result = new List<object>();

            OnUI(() =>
            {
                int n = 0;
                int visited = 0;
                int maxVisit = 10000;
                var seen = new HashSet<string>();

                foreach (var model in Doc.Models)
                {
                    foreach (var item in model.RootItem.DescendantsAndSelf)
                    {
                        if (n >= maxItems || visited++ > maxVisit) break;
                        if (!SafeHasGeometry(item)) continue;

                        var snap = TryBuildSnapshot(item);
                        if (snap == null) continue;
                        if (!string.IsNullOrEmpty(categoria) && !OIC(snap.Category, categoria)) continue;
                        if (!seen.Add(snap.CanonicalId)) continue;

                        n++;
                        result.Add(new
                        {
                            nodo = snap.Type,
                            geo = item.DisplayName,
                            cat = snap.Category,
                            nivel = snap.Level,
                            area_m2 = snap.AreaM2,
                            vol_m3 = snap.VolumeM3,
                            long_m = snap.LengthM,
                            material = snap.Material,
                            nominal_weight = snap.NominalWeightKgm,
                            section_name = snap.SectionName
                        });
                    }

                    if (n >= maxItems || visited > maxVisit) break;
                }
            });

            return new
            {
                categoria,
                n = result.Count,
                nota = "area_m2/vol_m3/long_m deben tener valor si el NWC exportó esos parámetros.",
                items = result
            };
        }

        public static object GetParametersForCategory(string category)
        {
            EnsureDoc();
            var result = new List<object>();

            OnUI(() =>
            {
                int n = 0;
                foreach (var model in Doc.Models)
                {
                    foreach (var item in model.RootItem.DescendantsAndSelf)
                    {
                        if (!SafeHasGeometry(item)) continue;
                        if (n >= 2) break;

                        var cat = CatFromTree(item);
                        if (!string.IsNullOrEmpty(category) && !OIC(cat ?? "", category)) continue;
                        n++;

                        result.Add(new
                        {
                            cat = cat,
                            nivel = LevelFromTree(item),
                            fam = FamFromTree(item),
                            arbol = item.AncestorsAndSelf.Reverse().Select(a => new
                            {
                                d = a.DisplayName,
                                cls = a.ClassDisplayName,
                                pcs = a.PropertyCategories.Where(pc => pc.Properties.Any()).Select(pc => new
                                {
                                    pc_d = pc.DisplayName,
                                    pc_n = pc.Name,
                                    props = pc.Properties.Select(p => new
                                    {
                                        d = p.DisplayName,
                                        n = p.Name,
                                        v = p.Value?.ToString() ?? ""
                                    }).ToList()
                                }).ToList()
                            }).ToList()
                        });
                    }

                    if (n >= 2) break;
                }
            });

            return new { buscado = category, n = result.Count, nota = result.Count == 0 ? "No encontrado." : "OK", items = result };
        }

        public static object ExportJson(string path = null)
        {
            EnsureDoc();
            var data = ExtractQuantities(new List<string>(), new RunOptions
            {
                ScopeMode = "all",
                OutputMode = "summary",
                StrictLimits = true
            });

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            path = !string.IsNullOrEmpty(path)
                ? path
                : Path.Combine(desktop, "BOQ_" + Path.GetFileNameWithoutExtension(Doc.Title ?? "m") + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".json");

            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented), Encoding.UTF8);
            return new { ok = true, archivo = path };
        }

        static object RunCorridaGeneral(string toolName, string rutina, List<string> cats, RunOptions opt, ExecutionBudget budget)
        {
            EnsureDoc();
            var pre = PreflightScope(opt, budget, s => SnapshotMatchesGeneralFilter(s, cats, opt.Level));

            if (!pre.AllowRun)
            {
                return new ToolEnvelope<object>
                {
                    Ok = false,
                    Tool = toolName,
                    ScopeMode = opt.ScopeMode,
                    OutputMode = "summary",
                    Preflight = pre,
                    UserMessage = pre.Message
                };
            }

            if (string.Equals(opt.OutputMode, "auto", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = pre.ForceSummary ? "summary" : "detail";

            if (pre.ForceSummary && string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase))
                opt.OutputMode = "summary";

            bool returnDetail = ShouldReturnDetail(opt, budget);
            var detailRows = returnDetail ? new List<BoqRow>() : null;
            var warnings = new List<string>();
            var buckets = new Dictionary<string, AggregateBucket>(StringComparer.OrdinalIgnoreCase);

            OnUI(() =>
            {
                int visited = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in ResolveScopeItems(opt))
                {
                    if (++visited > Math.Min(opt.MaxNodes, budget.MaxNodesToVisit))
                    {
                        warnings.Add("Se alcanzó el límite de nodos visitados.");
                        break;
                    }

                    if (!SafeHasGeometry(item) && !HasElementCategory(item)) continue;

                    var snap = TryBuildSnapshot(item);
                    if (snap == null) continue;
                    if (!seen.Add(snap.CanonicalId)) continue;
                    if (!SnapshotMatchesGeneralFilter(snap, cats, opt.Level)) continue;

                    var row = ToBoqRow(snap);
                    var key = $"{row.Nivel}|{row.Categoria}|{row.Familia}|{row.Tipo}|{row.Unidad}";

                    if (!buckets.TryGetValue(key, out var bucket))
                    {
                        bucket = new AggregateBucket
                        {
                            Level = row.Nivel,
                            Category = row.Categoria,
                            Family = row.Familia,
                            Type = row.Tipo,
                            Unit = row.Unidad,
                            TypeDesc = row.TipoDesc,
                            TypeMaterial = row.TipoMaterial,
                            TypeWidth = row.TipoAncho,
                            TypeThickness = row.TipoEspesor
                        };
                        buckets[key] = bucket;
                    }

                    bucket.Count++;
                    bucket.LengthTotal += row.Longitud;
                    bucket.AreaTotal += row.Area;
                    bucket.VolumeTotal += row.Volumen;
                    bucket.QuantityTotal += row.Cantidad;

                    if (detailRows != null && detailRows.Count < budget.MaxDetailRows)
                        detailRows.Add(row);
                }
            });

            if (detailRows != null && detailRows.Count >= budget.MaxDetailRows)
                warnings.Add("Detalle truncado por tamaño del alcance. Segmenta más el modelo si necesitas detalle completo.");

            var resumen = buckets.Values
                .Select(b => new BoqSummaryRow
                {
                    Nivel = b.Level,
                    Cat = b.Category,
                    Familia = b.Family,
                    Tipo = b.Type,
                    Unidad = b.Unit,
                    N = b.Count,
                    Long_ = Math.Round(b.LengthTotal, 2),
                    Area = Math.Round(b.AreaTotal, 2),
                    Vol = Math.Round(b.VolumeTotal, 2),
                    Cantidad = Math.Round(b.QuantityTotal, 2),
                    TipoDesc = b.TypeDesc,
                    TipoMaterial = b.TypeMaterial,
                    TipoAncho = Math.Round(b.TypeWidth, 4),
                    TipoEspesor = Math.Round(b.TypeThickness, 4)
                })
                .OrderBy(r => r.Cat).ThenBy(r => r.Nivel).ThenBy(r => r.Tipo)
                .ToList();

            return new ToolEnvelope<object>
            {
                Ok = true,
                Tool = toolName,
                ScopeMode = opt.ScopeMode,
                OutputMode = opt.OutputMode,
                Preflight = pre,
                Warnings = warnings,
                UserMessage = BuildUserScopeMessage(pre),
                Data = new
                {
                    rutina,
                    total = resumen.Sum(r => r.N),
                    resumen,
                    detalle = returnDetail ? detailRows : null
                }
            };
        }

        static ScopePreflight PreflightScope(RunOptions opt, ExecutionBudget budget, Func<ElementSnapshot, bool> candidateFilter)
        {
            EnsureDoc();

            var levels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int visitedNodes = 0;
            int candidateItems = 0;
            int geometricItems = 0;
            bool hitNodeBudget = false;

            OnUI(() =>
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var item in ResolveScopeItems(opt))
                {
                    visitedNodes++;
                    if (visitedNodes > Math.Min(opt.MaxNodes, budget.MaxNodesToVisit))
                    {
                        hitNodeBudget = true;
                        break;
                    }

                    if (SafeHasGeometry(item)) geometricItems++;

                    if (!SafeHasGeometry(item) && !HasElementCategory(item))
                        continue;

                    var snap = TryBuildSnapshot(item);
                    if (snap == null) continue;
                    if (!seen.Add(snap.CanonicalId)) continue;
                    if (!candidateFilter(snap)) continue;

                    candidateItems++;
                    if (!string.IsNullOrWhiteSpace(snap.Level)) levels.Add(snap.Level);
                    if (!string.IsNullOrWhiteSpace(snap.Category)) cats.Add(snap.Category);

                    if (candidateItems > opt.MaxItems && opt.StrictLimits)
                        break;
                }
            });

            var pre = new ScopePreflight
            {
                ScopeResolved = ResolveScopeName(opt),
                VisitedNodes = visitedNodes,
                CandidateItems = candidateItems,
                GeometricItems = geometricItems,
                DistinctLevels = levels.Count,
                DistinctCategories = cats.Count
            };

            if (hitNodeBudget || visitedNodes > budget.MaxNodesToVisit)
            {
                pre.RiskBand = "red";
                pre.AllowRun = false;
                pre.ForceSummary = true;
                pre.Message = "El alcance excede el umbral seguro de nodos visitados. Segmenta más el modelo con Selection Sets o por nivel.";
                FillSegmentationHints(pre, levels, cats);
                return pre;
            }

            if (candidateItems > budget.YellowCandidateLimit)
            {
                pre.RiskBand = "red";
                pre.AllowRun = !opt.StrictLimits;
                pre.ForceSummary = true;
                pre.Message = "El alcance excede el umbral seguro de esta corrida. Segmenta más el modelo con Selection Sets o por nivel.";
                FillSegmentationHints(pre, levels, cats);
                return pre;
            }

            if (candidateItems > budget.GreenCandidateLimit)
            {
                pre.RiskBand = "yellow";
                pre.AllowRun = true;
                pre.ForceSummary = true;
                pre.Message = "El alcance es procesable, pero por estabilidad se devolverá solo resumen.";
                FillSegmentationHints(pre, levels, cats);
                return pre;
            }

            pre.RiskBand = "green";
            pre.AllowRun = true;
            pre.ForceSummary = false;
            pre.Message = "La corrida puede ejecutarse completa con este alcance.";
            FillSegmentationHints(pre, levels, cats);
            return pre;
        }

        static void FillSegmentationHints(ScopePreflight pre, HashSet<string> levels, HashSet<string> cats)
        {
            if (levels.Count > 0)
            {
                foreach (var lvl in levels.Take(3))
                    pre.SuggestedSegmentation.Add("Correr por nivel: " + lvl);
            }

            if (cats.Count > 1)
            {
                foreach (var cat in cats.Take(3))
                    pre.SuggestedSegmentation.Add("Separar categoría: " + cat);
            }

            pre.SuggestedSegmentation.Add("Usar Selection Sets más segmentados.");
        }

        static string BuildUserScopeMessage(ScopePreflight pre)
        {
            if (pre == null) return "";
            if (pre.RiskBand == "green") return pre.Message;
            if (pre.RiskBand == "yellow") return pre.Message + " Si necesitas detalle, segmenta por nivel o por Selection Set.";
            return pre.Message;
        }

        static bool ShouldReturnDetail(RunOptions opt, ExecutionBudget budget) =>
            string.Equals(opt.OutputMode, "detail", StringComparison.OrdinalIgnoreCase) &&
            budget.MaxDetailRows > 0;

        static string ResolveScopeName(RunOptions opt)
        {
            return opt.ScopeMode switch
            {
                "selection_set" => $"selection_set:{opt.SelectionSet}",
                "selection" => "current_selection",
                "level" => $"level:{opt.Level}",
                _ => "all"
            };
        }

        static IEnumerable<ModelItem> ResolveScopeItems(RunOptions opt)
        {
            if (Doc == null) yield break;

            switch ((opt.ScopeMode ?? "all").ToLowerInvariant())
            {
                case "selection":
                    foreach (var mi in Application.ActiveDocument.CurrentSelection.SelectedItems)
                        yield return mi;
                    break;

                case "selection_set":
                    foreach (var mi in GetItemsFromSetInternal(opt.SelectionSet))
                        yield return mi;
                    break;

                case "level":
                    foreach (var model in Doc.Models)
                    foreach (var item in model.RootItem.DescendantsAndSelf)
                    {
                        if (!SafeHasGeometry(item) && !HasElementCategory(item)) continue;
                        var lvl = LevelFromTree(item) ?? "";
                        if (OIC(lvl, opt.Level))
                            yield return item;
                    }
                    break;

                default:
                    foreach (var model in Doc.Models)
                    foreach (var item in model.RootItem.DescendantsAndSelf)
                        yield return item;
                    break;
            }
        }

        static ElementSnapshot TryBuildSnapshot(ModelItem item)
        {
            try
            {
                if (item == null) return null;

                PropertyCategory elemPC = null;
                ModelItem instNode = null;

                foreach (var a in item.AncestorsAndSelf)
                {
                    PropertyCategory pc = null;
                    try { pc = a.PropertyCategories.FindCategoryByDisplayName("Componente"); } catch { }
                    if (pc == null) try { pc = a.PropertyCategories.FindCategoryByDisplayName("Element"); } catch { }
                    if (pc == null)
                        pc = a.PropertyCategories.FirstOrDefault(p =>
                            string.Equals(p.Name, CAT_REVIT_ELEMENT, StringComparison.OrdinalIgnoreCase));

                    if (pc == null) continue;

                    var catProp = pc.Properties.FirstOrDefault(p =>
                        string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));

                    if (catProp?.Value == null) continue;
                    var catVal = SafeDisplay(catProp.Value);
                    if (string.IsNullOrWhiteSpace(catVal)) continue;

                    instNode = a;
                    elemPC = pc;
                    break;
                }

                if (instNode == null || elemPC == null) return null;

                var snap = new ElementSnapshot
                {
                    CanonicalId = SafeCanonicalId(instNode),
                    ElementId = SafeDisplay(elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementId")?.Value) ?? "",
                    Category = SafeDisplay(elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementCategory")?.Value) ?? "",
                    Family = SafeDisplay(elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementFamily")?.Value) ?? "",
                    Type = SafeDisplay(elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementType")?.Value)
                           ?? SafeDisplay(elemPC.Properties.FirstOrDefault(p => p.Name == "LcRevitPropertyElementName")?.Value) ?? "",
                    Level = LevelFromTree(item) ?? "Sin nivel"
                };

                var matInst = ReadStringProp(instNode, CAT_REVIT_ELEMENT, PROP_STRUCT_MAT);
                var mark = ReadStringProp(instNode, CAT_REVIT_ELEMENT, PROP_MARK);

                snap.Material = matInst ?? "";
                snap.Mark = mark ?? "";

                double area = 0, vol = 0, len = 0;
                ReadQuantities(instNode, ref area, ref vol, ref len, snap.Category);
                snap.AreaM2 = Math.Round(area, 4);
                snap.VolumeM3 = Math.Round(vol, 4);
                snap.LengthM = Math.Round(len, 4);

                var typeKey = $"{snap.Category}|{snap.Family}|{snap.Type}";
                if (!_steelTypeCache.TryGetValue(typeKey, out var steelCache))
                {
                    steelCache = ReadSteelTypeCache(instNode, snap.Category, snap.Family, snap.Type);
                    _steelTypeCache[typeKey] = steelCache;
                }

                snap.NominalWeightKgm = steelCache.NominalWeightKgm;
                snap.SectionName = steelCache.SectionName;
                snap.SectionShape = steelCache.SectionShape;
                snap.CodeName = steelCache.CodeName;

                if (string.IsNullOrWhiteSpace(snap.Material))
                    snap.Material = steelCache.Material;

                ReadGenericTypeProps(instNode, out var typeDesc, out var typeMat, out var typeW, out var typeTh);
                snap.TypeDesc = typeDesc;
                snap.TypeMaterial = !string.IsNullOrWhiteSpace(typeMat) ? typeMat : snap.Material;
                snap.TypeWidth = typeW;
                snap.TypeThickness = typeTh;

                return snap;
            }
            catch
            {
                return null;
            }
        }

        static SteelTypeCache ReadSteelTypeCache(ModelItem instNode, string cat, string fam, string type)
        {
            var cache = new SteelTypeCache
            {
                TypeKey = $"{cat}|{fam}|{type}"
            };

            foreach (var a in instNode.AncestorsAndSelf)
            {
                var tpc = a.PropertyCategories.FirstOrDefault(p =>
                    string.Equals(p.Name, "lcldrevit_tab_type", StringComparison.OrdinalIgnoreCase));
                if (tpc == null) continue;

                cache.NominalWeightKgm = ExtractDoubleFromVariant(tpc.Properties.FirstOrDefault(p => p.Name == PROP_NOMINAL_WEIGHT)?.Value);
                cache.SectionName = SafeDisplay(tpc.Properties.FirstOrDefault(p => p.Name == PROP_SECTION_NAME)?.Value) ?? "";
                cache.SectionShape = SafeDisplay(tpc.Properties.FirstOrDefault(p => p.Name == PROP_SECTION_SHAPE)?.Value) ?? "";
                cache.CodeName = SafeDisplay(tpc.Properties.FirstOrDefault(p => p.Name == PROP_CODE_NAME)?.Value) ?? "";
                cache.Material = SafeDisplay(tpc.Properties.FirstOrDefault(p => p.Name == PROP_STRUCT_MAT)?.Value) ?? "";
                break;
            }

            return cache;
        }

        static void ReadGenericTypeProps(ModelItem instNode, out string tipoDesc, out string tipoMat, out double tipoAncho, out double tipoEspesor)
        {
            tipoDesc = "";
            tipoMat = "";
            tipoAncho = 0;
            tipoEspesor = 0;

            foreach (var a in instNode.AncestorsAndSelf)
            {
                var typePC = a.PropertyCategories.FirstOrDefault(p =>
                    string.Equals(p.Name, "lcldrevit_tab_type", StringComparison.OrdinalIgnoreCase));
                if (typePC == null) continue;

                tipoDesc = SafeDisplay(typePC.Properties.FirstOrDefault(p => p.Name == PROP_TYPE_DESC)?.Value) ?? "";
                tipoMat = SafeDisplay(typePC.Properties.FirstOrDefault(p => p.Name == PROP_STRUCT_MAT)?.Value) ?? "";
                tipoAncho = ExtractDoubleFromVariant(typePC.Properties.FirstOrDefault(p => p.Name == PROP_TYPE_WIDTH)?.Value);

                foreach (var eid in PROP_TYPE_THICKNESS)
                {
                    var p = typePC.Properties.FirstOrDefault(x => x.Name == eid);
                    var v = ExtractDoubleFromVariant(p?.Value);
                    if (v > 0)
                    {
                        tipoEspesor = v;
                        break;
                    }
                }

                break;
            }
        }

        static BoqRow ToBoqRow(ElementSnapshot snap)
        {
            string boq = snap.Category;
            string unit = "";

            var m = CM.Mapa.FirstOrDefault(x => OIC(snap.Category, x.Key));
            if (m.Key != null)
            {
                boq = m.Value.N;
                unit = m.Value.U;
            }

            double qty;
            string u;

            if (unit == "pza" || CM.EsPza.Contains(snap.Category))
            {
                qty = 1;
                u = "pza";
            }
            else if (unit == "m2" && snap.AreaM2 > 0)
            {
                qty = Math.Round(snap.AreaM2, 3);
                u = "m2";
            }
            else if (unit == "m3" && snap.VolumeM3 > 0)
            {
                qty = Math.Round(snap.VolumeM3, 3);
                u = "m3";
            }
            else if (unit == "ml" && snap.LengthM > 0)
            {
                qty = Math.Round(snap.LengthM, 3);
                u = "ml";
            }
            else if (snap.AreaM2 > 0)
            {
                qty = Math.Round(snap.AreaM2, 3);
                u = "m2";
            }
            else if (snap.VolumeM3 > 0)
            {
                qty = Math.Round(snap.VolumeM3, 3);
                u = "m3";
            }
            else if (snap.LengthM > 0)
            {
                qty = Math.Round(snap.LengthM, 3);
                u = "ml";
            }
            else
            {
                qty = 1;
                u = "pza";
            }

            return new BoqRow
            {
                Nivel = snap.Level,
                Categoria = boq,
                Familia = Clean(snap.Family),
                Tipo = Clean(snap.Type),
                TipoDesc = snap.TypeDesc,
                TipoMaterial = snap.TypeMaterial,
                TipoAncho = Math.Round(snap.TypeWidth, 4),
                TipoEspesor = Math.Round(snap.TypeThickness, 4),
                Area = Math.Round(snap.AreaM2, 4),
                Volumen = Math.Round(snap.VolumeM3, 4),
                Longitud = Math.Round(snap.LengthM, 4),
                Cantidad = qty,
                Unidad = u,
                ElemId = snap.ElementId
            };
        }

        static bool SnapshotMatchesGeneralFilter(ElementSnapshot s, List<string> cats, string level)
        {
            if (s == null) return false;

            if (cats != null && cats.Count > 0 && !cats.Any(f => OIC(s.Category, f)))
                return false;

            if (!string.IsNullOrWhiteSpace(level) && !OIC(s.Level, level))
                return false;

            return true;
        }

        static bool SnapshotMatchesSteelFilter(ElementSnapshot s)
        {
            if (s == null) return false;

            bool isTargetCategory =
                OIC(s.Category, "Structural Framing") ||
                OIC(s.Category, "Structural Columns");

            if (!isTargetCategory) return false;

            string mat = s.Material ?? "";
            var aceroKeywords = new[] { "steel", "acero", "metal", "metalic", "metallic", "w shape", "hss", "pipe" };
            var concretoKeywords = new[] { "concrete", "concreto", "hormigon", "masonry" };

            bool esConcreto = !string.IsNullOrWhiteSpace(mat) &&
                concretoKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
            if (esConcreto) return false;

            bool esAcero = string.IsNullOrWhiteSpace(mat) ||
                aceroKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

            return esAcero;
        }

        static bool LooksConcreteSteelCandidate(ElementSnapshot s)
        {
            if (s == null) return false;
            bool isTargetCategory =
                OIC(s.Category, "Structural Framing") ||
                OIC(s.Category, "Structural Columns");

            if (!isTargetCategory) return false;

            string mat = s.Material ?? "";
            var concretoKeywords = new[] { "concrete", "concreto", "hormigon", "masonry" };
            return !string.IsNullOrWhiteSpace(mat) &&
                   concretoKeywords.Any(k => mat.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        static void ReadQuantities(ModelItem node, ref double area, ref double vol, ref double len, string catRevit = null)
        {
            bool isStructural = !string.IsNullOrEmpty(catRevit) &&
                                (OIC(catRevit, "Structural Column") ||
                                 OIC(catRevit, "Columna") ||
                                 OIC(catRevit, "Structural Framing") ||
                                 OIC(catRevit, "Viga") ||
                                 OIC(catRevit, "Framing"));

            string lenProp = isStructural ? PROP_LEN_STRUCT : PROP_LEN_DEFAULT;

            try
            {
                var pArea = node.PropertyCategories.FindPropertyByName(CAT_REVIT_ELEMENT, PROP_AREA);
                if (pArea?.Value != null)
                {
                    var v = ExtractDoubleFromVariant(pArea.Value);
                    if (v > 0) area = v;
                }
            }
            catch { }

            try
            {
                var pVol = node.PropertyCategories.FindPropertyByName(CAT_REVIT_ELEMENT, PROP_VOL);
                if (pVol?.Value != null)
                {
                    var v = ExtractDoubleFromVariant(pVol.Value);
                    if (v > 0) vol = v;
                }
            }
            catch { }

            try
            {
                var pLen = node.PropertyCategories.FindPropertyByName(CAT_REVIT_ELEMENT, lenProp);
                if (pLen?.Value != null)
                {
                    var v = ExtractDoubleFromVariant(pLen.Value);
                    if (v > 0) len = v;
                }
            }
            catch { }

            if (area > 0 || vol > 0 || len > 0) return;

            foreach (var pc in node.PropertyCategories)
                ReadQuantitiesFromPC(pc, ref area, ref vol, ref len);

            if (area == 0 && vol == 0 && len == 0 && node.Parent != null)
            {
                foreach (var pc in node.Parent.PropertyCategories)
                    ReadQuantitiesFromPC(pc, ref area, ref vol, ref len);
            }
        }

        static void ReadQuantitiesFromPC(PropertyCategory pc, ref double area, ref double vol, ref double len)
        {
            foreach (var p in pc.Properties)
            {
                if (p.Value == null) continue;
                var pn = p.Name ?? "";

                bool isArea = string.Equals(pn, PROP_AREA, StringComparison.OrdinalIgnoreCase);
                bool isVol = string.Equals(pn, PROP_VOL, StringComparison.OrdinalIgnoreCase);
                bool isLen = string.Equals(pn, PROP_LEN_DEFAULT, StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(pn, PROP_LEN_STRUCT, StringComparison.OrdinalIgnoreCase);

                if (!isArea && !isVol && !isLen)
                {
                    var dn = RemoveAccents(p.DisplayName ?? "");
                    isArea = OIC(dn, "Area") && !OIC(dn, "Category") && !OIC(dn, "Number") && !OIC(dn, "Room");
                    isVol = OIC(dn, "Volume") || OIC(dn, "Volumen");
                    isLen = (OIC(dn, "Length") || OIC(dn, "Longitud")) &&
                            !OIC(dn, "Number") && !OIC(dn, "Wall") && !OIC(dn, "Curve");
                }

                if (!isArea && !isVol && !isLen) continue;

                double v = ExtractDoubleFromVariant(p.Value);

                if (isArea && v > 0 && v > area) area = v;
                if (isVol && v > 0 && v > vol) vol = v;
                if (isLen && v > 0 && v > len) len = v;
            }
        }

        static string ReadStructuralLocation(ModelItem instNode, string categoria)
        {
            if (instNode == null) return "";

            bool aplica =
                OIC(categoria, "Structural Framing") ||
                OIC(categoria, "Structural Columns") ||
                OIC(categoria, "Structural Foundations");

            if (!aplica) return "";

            string[] candidateInternalNames = new[]
            {
                "lcldrevit_parameter_-1005208",
                "lcldrevit_parameter_-1005214",
                "lcldrevit_parameter_-1005215",
                "lcldrevit_parameter_-1005216"
            };

            foreach (var propName in candidateInternalNames)
            {
                try
                {
                    var p = instNode.PropertyCategories.FindPropertyByName("LcRevitData_Element", propName);
                    if (p?.Value != null)
                    {
                        var s = SafeDisplay(p.Value) ?? "";
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                    }
                }
                catch { }
            }

            try
            {
                var elemPc = instNode.PropertyCategories.FirstOrDefault(p =>
                    string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));

                if (elemPc != null)
                {
                    foreach (var p in elemPc.Properties)
                    {
                        var dn = RemoveAccents(p.DisplayName ?? "");
                        if (OIC(dn, "Entreje") ||
                            OIC(dn, "Ubicacion") ||
                            OIC(dn, "Location") ||
                            OIC(dn, "Grid") ||
                            OIC(dn, "Eje"))
                        {
                            var s = SafeDisplay(p.Value) ?? "";
                            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        static double ExtractDoubleFromVariant(VariantData v)
        {
            const double FT2_TO_M2 = 10.763910417;
            const double FT3_TO_M3 = 35.314666721;

            if (v == null) return 0;

            try
            {
                switch (v.DataType)
                {
                    case VariantDataType.DoubleArea:
                        return v.ToDoubleArea() / FT2_TO_M2;
                    case VariantDataType.DoubleVolume:
                        return v.ToDoubleVolume() / FT3_TO_M3;
                    case VariantDataType.DoubleLength:
                        return v.ToDoubleLength();
                    case VariantDataType.Double:
                        return v.ToDouble();
                    case VariantDataType.Int32:
                        return v.ToInt32();
                    default:
                        var ds = "";
                        try { ds = v.ToDisplayString() ?? ""; } catch { }
                        return SplitNum(ds);
                }
            }
            catch
            {
                return 0;
            }
        }

        static string SafeDisplay(VariantData v)
        {
            if (v == null) return null;
            try
            {
                var s = v.ToDisplayString();
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            catch
            {
                return null;
            }
        }

        static string ReadStringProp(ModelItem item, string catInternal, string propInternal)
        {
            try
            {
                var dp = item.PropertyCategories.FindPropertyByName(catInternal, propInternal);
                if (dp?.Value == null) return null;
                return SafeDisplay(dp.Value) ?? dp.Value.ToString();
            }
            catch
            {
                return null;
            }
        }

        static double SplitNum(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();

            int col = s.IndexOf(':');
            if (col >= 0 && col < 25 && !s.Substring(0, col).Any(char.IsDigit))
                s = s.Substring(col + 1).Trim();

            var m = _reNumUnit.Match(s);
            if (m.Success) return NormalizeAndParse(m.Groups["num"].Value.Replace(" ", ""));

            var m2 = _reFirstNum.Match(s);
            if (m2.Success)
            {
                int e = m2.Index + m2.Length;
                if (e < s.Length && char.IsLetter(s[e])) return 0;
                return NormalizeAndParse(m2.Value);
            }

            return 0;
        }

        static double NormalizeAndParse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            string t = token.Trim();

            if (t.Contains(',') && t.Contains('.'))
            {
                int lastDot = t.LastIndexOf('.');
                int lastComma = t.LastIndexOf(',');
                t = (lastComma > lastDot)
                    ? t.Replace(".", "").Replace(',', '.')
                    : t.Replace(",", "");
            }
            else if (t.Contains(','))
            {
                t = t.Replace(',', '.');
            }

            return double.TryParse(
                t,
                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                System.Globalization.CultureInfo.InvariantCulture,
                out double d) ? d : 0;
        }

        static string CatFromTree(ModelItem item)
        {
            foreach (var a in item.AncestorsAndSelf)
            {
                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name, "LcOaNode", StringComparison.OrdinalIgnoreCase)) continue;
                    bool isCol = false, isCat = false;

                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name, "LcOaSceneBaseClassName") &&
                            (p.Value?.ToString() ?? "").Contains("LcRevitCollection")) isCol = true;

                        if (string.Equals(p.Name, "LcOaSceneBaseClassUserName"))
                        {
                            var v = p.Value?.ToString() ?? "";
                            if (OIC(v, "Categoria") || OIC(v, "Category")) isCat = true;
                        }
                    }

                    if (isCol && isCat) return a.DisplayName;
                }
            }
            return null;
        }

        static string LevelFromTree(ModelItem item)
        {
            foreach (var a in item.AncestorsAndSelf)
            {
                bool isLayer = false;
                string layerName = null;

                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name, "LcOaNode", StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name, "LcOaSceneBaseClassName") &&
                            (p.Value?.ToString() ?? "").Contains("LcRevitLayer")) isLayer = true;

                        if (string.Equals(p.Name, "LcOaNodeLayer"))
                            layerName = p.Value?.ToString() ?? "";
                    }
                }

                if (isLayer) return !string.IsNullOrEmpty(layerName) ? layerName : a.DisplayName;
            }

            return null;
        }

        static string FamFromTree(ModelItem item)
        {
            foreach (var a in item.AncestorsAndSelf)
            {
                if (a.PropertyCategories.Any(pc => string.Equals(pc.Name, CAT_REVIT_ELEMENT, StringComparison.OrdinalIgnoreCase)))
                    continue;

                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name, "LcOaNode", StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name, "LcOaSceneBaseClassUserName") && OIC(p.Value?.ToString() ?? "", "Familia"))
                            return a.DisplayName;
                    }
                }
            }

            return null;
        }

        static string TipoFromTree(ModelItem item)
        {
            foreach (var a in item.AncestorsAndSelf)
            {
                foreach (var pc in a.PropertyCategories)
                {
                    if (string.Equals(pc.Name, "lcldrevit_tab_type", StringComparison.OrdinalIgnoreCase))
                        return a.DisplayName;

                    if (!string.Equals(pc.Name, "LcOaNode", StringComparison.OrdinalIgnoreCase)) continue;

                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name, "LcOaSceneBaseClassUserName") && OIC(p.Value?.ToString() ?? "", "Tipo"))
                            return a.DisplayName;
                    }
                }
            }

            return null;
        }

        static bool SafeHasGeometry(ModelItem item)
        {
            try { return item != null && item.HasGeometry; }
            catch { return false; }
        }

        static bool HasElementCategory(ModelItem item)
        {
            try
            {
                return item.PropertyCategories.Any(p =>
                    string.Equals(p.Name, CAT_REVIT_ELEMENT, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        static string SafeCanonicalId(ModelItem item)
        {
            try
            {
                return item.InstanceGuid.ToString();
            }
            catch
            {
                try
                {
                    return string.Join("|", item.AncestorsAndSelf.Reverse().Select(x => x.DisplayName));
                }
                catch
                {
                    return Guid.NewGuid().ToString("N");
                }
            }
        }

        static List<ModelItem> GetItemsFromSetInternal(string setName)
        {
            var found = FindSet(Doc.SelectionSets.Value, setName);
            if (found == null) throw new Exception("Selection Set '" + setName + "' no encontrado.");

            var items = new List<ModelItem>();
            var src = Application.ActiveDocument.SelectionSets.CreateSelectionSource(found);
            var col = src.TryGetSelectedItems(Application.ActiveDocument);

            if (col != null && col.Count > 0)
            {
                foreach (var mi in col) items.Add(mi);
            }
            else if (found is SelectionSet ss)
            {
                try
                {
                    var si = ss.GetSelectedItems(Application.ActiveDocument);
                    if (si != null) foreach (var mi in si) items.Add(mi);
                }
                catch { }
            }
            else if (found is GroupItem gi)
            {
                var gcol = new ModelItemCollection();
                CollectGroup(gi, gcol);
                foreach (var mi in gcol) items.Add(mi);
            }

            return items;
        }

        static void CollectGroup(GroupItem group, ModelItemCollection col)
        {
            foreach (var child in group.Children)
            {
                if (child is SelectionSet ss)
                {
                    try
                    {
                        var it = ss.GetSelectedItems(Application.ActiveDocument);
                        if (it != null) foreach (var mi in it) col.Add(mi);
                    }
                    catch { }
                }
                else if (child is GroupItem gi)
                {
                    CollectGroup(gi, col);
                }
            }
        }

        static void EnumSets(SavedItemCollection col, List<object> result, string prefix = "")
        {
            foreach (var item in col)
            {
                var dn = prefix + item.DisplayName;
                if (item is GroupItem g)
                {
                    result.Add(new { nombre = dn, tipo = "Grupo" });
                    EnumSets(g.Children, result, dn + " / ");
                }
                else if (item is SelectionSet)
                {
                    result.Add(new { nombre = dn, tipo = "SelectionSet" });
                }
                else
                {
                    result.Add(new { nombre = dn, tipo = item.GetType().Name });
                }
            }
        }

        static SavedItem FindSet(SavedItemCollection col, string name)
        {
            foreach (var item in col)
            {
                if (string.Equals(item.DisplayName, name, StringComparison.OrdinalIgnoreCase)) return item;
                if (item is GroupItem g)
                {
                    var f = FindSet(g.Children, name);
                    if (f != null) return f;
                }
            }
            return null;
        }

        static void EnsureDoc()
        {
            if (Doc == null || Doc.Models.Count == 0)
                throw new Exception("No hay documento abierto en Navisworks.");
        }

        static string RemoveAccents(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char ch in norm)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Sin tipo";
            if (s.Contains(" : ")) s = s.Split(new[] { " : " }, StringSplitOptions.None).Last().Trim();
            return s.Trim();
        }

        static bool OIC(string s, string v)
        {
            if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(v)) return false;
            return s.IndexOf(v, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}