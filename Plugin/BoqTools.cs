// BoqTools.cs — v12 FINAL (archivo limpio, sin duplicados)
// Árbol confirmado por screenshots:
//   NWD > NWC > [NIVEL: LcRevitLayer] > [CAT: LcRevitCollection/Categoria]
//            > [FAMILIA: LcRevitCollection/Familia] > [TIPO: LcRevitCollection/Tipo + lcldrevit_tab_type]
//            > [INSTANCIA: LcRevitData_Element = tab "Componente"]
//
// Cantidades en instancia (ya en metros, NO convertir):
//   lcldrevit_parameter_-1012805 = Área (m²)
//   lcldrevit_parameter_-1012806 = Volumen (m³)
//   lcldrevit_parameter_-1004005 = Longitud (m)
//
// Lectura de valores: p.Value.ToDouble(null) → fallback p.Value.ToDisplayString() → fallback PV(ToString())

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Autodesk.Navisworks.Api;

namespace NavisBOQ.Plugin
{
    public class BoqRow
    {
        public string Nivel    { get; set; } = "Sin nivel";
        public string Categoria{ get; set; } = "";
        public string Familia  { get; set; } = "";
        public string Tipo     { get; set; } = "";
        public double Area     { get; set; }
        public double Volumen  { get; set; }
        public double Longitud { get; set; }
        public double Cantidad { get; set; }
        public string Unidad   { get; set; } = "pza";
        public string ElemId   { get; set; } = "";
    }

    public class BoqSummaryRow
    {
        public string Nivel    { get; set; } = "";
        public string Cat      { get; set; } = "";
        public string Familia  { get; set; } = "";
        public string Tipo     { get; set; } = "";
        public double Area     { get; set; }
        public double Vol      { get; set; }
        public double Long_    { get; set; }
        public double Cantidad { get; set; }
        public string Unidad   { get; set; } = "";
        public int    N        { get; set; }
    }

    public class ExtractResult
    {
        public int                 Total   { get; set; }
        public List<BoqSummaryRow> Resumen { get; set; } = new List<BoqSummaryRow>();
        public List<BoqRow>        Detalle { get; set; }
        public string              Nota    { get; set; } = "";
    }

    public static class CM
    {
        public static readonly Dictionary<string,(string N,string U)> Mapa =
            new Dictionary<string,(string,string)>(StringComparer.OrdinalIgnoreCase)
        {
            {"Muros",              ("Muros",       "m2")},{"Walls",              ("Muros",       "m2")},
            {"Suelos",             ("Losas",        "m2")},{"Floors",             ("Losas",        "m2")},
            {"Techos",             ("Plafones",    "m2")},{"Ceilings",           ("Plafones",    "m2")},
            {"Cubiertas",          ("Cubiertas",   "m2")},{"Roofs",              ("Cubiertas",   "m2")},
            {"Puertas",            ("Puertas",     "pza")},{"Doors",              ("Puertas",     "pza")},
            {"Ventanas",           ("Ventanas",    "pza")},{"Windows",            ("Ventanas",    "pza")},
            {"Escaleras",          ("Escaleras",   "pza")},{"Stairs",             ("Escaleras",   "pza")},
            {"Barandillas",        ("Barandales",  "ml")}, {"Railings",           ("Barandales",  "ml")},
            {"Structural Framing", ("Estructura",  "ml")}, {"Structural Columns", ("Columnas",    "pza")},
            {"Structural Foundations",("Cimentacion","m3")},
            {"Curtain Wall Panels",("Fachada",     "m2")}, {"Curtain Panels",     ("Fachada",     "m2")},
            {"Curtain Wall Mullions",("Montantes",  "ml")},
            {"Ducts",              ("Ductos",      "ml")}, {"Duct Fittings",      ("Conex Ducto", "pza")},
            {"Pipes",              ("Tuberias",    "ml")}, {"Pipe Fittings",      ("Conex Tubo",  "pza")},
            {"Plumbing Fixtures",  ("Sanitarios",  "pza")},{"Aparatos sanitarios",("Sanitarios",  "pza")},
            {"Mechanical Equipment",("Eq Mecanico","pza")},{"Air Terminals",      ("Difusores",   "pza")},
            {"Electrical Equipment",("Tableros",   "pza")},{"Lighting Fixtures",  ("Luminarias",  "pza")},
            {"Cable Trays",        ("Charolas",    "ml")}, {"Conduits",           ("Conduits",    "ml")},
            {"Generic Models",     ("Generico",    "pza")},{"Modelos genéricos",  ("Generico",    "pza")},
            {"Specialty Equipment",("Eq Especial", "pza")},{"Furniture",          ("Mobiliario",  "pza")},
            {"Mobiliario",         ("Mobiliario",  "pza")},{"Casework",           ("Carpinteria", "pza")},
        };

        public static readonly HashSet<string> EsPza = new HashSet<string>(StringComparer.OrdinalIgnoreCase){
            "Doors","Windows","Mechanical Equipment","Air Terminals","Plumbing Fixtures",
            "Electrical Equipment","Lighting Fixtures","Stairs","Generic Models",
            "Structural Columns","Duct Fittings","Pipe Fittings","Railings","Specialty Equipment",
            "Furniture","Casework","Puertas","Ventanas","Escaleras","Mobiliario","Carpinteria",
            "Aparatos sanitarios","Mobiliario","Eq Mecanico","Difusores","Sanitarios","Luminarias",
        };
    }

    public static class BoqTools
    {
        static Document Doc => Application.ActiveDocument;

        static void OnUI(Action a)
        {
            Exception ex = null;
            System.Windows.Forms.Control ctrl = null;
            try { var h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; if (h != IntPtr.Zero) ctrl = System.Windows.Forms.Control.FromHandle(h); } catch {}
            if (ctrl != null && ctrl.InvokeRequired) ctrl.Invoke(new System.Windows.Forms.MethodInvoker(()=>{ try{a();}catch(Exception e){ex=e;} }));
            else { try{a();}catch(Exception e){ex=e;} }
            if (ex != null) throw ex;
        }

        // ── PING ─────────────────────────────────────────────────────────────
        public static object Ping() =>
            new { pong=true, version="NavisBOQ v3", doc=Doc?.Title??"Sin doc" };

        // ── LIST SOURCE FILES ────────────────────────────────────────────────
        public static object ListSourceFiles()
        {
            EnsureDoc();
            return new {
                documento = Doc.Title,
                archivos  = Doc.Models.Select(m => new {
                    nombre    = m.RootItem.DisplayName,
                    ruta      = m.FileName ?? "n/a",
                    elementos = m.RootItem.DescendantsAndSelf.Count(i=>i.HasGeometry),
                    tipo      = Path.GetExtension(m.FileName??"").ToUpperInvariant().TrimStart('.')
                }).ToList()
            };
        }

        // ── DUMP PROPERTIES ──────────────────────────────────────────────────
        public static object DumpProperties(int maxItems = 3)
        {
            EnsureDoc();
            var result = new List<object>();
            int n = 0;
            foreach (var model in Doc.Models)
            {
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!item.HasGeometry) continue;
                    if (n++ >= maxItems) break;
                    result.Add(new {
                        geo   = item.DisplayName,
                        clase = item.ClassDisplayName,
                        arbol = item.AncestorsAndSelf.Reverse().Select(a => new {
                            display = a.DisplayName, clase = a.ClassDisplayName, geo = a.HasGeometry,
                            pcs = a.PropertyCategories.Where(pc=>pc.Properties.Any()).Select(pc => new {
                                pc_d=pc.DisplayName, pc_n=pc.Name,
                                props=pc.Properties.Select(p=>new{ d=p.DisplayName, n=p.Name, v=p.Value?.ToString()??"" }).ToList()
                            }).ToList()
                        }).ToList()
                    });
                }
                if (n >= maxItems) break;
            }
            return new { nota=$"{n} nodos con geometria.", items=result };
        }

        // ── DUMP GEO SET ─────────────────────────────────────────────────────
        public static object DumpGeoSet(string setName, int maxItems = 3)
        {
            EnsureDoc();
            var items = GetItemsFromSet(setName);
            var result = new List<object>();
            int n = 0;
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                var geos = item.HasGeometry ? new[]{item} : item.DescendantsAndSelf.Where(d=>d.HasGeometry).ToArray();
                foreach (var geo in geos)
                {
                    if (n >= maxItems) break;
                    if (!seen.Add(geo.InstanceGuid.ToString())) continue;
                    n++;
                    result.Add(new {
                        geo=geo.DisplayName, clase=geo.ClassDisplayName,
                        arbol=geo.AncestorsAndSelf.Reverse().Select(a=>new{
                            display=a.DisplayName, clase=a.ClassDisplayName, geo_a=a.HasGeometry,
                            pcs=a.PropertyCategories.Where(pc=>pc.Properties.Any()).Select(pc=>new{
                                pc_d=pc.DisplayName,pc_n=pc.Name,
                                props=pc.Properties.Select(p=>new{d=p.DisplayName,n=p.Name,v=p.Value?.ToString()??"" }).ToList()
                            }).ToList()
                        }).ToList()
                    });
                }
                if (n >= maxItems) break;
            }
            return new { set=setName, total_sel=items.Count, nota=$"{n} nodos geo.", items=result };
        }

        // ── DUMP INSTANCIA ────────────────────────────────────────────────────
        // Diagnóstico: recorre nodos buscando LcRevitData_Element directamente
        // (misma lógica que BuildRow). Límite 30,000 nodos. Sin filtro HasGeometry.
        public static object DumpInstancia(string categoria = "Walls", int maxItems = 3)
        {
            EnsureDoc();
            var result   = new List<object>();
            var seen     = new HashSet<string>();
            int n        = 0;
            int visited  = 0;
            int maxVisit = 30000;

            var propsKey = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
                "LcRevitPropertyElementName","LcRevitPropertyElementType",
                "LcRevitPropertyElementFamily","LcRevitPropertyElementCategory",
                "lcldrevit_parameter_-1012805","lcldrevit_parameter_-1012806",
                "lcldrevit_parameter_-1004005"
            };

            foreach (var model in Doc.Models)
            {
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (n >= maxItems || visited++ > maxVisit) break;

                    // Buscar LcRevitData_Element en este nodo directamente
                    var pc = item.PropertyCategories.FirstOrDefault(p =>
                        string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                    if (pc == null) continue;

                    // Filtrar por categoría
                    var catProp = pc.Properties.FirstOrDefault(p =>
                        string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));
                    if (catProp?.Value == null) continue;
                    var cat = SafeDisplay(catProp.Value) ?? "";
                    if (!string.IsNullOrEmpty(categoria) && !OIC(cat, categoria)) continue;

                    if (!seen.Add(item.InstanceGuid.ToString())) continue;
                    n++;

                    result.Add(new {
                        nodo    = item.DisplayName,
                        clase   = item.ClassDisplayName,
                        has_geo = item.HasGeometry,
                        cat     = cat,
                        nivel   = LevelFromTree(item) ?? "?",
                        params_clave = pc.Properties
                            .Where(p => propsKey.Contains(p.Name))
                            .Select(p => new {
                                display     = p.DisplayName,
                                name        = p.Name,
                                v_toString  = p.Value?.ToString()    ?? "NULL",
                                v_toDisplay = SafeDisplay(p.Value)    ?? "NULL",
                                v_toDouble  = SafeDouble(p.Value),
                                v_pv        = PV(SafeDisplay(p.Value) ?? "")
                            }).ToList()
                    });
                }
                if (n >= maxItems || visited > maxVisit) break;
            }

            return new {
                categoria, n, visitados=visited,
                nota="Compara v_toDisplay vs v_pv para -1012805(Area) y -1012806(Vol). Si v_pv=0 hay bug en PV().",
                items=result
            };
        }

        // ── GET MODEL SUMMARY ────────────────────────────────────────────────
        public static object GetModelSummary()
        {
            EnsureDoc();
            int totalGeo=0;
            var cats   = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
            var niveles= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int limit  = 15000;
            foreach (var model in Doc.Models)
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!item.HasGeometry) continue;
                    totalGeo++;
                    if (totalGeo > limit) continue;
                    var cat = CatFromTree(item);
                    if (!string.IsNullOrEmpty(cat)) { if (!cats.ContainsKey(cat)) cats[cat]=0; cats[cat]++; }
                    var niv = LevelFromTree(item);
                    if (!string.IsNullOrEmpty(niv)) niveles.Add(niv);
                }
            return new {
                archivo=Doc.Title, total_geo=totalGeo,
                muestra=totalGeo>limit?$"{limit}/{totalGeo}":"completo",
                categorias=cats.Select(kv=>{ var m=CM.Mapa.FirstOrDefault(x=>OIC(kv.Key,x.Key)); return new{cat=kv.Key,boq=m.Key!=null?m.Value.N:kv.Key,n=kv.Value}; }).OrderByDescending(x=>x.n).ToList(),
                niveles=niveles.OrderBy(x=>x).ToList()
            };
        }

        // ── GET PARAMETERS ───────────────────────────────────────────────────
        public static object GetParametersForCategory(string category)
        {
            EnsureDoc();
            var result = new List<object>(); int n = 0;
            foreach (var model in Doc.Models)
            {
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!item.HasGeometry) continue;
                    if (n >= 2) break;
                    var cat = CatFromTree(item);
                    if (!string.IsNullOrEmpty(category) && !OIC(cat??"", category)) continue;
                    n++;
                    result.Add(new {
                        cat=cat, nivel=LevelFromTree(item), fam=FamFromTree(item),
                        arbol=item.AncestorsAndSelf.Reverse().Select(a=>new{
                            d=a.DisplayName,cls=a.ClassDisplayName,
                            pcs=a.PropertyCategories.Where(pc=>pc.Properties.Any()).Select(pc=>new{
                                pc_d=pc.DisplayName,pc_n=pc.Name,
                                props=pc.Properties.Select(p=>new{d=p.DisplayName,n=p.Name,v=p.Value?.ToString()??"" }).ToList()
                            }).ToList()
                        }).ToList()
                    });
                }
                if (n >= 2) break;
            }
            return new { buscado=category, n, nota=n==0?"No encontrado.":"OK", items=result };
        }

        // ── LIST SELECTION SETS ──────────────────────────────────────────────
        public static object ListSelectionSets()
        {
            EnsureDoc();
            var sets = new List<object>();
            EnumSets(Doc.SelectionSets.Value, sets);
            return new { sets, total=sets.Count };
        }

        // ── EXTRACT FROM SELECTION SET ───────────────────────────────────────
        public static object ExtractFromSelectionSet(string setName)
        {
            EnsureDoc();
            return Extract(GetItemsFromSet(setName), setName);
        }

        // ── EXTRACT FROM CURRENT SELECTION ──────────────────────────────────
        public static object ExtractFromCurrentSelection()
        {
            EnsureDoc();
            var items = new List<ModelItem>();
            OnUI(() => { foreach (var mi in Application.ActiveDocument.CurrentSelection.SelectedItems) items.Add(mi); });
            if (items.Count == 0) return new { error="Sin seleccion activa.", n=0 };
            return Extract(items, "seleccion_manual");
        }

        // ── EXTRACT QUANTITIES ───────────────────────────────────────────────
        public static ExtractResult ExtractQuantities(List<string> cats, string nivel=null, bool summaryOnly=true)
        {
            EnsureDoc();
            var rows = new List<BoqRow>(); var seen = new HashSet<string>(); int count=0;
            foreach (var model in Doc.Models)
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!item.HasGeometry) continue;
                    if (count++ > 10000) break;
                    if (!seen.Add(item.InstanceGuid.ToString())) continue;
                    var row = BuildRow(item);
                    if (row == null) continue;
                    if (cats?.Count>0 && !cats.Any(f=>OIC(row.Categoria,f))) continue;
                    if (!string.IsNullOrEmpty(nivel) && !OIC(row.Nivel,nivel)) continue;
                    rows.Add(row);
                }
            return new ExtractResult { Total=rows.Count, Resumen=Summ(rows), Detalle=summaryOnly?null:rows,
                Nota=rows.Count==0?"0 elementos. Usa dump_geo_set para diagnosticar.":"" };
        }

        // ── RUN PRECONSTRUCCION 1 ────────────────────────────────────────────
        public static object RunPreConstruccion1()
        {
            var cats = new List<string>{"Muros","Walls","Losas","Floors","Cubiertas","Roofs",
                "Plafones","Ceilings","Puertas","Doors","Ventanas","Windows","Fachada","Curtain Wall Panels"};
            return FmtDesglose("Preconstruccion 1 - Arquitectura", ExtractQuantities(cats, summaryOnly:false));
        }

        // ── HIGHLIGHT / CLEAR ────────────────────────────────────────────────
        public static object HighlightByCategory(List<string> cats, string nivel=null)
        {
            EnsureDoc();
            var items = new List<ModelItem>(); var seen = new HashSet<string>(); int count=0;
            foreach (var model in Doc.Models)
                foreach (var item in model.RootItem.DescendantsAndSelf)
                {
                    if (!item.HasGeometry) continue;
                    if (count++ > 10000) break;
                    if (!seen.Add(item.InstanceGuid.ToString())) continue;
                    var cat=CatFromTree(item)??""; var m=CM.Mapa.FirstOrDefault(x=>OIC(cat,x.Key)); var nb=m.Key!=null?m.Value.N:cat;
                    if (cats?.Count>0 && !cats.Any(f=>OIC(nb,f)||OIC(cat,f))) continue;
                    if (!string.IsNullOrEmpty(nivel) && !OIC(LevelFromTree(item)??"",nivel)) continue;
                    items.Add(item);
                }
            var col = new ModelItemCollection(); foreach (var mi in items) col.Add(mi);
            OnUI(()=>{ Application.ActiveDocument.CurrentSelection.Clear(); Application.ActiveDocument.CurrentSelection.CopyFrom(col); });
            return new { resaltados=items.Count };
        }

        public static object ClearSelection()
        { EnsureDoc(); OnUI(()=>Application.ActiveDocument.CurrentSelection.Clear()); return new{ok=true}; }

        // ── EXPORT JSON ──────────────────────────────────────────────────────
        public static object ExportJson(string path=null)
        {
            EnsureDoc();
            var data=ExtractQuantities(new List<string>(), summaryOnly:false);
            var desktop=Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            path=!string.IsNullOrEmpty(path)?path:Path.Combine(desktop,"BOQ_"+Path.GetFileNameWithoutExtension(Doc.Title??"m")+"_"+DateTime.Now.ToString("yyyyMMdd_HHmm")+".json");
            File.WriteAllText(path,JsonConvert.SerializeObject(new{
                metadata=new{archivo=Doc.Title,generado=DateTime.Now.ToString("s"),total=data.Total,unidades="m/m2/m3"},
                resumen=data.Resumen,detalle=data.Detalle
            },Formatting.Indented),Encoding.UTF8);
            return new{ok=true,archivo=path};
        }

        // ====================================================================
        // CORE: BuildRow
        // Busca el nodo INSTANCIA = primer ancestro con LcRevitData_Element
        // que tenga LcRevitPropertyElementCategory con valor.
        // Lee cantidades con ToDouble() > ToDisplayString() > ToString().
        // ====================================================================
        static BoqRow BuildRow(ModelItem geo)
        {
            // 1. Encontrar el nodo instancia real
            PropertyCategory elemPC  = null;
            ModelItem        instNode = null;

            foreach (var a in geo.AncestorsAndSelf)
            {
                var pc = a.PropertyCategories.FirstOrDefault(p =>
                    string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                if (pc == null) continue;

                // Verificar que tiene LcRevitPropertyElementCategory con valor
                var catProp = pc.Properties.FirstOrDefault(p =>
                    string.Equals(p.Name, "LcRevitPropertyElementCategory", StringComparison.OrdinalIgnoreCase));
                if (catProp?.Value == null) continue;
                var catVal = SafeDisplay(catProp.Value);
                if (string.IsNullOrWhiteSpace(catVal)) continue;

                instNode = a;
                elemPC   = pc;
                break;
            }

            if (instNode == null || elemPC == null) return null;

            // 2. Metadatos
            var catRevit = SafeDisplay(elemPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementCategory")?.Value) ?? CatFromTree(geo);
            if (string.IsNullOrEmpty(catRevit)) return null;

            string boq=catRevit, unit="";
            var m=CM.Mapa.FirstOrDefault(x=>OIC(catRevit,x.Key));
            if (m.Key!=null){boq=m.Value.N;unit=m.Value.U;}

            var familia = SafeDisplay(elemPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementFamily")?.Value) ?? FamFromTree(geo) ?? "Sin familia";
            var tipo    = SafeDisplay(elemPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementType")?.Value)
                       ?? SafeDisplay(elemPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementName")?.Value)
                       ?? TipoFromTree(geo) ?? familia;
            var nivel   = LevelFromTree(geo) ?? "Sin nivel";
            var elemId  = SafeDisplay(elemPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementId")?.Value) ?? "";

            // 3. Cantidades — buscar en TODOS los PCs Revit de la instancia y sus ancestros cercanos
            double area=0, vol=0, len=0;
            ReadQuantities(instNode, ref area, ref vol, ref len);

            // Si no encontró, intentar en el nodo padre de la instancia (para nested families)
            if ((area==0 || vol==0) && instNode.Parent != null)
            {
                var parentPC = instNode.Parent.PropertyCategories.FirstOrDefault(p =>
                    string.Equals(p.Name, "LcRevitData_Element", StringComparison.OrdinalIgnoreCase));
                if (parentPC != null) ReadQuantitiesFromPC(parentPC, ref area, ref vol, ref len);
            }

            // 4. Cantidad y unidad
            double qty; string u;
            if (unit=="pza"||CM.EsPza.Contains(catRevit)){qty=1;u="pza";}
            else if (unit=="m2"&&area>0){qty=Math.Round(area,3);u="m2";}
            else if (unit=="m3"&&vol >0){qty=Math.Round(vol,3); u="m3";}
            else if (unit=="ml"&&len >0){qty=Math.Round(len,3); u="ml";}
            else if (area>0){qty=Math.Round(area,3);u="m2";}
            else if (vol >0){qty=Math.Round(vol,3); u="m3";}
            else if (len >0){qty=Math.Round(len,3); u="ml";}
            else           {qty=1;u="pza";}

            return new BoqRow{
                Nivel=nivel,Categoria=boq,Familia=Clean(familia),Tipo=Clean(tipo),
                Area=Math.Round(area,4),Volumen=Math.Round(vol,4),Longitud=Math.Round(len,4),
                Cantidad=qty,Unidad=u,ElemId=elemId
            };
        }

        // ── LECTORES DE CANTIDADES ────────────────────────────────────────────
        // Estrategia GPC: iterar PropertyCategories del nodo DIRECTAMENTE,
        // sin subir a ancestros. Leer con ToDisplayString() y parsear con SplitNum().
        static void ReadQuantities(ModelItem node, ref double area, ref double vol, ref double len)
        {
            // Solo iterar PropertyCategories del nodo instancia — NO AncestorsAndSelf
            foreach (var pc in node.PropertyCategories)
                ReadQuantitiesFromPC(pc, ref area, ref vol, ref len);

            // Si no encontró nada, intentar en el nodo padre (para casos de geometry leaf)
            if (area == 0 && vol == 0 && len == 0 && node.Parent != null)
                foreach (var pc in node.Parent.PropertyCategories)
                    ReadQuantitiesFromPC(pc, ref area, ref vol, ref len);
        }

        static void ReadQuantitiesFromPC(PropertyCategory pc, ref double area, ref double vol, ref double len)
        {
            foreach (var p in pc.Properties)
            {
                if (p.Value == null) continue;
                var pn = p.Name ?? "";
                var dn = RemoveAccents(p.DisplayName ?? "");

                // Identificar por InternalName (más confiable que DisplayName)
                bool isArea = string.Equals(pn, "lcldrevit_parameter_-1012805", StringComparison.OrdinalIgnoreCase);
                bool isVol  = string.Equals(pn, "lcldrevit_parameter_-1012806", StringComparison.OrdinalIgnoreCase);
                bool isLen  = string.Equals(pn, "lcldrevit_parameter_-1004005", StringComparison.OrdinalIgnoreCase);

                // Fallback por DisplayName si InternalName no matchea
                if (!isArea && !isVol && !isLen)
                {
                    isArea = OIC(dn,"Area")    && !OIC(dn,"Category") && !OIC(dn,"Number") && !OIC(dn,"Base") && !OIC(dn,"Room") && !OIC(dn,"IFC");
                    isVol  = OIC(dn,"Volume")  || OIC(dn,"Volumen");
                    isLen  = (OIC(dn,"Length") || OIC(dn,"Longitud")) && !OIC(dn,"Number") && !OIC(dn,"Wall") && !OIC(dn,"Curve") && !OIC(dn,"Unconnected");
                }

                if (!isArea && !isVol && !isLen) continue;

                // GPC Pipeline: usar ToDisplayString() y extraer número con SplitNum()
                // ToDisplayString() → "3225,846 m²" → SplitNum() → 3225.846
                // ToString()        → "Unknown"      → NO USAR
                var ds = "";
                try { ds = p.Value.ToDisplayString() ?? ""; } catch {}
                double v = SplitNum(ds);

                if (isArea && v > area) area = v;
                if (isVol  && v > vol)  vol  = v;
                if (isLen  && v > len)  len  = v;
            }
        }

        // Extrae el número de un string con unidades: "3225,846 m²" → 3225.846
        // Basado en la lógica de NavisHeuristics.SplitNumberAndUnit() del plugin GPC.
        static double SplitNum(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim().Replace(' ', ' ');

            // Quitar prefijo tipo "DoubleLength:" o "DisplayString:"
            int col = s.IndexOf(':');
            if (col >= 0 && col < 20 && !s.Substring(0,col).Any(char.IsDigit))
                s = s.Substring(col+1).Trim();

            // Extraer el primer token numérico (antes de la unidad)
            // Regex equivalente: [-+]?d+[.,]?d*
            var sb = new System.Text.StringBuilder();
            bool hasDig = false, hasDec = false;
            foreach (char ch in s)
            {
                if (char.IsDigit(ch))      { sb.Append(ch); hasDig = true; }
                else if (ch == '-' && !hasDig) sb.Append(ch);
                else if ((ch == ',' || ch == '.') && hasDig && !hasDec) { sb.Append(ch); hasDec = true; }
                else if (hasDig)           break; // primera no-cifra después de dígitos
            }
            if (sb.Length == 0) return 0;

            var token = sb.ToString();
            // Normalizar separador decimal: si tiene coma pero no punto → coma es decimal europeo
            if (token.Contains(',') && !token.Contains('.'))
                token = token.Replace(',', '.');

            return double.TryParse(token, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : 0;
        }

        // ── LECTORES DE ÁRBOL ─────────────────────────────────────────────────

        static string CatFromTree(ModelItem item)
        {
            foreach (var a in item.AncestorsAndSelf)
            {
                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name,"LcOaNode",StringComparison.OrdinalIgnoreCase)) continue;
                    bool isCol=false, isCat=false;
                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name,"LcOaSceneBaseClassName") && (p.Value?.ToString()??"").Contains("LcRevitCollection")) isCol=true;
                        if (string.Equals(p.Name,"LcOaSceneBaseClassUserName")) { var v=p.Value?.ToString()??""; if(OIC(v,"Categoria")||OIC(v,"Category")) isCat=true; }
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
                bool isLayer=false; string layerName=null;
                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name,"LcOaNode",StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var p in pc.Properties)
                    {
                        if (string.Equals(p.Name,"LcOaSceneBaseClassName") && (p.Value?.ToString()??"").Contains("LcRevitLayer")) isLayer=true;
                        if (string.Equals(p.Name,"LcOaNodeLayer")) layerName=p.Value?.ToString()??"";
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
                if (a.PropertyCategories.Any(pc=>string.Equals(pc.Name,"LcRevitData_Element",StringComparison.OrdinalIgnoreCase))) continue;
                foreach (var pc in a.PropertyCategories)
                {
                    if (!string.Equals(pc.Name,"LcOaNode",StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var p in pc.Properties)
                        if (string.Equals(p.Name,"LcOaSceneBaseClassUserName") && OIC(p.Value?.ToString()??"","Familia"))
                            return a.DisplayName;
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
                    if (string.Equals(pc.Name,"lcldrevit_tab_type",StringComparison.OrdinalIgnoreCase)) return a.DisplayName;
                    if (!string.Equals(pc.Name,"LcOaNode",StringComparison.OrdinalIgnoreCase)) continue;
                    foreach (var p in pc.Properties)
                        if (string.Equals(p.Name,"LcOaSceneBaseClassUserName") && OIC(p.Value?.ToString()??"","Tipo"))
                            return a.DisplayName;
                }
            }
            return null;
        }

        // ── EXTRACT / FORMAT ──────────────────────────────────────────────────
        static object Extract(List<ModelItem> items, string fuente)
        {
            var seen=new HashSet<string>(); var rows=new List<BoqRow>();
            foreach (var item in items)
            {
                // Caso 1: nodo con LcRevitData_Element con categoría (= instancia directa)
                var directPC = item.PropertyCategories.FirstOrDefault(p=>string.Equals(p.Name,"LcRevitData_Element",StringComparison.OrdinalIgnoreCase));
                if (directPC != null)
                {
                    var catDirect = directPC.Properties.FirstOrDefault(p=>p.Name=="LcRevitPropertyElementCategory");
                    if (catDirect?.Value != null && !string.IsNullOrWhiteSpace(SafeDisplay(catDirect.Value)))
                    {
                        if (seen.Add(item.InstanceGuid.ToString())) { var r=BuildRow(item); if(r!=null) rows.Add(r); }
                        continue;
                    }
                }
                // Caso 2: nodo geo
                if (item.HasGeometry) { if(seen.Add(item.InstanceGuid.ToString())){var r=BuildRow(item);if(r!=null)rows.Add(r);} continue; }
                // Caso 3: nodo padre — expandir descendientes
                foreach (var desc in item.DescendantsAndSelf)
                {
                    if (!desc.HasGeometry) continue;
                    if (!seen.Add(desc.InstanceGuid.ToString())) continue;
                    var r=BuildRow(desc); if(r!=null) rows.Add(r);
                }
            }
            var desglose=rows.GroupBy(r=>r.Categoria).OrderBy(g=>g.Key).Select(gC=>(object)new{
                categoria=gC.Key,piezas=gC.Count(),
                area_m2=Math.Round(gC.Sum(r=>r.Area),3),vol_m3=Math.Round(gC.Sum(r=>r.Volumen),3),ml=Math.Round(gC.Sum(r=>r.Longitud),3),
                niveles=gC.GroupBy(r=>r.Nivel).OrderBy(g=>g.Key).Select(gN=>new{
                    nivel=gN.Key,piezas=gN.Count(),area=Math.Round(gN.Sum(r=>r.Area),3),vol=Math.Round(gN.Sum(r=>r.Volumen),3),ml=Math.Round(gN.Sum(r=>r.Longitud),3),
                    familias=gN.GroupBy(r=>r.Familia).Select(gF=>new{fam=gF.Key,
                        tipos=gF.GroupBy(r=>r.Tipo).OrderBy(g=>g.Key).Select(gT=>new{tipo=gT.Key,piezas=gT.Count(),area=Math.Round(gT.Sum(r=>r.Area),3),vol=Math.Round(gT.Sum(r=>r.Volumen),3),ml=Math.Round(gT.Sum(r=>r.Longitud),3)}).ToList()
                    }).ToList()
                }).ToList()
            }).ToList();
            return new{fuente,sel=items.Count,geo=rows.Count,resumen=Summ(rows),desglose,nota=rows.Count==0?"0 BOQ. Usa dump_geo_set.":"OK"};
        }

        static object FmtDesglose(string rutina, ExtractResult res)
        {
            var d=(res.Detalle??new List<BoqRow>()).GroupBy(r=>r.Categoria).OrderBy(g=>g.Key).Select(gC=>(object)new{
                cat=gC.Key,piezas=gC.Count(),area_m2=Math.Round(gC.Sum(r=>r.Area),2),vol_m3=Math.Round(gC.Sum(r=>r.Volumen),2),ml=Math.Round(gC.Sum(r=>r.Longitud),2),
                niveles=gC.GroupBy(r=>r.Nivel).OrderBy(g=>g.Key).Select(gN=>new{nivel=gN.Key,piezas=gN.Count(),area=Math.Round(gN.Sum(r=>r.Area),2),vol=Math.Round(gN.Sum(r=>r.Volumen),2),ml=Math.Round(gN.Sum(r=>r.Longitud),2),
                    fams=gN.GroupBy(r=>r.Familia).Select(gF=>new{fam=gF.Key,tipos=gF.GroupBy(r=>r.Tipo).OrderBy(g=>g.Key).Select(gT=>new{tipo=gT.Key,piezas=gT.Count(),area=Math.Round(gT.Sum(r=>r.Area),2),vol=Math.Round(gT.Sum(r=>r.Volumen),2),ml=Math.Round(gT.Sum(r=>r.Longitud),2)}).ToList()}).ToList()
                }).ToList()
            }).ToList();
            return new{rutina,total=res.Total,resumen=res.Resumen,desglose=d,nota=res.Nota};
        }

        static List<BoqSummaryRow> Summ(List<BoqRow> rows) =>
            rows.GroupBy(r=>new{r.Nivel,r.Categoria,r.Familia,r.Tipo,r.Unidad})
                .Select(g=>new BoqSummaryRow{Nivel=g.Key.Nivel,Cat=g.Key.Categoria,Familia=g.Key.Familia,Tipo=g.Key.Tipo,
                    Area=Math.Round(g.Sum(r=>r.Area),2),Vol=Math.Round(g.Sum(r=>r.Volumen),2),Long_=Math.Round(g.Sum(r=>r.Longitud),2),
                    Cantidad=Math.Round(g.Sum(r=>r.Cantidad),2),Unidad=g.Key.Unidad,N=g.Count()})
                .OrderBy(r=>r.Cat).ThenBy(r=>r.Nivel).ThenBy(r=>r.Tipo).ToList();

        // ── HELPERS ───────────────────────────────────────────────────────────

        // Leer string de VariantData usando ToDisplayString()
        static string SafeDisplay(VariantData v)
        {
            if (v == null) return null;
            try { var s=v.ToDisplayString(); return string.IsNullOrWhiteSpace(s)?null:s.Trim(); } catch { return null; }
        }

        // Leer double de VariantData.
        // ToDisplayString() es el método correcto (confirmado por GPC ExportPropertiesPlugin).
        // Para DoubleLength devuelve "3225,846 m²" que PV() parsea a 3225.846.
        // ToString() devuelve "Unknown" para DoubleLength — NO usar.
        static double SafeDouble(VariantData v)
        {
            if (v == null) return 0;
            try { var s = v.ToDisplayString() ?? ""; if (!string.IsNullOrEmpty(s)) { var d = PV(s); if (d > 0) return d; } } catch {}
            try { return v.ToDouble(); } catch {}
            return 0;
        }

        // Parse string a double — lógica robusta basada en GPC NavisHeuristics.SplitNumberAndUnit
        // Maneja: "DoubleLength:3225,846 m²", "3 225,846 m²", "3.225,846 m²", "13.615 m", etc.
        // Estrategia: quitar prefijo tipo → buscar primer número con unidad via regex
        //             → si no, buscar primer número libre → normalizar separadores
        static readonly System.Text.RegularExpressions.Regex _reNumUnit =
            new System.Text.RegularExpressions.Regex(
                @"(?<![A-Za-z0-9_])(?<num>[-+]?\d[\d\s]*(?:[.,]\d+)?(?:[eE][-+]?\d+)?)\s*(?<unit>%|°[CF]?|mm|cm|m|km|in|ft|yd|kg|g|lb|N|kN|Pa|MPa|psi|L|s|ms|rad|deg|°|m²|m³|mm²|mm³|ft²|ft³|m2|m3|mm2|mm3)(?:|$)",
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant | System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);

        static readonly System.Text.RegularExpressions.Regex _reFirstNum =
            new System.Text.RegularExpressions.Regex(
                @"(?<![A-Za-z0-9_])[-+]?\d+(?:[.,]\d+)?(?:[eE][-+]?\d+)?",
                System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        static double PV(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return 0;

            // 1. Quitar prefijo tipo: "DoubleLength:", "Double:", "Int32:", etc.
            int colon = raw.IndexOf(':');
            if (colon > 0 && colon < 20) // prefijos son cortos
            {
                var prefix = raw.Substring(0, colon);
                if (!prefix.Any(char.IsDigit)) raw = raw.Substring(colon + 1).Trim();
            }
            raw = raw.Replace(' ', ' ').Replace("−", "-").Trim(); // NBSP + minus sign

            // 2. Buscar número + unidad (regex GPC): "3225,846 m²" → token="3225,846", unit="m²"
            var mUnit = _reNumUnit.Match(raw);
            if (mUnit.Success)
            {
                var token = mUnit.Groups["num"].Value.Replace(" ", ""); // quitar espacios de miles
                return NormalizeAndParse(token);
            }

            // 3. Fallback: buscar cualquier número (sin unidad): "13.615", "4.813"
            var mNum = _reFirstNum.Match(raw);
            if (mNum.Success)
            {
                // Verificar que no haya letra pegada después (sería texto, no número)
                int end = mNum.Index + mNum.Length;
                if (end < raw.Length && char.IsLetter(raw[end])) return 0;
                return NormalizeAndParse(mNum.Value);
            }

            return 0;
        }

        // Normalizar separadores decimales y parsear
        // Lógica de GPC: detectar cuál es el separador decimal por posición
        static double NormalizeAndParse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            string t = token.Trim();

            if (t.Contains(',') && t.Contains('.'))
            {
                // Ambos: el último es decimal, el otro es miles
                int lastDot   = t.LastIndexOf('.');
                int lastComma = t.LastIndexOf(',');
                t = (lastComma > lastDot)
                    ? t.Replace(".", "").Replace(',', '.')   // "3.225,846" → "3225.846"
                    : t.Replace(",", "");                     // "3,225.846" → "3225.846"
            }
            else if (t.Contains(','))
            {
                t = t.Replace(',', '.');  // "3225,846" → "3225.846"
            }
            // Si solo tiene punto: ya está bien "3225.846"

            return double.TryParse(t,
                System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : 0;
        }

        static string RemoveAccents(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm=s.Normalize(System.Text.NormalizationForm.FormD);
            var sb=new StringBuilder();
            foreach (char ch in norm)
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)!=System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "Sin tipo";
            if (s.Contains(" : ")) s=s.Split(new[]{" : "},StringSplitOptions.None).Last().Trim();
            return s.Trim();
        }

        static bool OIC(string s, string v)
        {
            if (string.IsNullOrEmpty(s)||string.IsNullOrEmpty(v)) return false;
            return s.IndexOf(v,StringComparison.OrdinalIgnoreCase)>=0;
        }

        // GetItemsFromSet — API correcta, todo en UI thread
        static List<ModelItem> GetItemsFromSet(string setName)
        {
            var found=FindSet(Doc.SelectionSets.Value,setName);
            if (found==null) throw new Exception("Selection Set '"+setName+"' no encontrado.");
            var items=new List<ModelItem>();
            OnUI(()=>{
                var src=Application.ActiveDocument.SelectionSets.CreateSelectionSource(found);
                var col=src.TryGetSelectedItems(Application.ActiveDocument);
                if (col!=null&&col.Count>0)
                {
                    Application.ActiveDocument.CurrentSelection.Clear();
                    Application.ActiveDocument.CurrentSelection.CopyFrom(col);
                    foreach (var mi in Application.ActiveDocument.CurrentSelection.SelectedItems) items.Add(mi);
                }
                else if (found is SelectionSet ss)
                {
                    try { var si=ss.GetSelectedItems(Application.ActiveDocument); if(si!=null){Application.ActiveDocument.CurrentSelection.Clear();Application.ActiveDocument.CurrentSelection.AddRange(si);foreach(var mi in Application.ActiveDocument.CurrentSelection.SelectedItems)items.Add(mi);} } catch {}
                }
                else if (found is GroupItem gi)
                {
                    var gcol=new ModelItemCollection(); CollectGroup(gi,gcol);
                    if(gcol.Count>0){Application.ActiveDocument.CurrentSelection.Clear();Application.ActiveDocument.CurrentSelection.CopyFrom(gcol);foreach(var mi in Application.ActiveDocument.CurrentSelection.SelectedItems)items.Add(mi);}
                }
            });
            return items;
        }

        static void CollectGroup(GroupItem group, ModelItemCollection col)
        {
            foreach (var child in group.Children)
            {
                if (child is SelectionSet ss){try{var it=ss.GetSelectedItems(Application.ActiveDocument);if(it!=null)foreach(var mi in it)col.Add(mi);}catch{}}
                else if (child is GroupItem gi) CollectGroup(gi,col);
            }
        }

        static void EnumSets(SavedItemCollection col, List<object> result, string prefix="")
        {
            foreach (var item in col)
            {
                var dn=prefix+item.DisplayName;
                if (item is GroupItem g){result.Add(new{nombre=dn,tipo="Grupo"});EnumSets(g.Children,result,dn+" / ");}
                else if (item is SelectionSet) result.Add(new{nombre=dn,tipo="SelectionSet"});
                else result.Add(new{nombre=dn,tipo=item.GetType().Name});
            }
        }

        static SavedItem FindSet(SavedItemCollection col, string name)
        {
            foreach (var item in col)
            {
                if (string.Equals(item.DisplayName,name,StringComparison.OrdinalIgnoreCase)) return item;
                if (item is GroupItem g){var f=FindSet(g.Children,name);if(f!=null)return f;}
            }
            return null;
        }

        static void EnsureDoc()
        {
            if (Doc==null||Doc.Models.Count==0) throw new Exception("No hay documento abierto en Navisworks.");
        }
    }
}
