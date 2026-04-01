using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class PropertyReaderService : IPropertyReaderService
    {
        private const string CAT_REVIT_ELEMENT = "LcRevitData_Element";
        private const string CAT_REVIT_TYPE = "lcldrevit_tab_type";
        private const string CAT_REVIT_CUSTOM = "LcRevitData_Custom";
        private const string CAT_REVIT_FAMILY = "lcldrevit_tab_family";

        private const string PROP_ELEMENT_ID = "LcRevitPropertyElementId";
        private const string PROP_CATEGORY = "LcRevitPropertyElementCategory";
        private const string PROP_CATEGORY_ID = "LcRevitPropertyElementCategoryId";
        private const string PROP_FAMILY = "LcRevitPropertyElementFamily";
        private const string PROP_TYPE = "LcRevitPropertyElementType";
        private const string PROP_MARK = "lcldrevit_parameter_-1001203";
        private const string PROP_VDC_WEIGHT = "VDC_WEIGHT";

        private const string PROP_AREA = "lcldrevit_parameter_-1012805";
        private const string PROP_VOL = "lcldrevit_parameter_-1012806";
        private const string PROP_LEN_DEFAULT = "lcldrevit_parameter_-1004005";
        private const string PROP_SIZE = "lcldrevit_parameter_-1114240";
        private const string PROP_FAMILY_TYPE_NAME = "LcOaSceneBaseUserName";
        private const string PROP_OMNICLASS_TITLE = "lcldrevit_parameter_-1002503";
        private const string PROP_PIECE_TYPE = "lcldrevit_parameter_-1114206";
        private const string PROP_LEN_STRUCT = "lcldrevit_parameter_-1001375";
        private const string PROP_CUT_LENGTH = "lcldrevit_parameter_-1001384";

        private const string PROP_TYPE_DESC = "lcldrevit_parameter_-1010103";
        private const string PROP_STRUCT_MAT = "lcldrevit_parameter_-1005500";
        private const string PROP_TYPE_WIDTH = "lcldrevit_parameter_-1001000";
        private const string PROP_TYPE_NODE_NAME = "LcRevitPropertyElementName";
        private const string PROP_TYPE_CATEGORY = "LcRevitPropertyElementCategory";
        private const string PROP_DIM_A = "lcldrevit_parameter_A_PG_DIMENSIONS";
        private const string PROP_DIM_B = "lcldrevit_parameter_B_PG_DIMENSIONS";

        private const string PROP_LINEAR_WEIGHT = "lcldrevit_parameter_Linear_Weight";
        private const string PROP_DEPTH = "lcldrevit_parameter_Depth";
        private const string PROP_WIDTH_X = "lcldrevit_parameter_Width_x";
        private const string PROP_ELECTRICAL_DATA = "lcldrevit_parameter_-1114241";
        private const string PROP_PANEL_NAME = "lcldrevit_parameter_-1140078";
        private const string PROP_MAIN_BREAKER_POWER = "lcldrevit_parameter_-1140140";
        private const string PROP_PANEL_INSTANCE = "lcldrevit_parameter_-1140104";
        private const string PROP_LOAD_CLASSIFICATION = "lcldrevit_parameter_Clasificaci?n de carga_PG_ELECTRICAL";
        private const string PROP_KEYNOTE_NOTE = "lcldrevit_parameter_-1140422";
        private const string PROP_TYPE_COMMENTS = "lcldrevit_parameter_-1010105";
        private const string PROP_URL = "lcldrevit_parameter_-1010104";
        private const string PROP_CUSTOM_PARTIDA = "lcldrevit_parameter_TR3Z - Partida_PG_TEXT";

        private static readonly string[] PROP_TYPE_THICKNESS =
        {
            "lcldrevit_parameter_-1001902",
            "lcldrevit_parameter_-1002206",
            "lcldrevit_parameter_-1001600",
            "lcldrevit_parameter_-1001006"
        };

        private const string PROP_NOMINAL_WEIGHT = "lcldrevit_parameter_-1005512";
        private const string PROP_SECTION_NAME = "lcldrevit_parameter_-1005554";
        private const string PROP_SECTION_SHAPE = "lcldrevit_parameter_-1005501";
        private const string PROP_CODE_NAME = "lcldrevit_parameter_-1005556";

        private readonly IModelStructurePolicyService _structurePolicy;

        public PropertyReaderService(IModelStructurePolicyService structurePolicy)
        {
            _structurePolicy = structurePolicy ?? throw new ArgumentNullException(nameof(structurePolicy));
        }

        public IEnumerable<ModelItem> EnumerateElementCandidates(ModelItem item)
        {
            if (item == null) yield break;

            foreach (var a in item.AncestorsAndSelf)
            {
                if (a == null) continue;
                if (!HasRevitElementData(a)) continue;
                yield return a;
            }
        }

        public bool HasRevitElementData(ModelItem item)
        {
            if (item == null) return false;

            try
            {
                var pc = item.PropertyCategories
                    .FirstOrDefault(p => string.Equals(p.Name, CAT_REVIT_ELEMENT, StringComparison.OrdinalIgnoreCase));

                if (pc == null) return false;

                var catProp = pc.Properties.FirstOrDefault(p =>
                    string.Equals(p.Name, PROP_CATEGORY, StringComparison.OrdinalIgnoreCase));

                return catProp?.Value != null && !string.IsNullOrWhiteSpace(ReadVariantAsString(catProp.Value));
            }
            catch
            {
                return false;
            }
        }

        public bool HasTypeData(ModelItem item)
        {
            if (item == null) return false;

            try
            {
                return item.PropertyCategories.Any(p =>
                    string.Equals(p.Name, CAT_REVIT_TYPE, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public ModelItem ResolveInstanceNode(ModelItem item)
        {
            if (item == null) return null;

            var candidates = new List<(ModelItem node, int score, int depth)>();
            int depth = 0;

            foreach (var c in EnumerateElementCandidates(item))
            {
                int score = _structurePolicy.ScoreElementCandidate(c);

                var elementId = ReadString(c, CAT_REVIT_ELEMENT, PROP_ELEMENT_ID);
                var family = ReadString(c, CAT_REVIT_ELEMENT, PROP_FAMILY);
                var type = ReadString(c, CAT_REVIT_ELEMENT, PROP_TYPE);
                var category = ReadString(c, CAT_REVIT_ELEMENT, PROP_CATEGORY);

                if (!string.IsNullOrWhiteSpace(category)) score += 30;
                if (!string.IsNullOrWhiteSpace(elementId)) score += 25;
                if (!string.IsNullOrWhiteSpace(family)) score += 15;
                if (!string.IsNullOrWhiteSpace(type)) score += 15;

                var display = (c.DisplayName ?? "").Trim();
                if (_structurePolicy.IsWrapperNode(display)) score -= 50;
                if (_structurePolicy.IsNoLevelNode(display)) score -= 10;

                candidates.Add((c, score, depth));
                depth++;
            }

            if (candidates.Count == 0)
                return null;

            return candidates
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.depth)
                .Select(x => x.node)
                .FirstOrDefault();
        }

        public ModelItem ResolveTypeNode(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return null;

            var candidates = new List<(ModelItem node, int score, int depth)>();
            int depth = 0;

            foreach (var a in inst.AncestorsAndSelf)
            {
                if (a == null) continue;
                if (!HasTypeData(a)) { depth++; continue; }

                int score = 0;
                var desc = ReadString(a, CAT_REVIT_TYPE, PROP_TYPE_DESC);
                var mat = ReadString(a, CAT_REVIT_TYPE, PROP_STRUCT_MAT);
                var sec = ReadString(a, CAT_REVIT_TYPE, PROP_SECTION_NAME);

                if (!string.IsNullOrWhiteSpace(desc)) score += 20;
                if (!string.IsNullOrWhiteSpace(mat)) score += 20;
                if (!string.IsNullOrWhiteSpace(sec)) score += 15;

                candidates.Add((a, score, depth));
                depth++;
            }

            if (candidates.Count == 0)
                return null;

            return candidates
                .OrderByDescending(x => x.score)
                .ThenBy(x => x.depth)
                .Select(x => x.node)
                .FirstOrDefault();
        }

        public string ReadString(ModelItem item, string categoryInternalName, string propertyInternalName)
        {
            var prop = FindProperty(item, categoryInternalName, propertyInternalName);
            return prop?.Value == null ? null : ReadVariantAsString(prop.Value);
        }

        public double? ReadDouble(ModelItem item, string categoryInternalName, string propertyInternalName)
        {
            var prop = FindProperty(item, categoryInternalName, propertyInternalName);
            return prop?.Value == null ? null : ReadVariantAsDouble(prop.Value);
        }

        public string ReadElementId(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_ELEMENT_ID);
        }

        public string ReadCategory(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_CATEGORY);
        }

        public string ReadCategoryId(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_CATEGORY_ID);
        }

        public string ReadFamily(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_FAMILY);
        }

        public string ReadType(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_TYPE);
        }

        public string ReadMark(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_MARK);
        }

        public double? ReadAreaM2(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            var prop = FindProperty(inst, CAT_REVIT_ELEMENT, PROP_AREA);
            if (prop?.Value == null) return null;

            var v = prop.Value;
            if (v.DataType == VariantDataType.DoubleArea)
                return v.ToDoubleArea() / 10.7639;

            return ReadVariantAsDouble(v);
        }

        public double? ReadVolumeM3(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            var prop = FindProperty(inst, CAT_REVIT_ELEMENT, PROP_VOL);
            if (prop?.Value == null) return null;

            var v = prop.Value;
            if (v.DataType == VariantDataType.DoubleVolume)
                return v.ToDoubleVolume() / 35.3147;

            return ReadVariantAsDouble(v);
        }

        public double? ReadLengthM(ModelItem item, string category)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return null;

            bool isStructural =
                string.Equals(category, "Structural Framing", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Structural Columns", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Armazón estructural", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Marcos estructurales", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Vigas estructurales", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Columnas estructurales", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(category, "Pilares estructurales", StringComparison.OrdinalIgnoreCase);

            var propName = isStructural ? PROP_LEN_STRUCT : PROP_LEN_DEFAULT;
            var prop = FindProperty(inst, CAT_REVIT_ELEMENT, propName);
            if (prop?.Value == null) return null;

            var v = prop.Value;
            if (v.DataType == VariantDataType.DoubleLength)
                return v.ToDoubleLength();

            return ReadVariantAsDouble(v);
        }

        public string ReadSizeText(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";

            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_SIZE) ?? "";
        }

        public double? ReadLengthByInstanceM(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return null;

            var prop = FindProperty(inst, CAT_REVIT_ELEMENT, PROP_LEN_DEFAULT);
            if (prop?.Value == null) return null;

            var v = prop.Value;
            if (v.DataType == VariantDataType.DoubleLength)
                return v.ToDoubleLength();

            return ReadVariantAsDouble(v);
        }

        public string ReadElectricalData(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_ELECTRICAL_DATA) ?? "";
        }

        public string ReadPanelName(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_PANEL_NAME) ?? "";
        }

        public string ReadMainBreakerPower(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_MAIN_BREAKER_POWER) ?? "";
        }

        public string ReadPanelInstance(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";
            return ReadString(inst, CAT_REVIT_ELEMENT, PROP_PANEL_INSTANCE) ?? "";
        }

        public string ReadCustomPartida(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";

            return ReadAsString(inst, CAT_REVIT_CUSTOM, PROP_CUSTOM_PARTIDA) ?? "";
        }

        public string ReadFamilyTypeName(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return "";

            foreach (var a in inst.AncestorsAndSelf)
            {
                var value = ReadString(a, CAT_REVIT_FAMILY, PROP_FAMILY_TYPE_NAME);
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "";
        }

        public string ReadTypeNodeName(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_NODE_NAME) ?? "";
        }

        public string ReadCategoryDisplay(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_CATEGORY) ?? "";
        }

        public string ReadLoadClassification(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_LOAD_CLASSIFICATION) ?? "";
        }

        public string ReadKeynoteNote(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_KEYNOTE_NOTE) ?? "";
        }

        public string ReadTypeComments(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_COMMENTS) ?? "";
        }

        public string ReadUrl(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return "";
            return ReadString(typeNode, CAT_REVIT_TYPE, PROP_URL) ?? "";
        }

        public double? ReadDimensionA(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return null;
            return ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_DIM_A);
        }

        public double? ReadDimensionB(ModelItem item)
        {
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return null;
            return ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_DIM_B);
        }

        public double? ReadCutLengthM(ModelItem item)
        {
            var inst = ResolveInstanceNode(item);
            if (inst == null) return null;

            var prop = FindProperty(inst, CAT_REVIT_ELEMENT, PROP_CUT_LENGTH);
            if (prop?.Value == null) return null;

            var v = prop.Value;
            if (v.DataType == VariantDataType.DoubleLength)
                return v.ToDoubleLength();

            return ReadVariantAsDouble(v);
        }

        public TypePropertyBag ReadTypeProperties(ModelItem item)
        {
            var instanceNode = ResolveInstanceNode(item);
            var typeNode = ResolveTypeNode(item);
            if (typeNode == null) return new TypePropertyBag();

            var bag = new TypePropertyBag
            {
                TypeDescription = ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_DESC) ?? "",
                StructuralMaterial = ReadString(typeNode, CAT_REVIT_TYPE, PROP_STRUCT_MAT) ?? "",
                WidthM = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_TYPE_WIDTH),
                NominalWeightKgm = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_NOMINAL_WEIGHT),
                LinearWeightKgm = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_LINEAR_WEIGHT),
                DepthM = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_DEPTH),
                WidthXM = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_WIDTH_X),
                SectionName = ReadString(typeNode, CAT_REVIT_TYPE, PROP_SECTION_NAME) ?? "",
                SectionShape = ReadString(typeNode, CAT_REVIT_TYPE, PROP_SECTION_SHAPE) ?? "",
                CodeName = ReadString(typeNode, CAT_REVIT_TYPE, PROP_CODE_NAME) ?? "",
                FamilyTypeName = ReadFamilyTypeName(instanceNode) ?? "",
                TypeNodeName = ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_NODE_NAME) ?? "",
                CategoryDisplay = ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_CATEGORY) ?? "",
                LoadClassification = ReadString(typeNode, CAT_REVIT_TYPE, PROP_LOAD_CLASSIFICATION) ?? "",
                KeynoteNote = ReadString(typeNode, CAT_REVIT_TYPE, PROP_KEYNOTE_NOTE) ?? "",
                TypeComments = ReadString(typeNode, CAT_REVIT_TYPE, PROP_TYPE_COMMENTS) ?? "",
                Url = ReadString(typeNode, CAT_REVIT_TYPE, PROP_URL) ?? "",
                DimensionA = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_DIM_A),
                DimensionB = ReadDouble(typeNode, CAT_REVIT_TYPE, PROP_DIM_B),
                CustomWeightRaw =
                    ReadAsString(typeNode, CAT_REVIT_CUSTOM, PROP_VDC_WEIGHT) ??
                    ReadAsString(instanceNode, CAT_REVIT_CUSTOM, PROP_VDC_WEIGHT) ??
                    ""
            };

            foreach (var propName in PROP_TYPE_THICKNESS)
            {
                var thickness = ReadDouble(typeNode, CAT_REVIT_TYPE, propName);
                if (thickness.HasValue)
                {
                    bag.ThicknessM = thickness;
                    break;
                }
            }

            return bag;
        }

        private string ReadAsString(ModelItem node, string category, string propName)
        {
            if (node == null) return null;

            foreach (var cat in node.PropertyCategories)
            {
                bool catMatch =
                    string.Equals(cat.Name, category, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cat.DisplayName, category, StringComparison.OrdinalIgnoreCase);

                if (!catMatch) continue;

                foreach (var prop in cat.Properties)
                {
                    bool propMatch =
                        string.Equals(prop.Name, propName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(prop.DisplayName, propName, StringComparison.OrdinalIgnoreCase);

                    if (!propMatch) continue;

                    var val = prop.Value == null ? null : ReadVariantAsString(prop.Value);
                    if (!string.IsNullOrWhiteSpace(val))
                        return val.Trim();
                }
            }

            return null;
        }

        private DataProperty FindProperty(ModelItem item, string categoryInternalName, string propertyInternalName)
        {
            if (item == null) return null;

            try
            {
                var byInternal = item.PropertyCategories.FindPropertyByName(categoryInternalName, propertyInternalName);
                if (byInternal != null) return byInternal;
            }
            catch { }

            try
            {
                foreach (var cat in item.PropertyCategories)
                {
                    bool catMatch =
                        string.Equals(cat.Name, categoryInternalName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(cat.DisplayName, categoryInternalName, StringComparison.OrdinalIgnoreCase);

                    if (!catMatch) continue;

                    foreach (var prop in cat.Properties)
                    {
                        bool propMatch =
                            string.Equals(prop.Name, propertyInternalName, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(prop.DisplayName, propertyInternalName, StringComparison.OrdinalIgnoreCase);

                        if (propMatch) return prop;
                    }
                }
            }
            catch { }

            return null;
        }

        private string ReadVariantAsString(VariantData v)
        {
            if (v == null) return null;

            try
            {
                switch (v.DataType)
                {
                    case VariantDataType.DisplayString:
                        return v.ToDisplayString();
                    case VariantDataType.NamedConstant:
                        {
                            var nc = v.ToNamedConstant();
                            return nc != null ? nc.ToString() : null;
                        }
                    case VariantDataType.Boolean:
                        return v.ToBoolean().ToString();
                    case VariantDataType.Int32:
                        return v.ToInt32().ToString(CultureInfo.InvariantCulture);
                    case VariantDataType.Double:
                        return v.ToDouble().ToString(CultureInfo.InvariantCulture);
                    case VariantDataType.DoubleLength:
                        return v.ToDoubleLength().ToString(CultureInfo.InvariantCulture);
                    case VariantDataType.DoubleArea:
                        return v.ToDoubleArea().ToString(CultureInfo.InvariantCulture);
                    case VariantDataType.DoubleVolume:
                        return v.ToDoubleVolume().ToString(CultureInfo.InvariantCulture);
                    default:
                        return v.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private double? ReadVariantAsDouble(VariantData v)
        {
            if (v == null) return null;

            try
            {
                switch (v.DataType)
                {
                    case VariantDataType.Int32:
                        return v.ToInt32();
                    case VariantDataType.Double:
                        return v.ToDouble();
                    case VariantDataType.DoubleLength:
                        return v.ToDoubleLength();
                    case VariantDataType.DoubleArea:
                        return v.ToDoubleArea();
                    case VariantDataType.DoubleVolume:
                        return v.ToDoubleVolume();
                    case VariantDataType.DisplayString:
                        return TryParseLooseDouble(v.ToDisplayString());
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static double? TryParseLooseDouble(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            raw = raw.Trim().Replace(",", ".");

            string token = new string(raw.TakeWhile(c =>
                char.IsDigit(c) || c == '.' || c == '-' || c == '+').ToArray());

            if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;

            return null;
        }
    }
}
