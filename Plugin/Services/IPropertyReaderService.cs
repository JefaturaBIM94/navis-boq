using System.Collections.Generic;
using Autodesk.Navisworks.Api;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IPropertyReaderService
    {
        ModelItem ResolveInstanceNode(ModelItem item);
        ModelItem ResolveTypeNode(ModelItem item);

        IEnumerable<ModelItem> EnumerateElementCandidates(ModelItem item);
        bool HasRevitElementData(ModelItem item);
        bool HasTypeData(ModelItem item);

        string ReadString(ModelItem item, string categoryInternalName, string propertyInternalName);
        double? ReadDouble(ModelItem item, string categoryInternalName, string propertyInternalName);
        double? ReadCutLengthM(ModelItem item);
        string ReadElementId(ModelItem item);
        string ReadCategory(ModelItem item);
        string ReadFamily(ModelItem item);
        string ReadType(ModelItem item);
        string ReadMark(ModelItem item);
        string ReadCategoryId(ModelItem item);
        double? ReadAreaM2(ModelItem item);
        double? ReadVolumeM3(ModelItem item);
        double? ReadLengthM(ModelItem item, string category);
        string ReadSizeText(ModelItem item);
        double? ReadLengthByInstanceM(ModelItem item);
        string ReadPanelInstance(ModelItem item);
        string ReadElectricalData(ModelItem item);
        string ReadPanelName(ModelItem item);
        string ReadMainBreakerPower(ModelItem item);
        string ReadCustomPartida(ModelItem item);
        string ReadFamilyTypeName(ModelItem item);
        string ReadTypeNodeName(ModelItem item);
        string ReadCategoryDisplay(ModelItem item);
        string ReadLoadClassification(ModelItem item);
        string ReadKeynoteNote(ModelItem item);
        string ReadTypeComments(ModelItem item);
        string ReadUrl(ModelItem item);
        double? ReadDimensionA(ModelItem item);
        double? ReadDimensionB(ModelItem item);

        TypePropertyBag ReadTypeProperties(ModelItem item);
    }
}
