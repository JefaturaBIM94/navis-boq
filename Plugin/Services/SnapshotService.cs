using System;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class SnapshotService : ISnapshotService
    {
        private readonly IPropertyReaderService _propertyReader;
        private readonly INodeResolutionPolicyService _nodePolicy;
        private readonly IModelCategoryAliasService _aliasService;

        public SnapshotService(
            IPropertyReaderService propertyReader,
            INodeResolutionPolicyService nodePolicy,
            IModelCategoryAliasService aliasService)
        {
            _propertyReader = propertyReader ?? throw new ArgumentNullException(nameof(propertyReader));
            _nodePolicy = nodePolicy ?? throw new ArgumentNullException(nameof(nodePolicy));
            _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
        }

        public ElementSnapshot TryBuildSnapshot(ModelItem item)
        {
            if (item == null) return null;

            var rawInstNode = _propertyReader.ResolveInstanceNode(item);
            if (rawInstNode == null) return null;

            var rawCategory = _propertyReader.ReadCategory(rawInstNode) ?? "";
            var rawCategoryId = _propertyReader.ReadCategoryId(rawInstNode) ?? "";
            var category = _aliasService.NormalizeStructuralCategory(rawCategory, rawCategoryId);

            var instNode = _nodePolicy.ResolvePreferredInstanceNode(item, category, rawCategoryId);
            if (instNode == null) return null;

            var typeProps = _propertyReader.ReadTypeProperties(instNode);

            var snapshot = new ElementSnapshot
            {
                CanonicalId = SafeCanonicalId(instNode),
                ElementId = _propertyReader.ReadElementId(instNode) ?? "",
                Level = _nodePolicy.ResolvePreferredLevel(item, category),
                Category = category,
                CategoryId = rawCategoryId,
                Family = _propertyReader.ReadFamily(instNode) ?? "",
                Type = _propertyReader.ReadType(instNode) ?? "",
                Material = typeProps?.StructuralMaterial ?? "",
                Mark = _propertyReader.ReadMark(instNode) ?? "",

                LengthM = _propertyReader.ReadLengthM(instNode, category)
                          ?? _propertyReader.ReadLengthByInstanceM(instNode)
                          ?? 0,
                CutLengthM = _propertyReader.ReadCutLengthM(instNode) ?? 0,
                AreaM2 = _propertyReader.ReadAreaM2(instNode) ?? 0,
                VolumeM3 = _propertyReader.ReadVolumeM3(instNode) ?? 0,

                TypeDesc = typeProps?.TypeDescription ?? "",
                TypeMaterial = typeProps?.StructuralMaterial ?? "",
                TypeWidth = typeProps?.WidthM ?? 0,
                TypeThickness = typeProps?.ThicknessM ?? 0,

                NominalWeightKgm = typeProps?.NominalWeightKgm ?? 0,
                LinearWeightKgm = typeProps?.LinearWeightKgm ?? 0,
                DepthM = typeProps?.DepthM ?? 0,
                WidthXM = typeProps?.WidthXM ?? 0,
                SectionName = typeProps?.SectionName ?? "",
                SectionShape = typeProps?.SectionShape ?? "",
                CodeName = typeProps?.CodeName ?? "",
                CustomWeightRaw = typeProps?.CustomWeightRaw ?? "",

                // SAFE MODE R4
                SystemName = "Sin sistema MEP",
                SystemType = "",
                SystemClassification = "",
                ElectricalData = "",
                PanelName = "",
                MainBreakerPower = "",
                PanelInstance = "",
                CustomPartida = "",
                OmniClassTitle = "",
                PieceType = "",
                FamilyTypeName = "",
                TypeNodeName = "",
                CategoryDisplay = "",
                LoadClassification = "",
                KeynoteNote = "",
                TypeComments = "",
                Url = "",
                DimensionA = 0,
                DimensionB = 0,
                SizeText = "",
                LengthByInstanceM = 0,

                SourceSystem = "Revit",

                ResolvedFrom = _nodePolicy.GetResolvedFrom(category),
                LevelSource = _nodePolicy.GetLevelSource(item, category),
                GeometryConfidence = _nodePolicy.GetGeometryConfidence(instNode, category),
                NestedFamilyDetected = _nodePolicy.IsNestedOrSubcomponent(item, category)
            };

            if (IsTubeLikeCategory(category))
            {
                try
                {
                    snapshot.SizeText = _propertyReader.ReadSizeText(instNode) ?? "";
                }
                catch
                {
                    snapshot.SizeText = "";
                }

                try
                {
                    snapshot.LengthByInstanceM = _propertyReader.ReadLengthByInstanceM(instNode) ?? 0;
                }
                catch
                {
                    snapshot.LengthByInstanceM = 0;
                }

                if (snapshot.LengthM <= 0 && snapshot.LengthByInstanceM > 0)
                    snapshot.LengthM = snapshot.LengthByInstanceM;
            }

            snapshot.PartialData =
                (_aliasService.IsStructuralFoundation(category, rawCategoryId) && snapshot.VolumeM3 <= 0) ||
                (_aliasService.IsGenericModel(category, rawCategoryId) && snapshot.Level == "Sin nivel");

            return snapshot;
        }

        private static bool IsTubeLikeCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;

            return string.Equals(category, "Conduits", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Conduit", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Tubos", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(category, "Tubo", StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeCanonicalId(ModelItem item)
        {
            try
            {
                var guid = item.InstanceGuid;
                if (guid != Guid.Empty)
                    return guid.ToString();
            }
            catch { }

            try
            {
                return item.DisplayName ?? Guid.NewGuid().ToString();
            }
            catch
            {
                return Guid.NewGuid().ToString();
            }
        }
    }
}
