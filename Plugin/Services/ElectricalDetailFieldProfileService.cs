using System.Collections.Generic;
using NavisBOQ.Plugin.Constants;
using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public class ElectricalDetailFieldProfileService : IDetailFieldProfileService
    {
        public List<PropertyFieldRequest> GetProfile(string runName, string profileName)
        {
            var fields = new List<PropertyFieldRequest>();

            var profile = (profileName ?? "").Trim().ToLowerInvariant();

            if (profile == DetailProfileNames.TubeFocus)
            {
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementName", OutputField = "TypeNodeName" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementCategory", OutputField = "CategoryDisplay" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_Clasificaci?n de carga_PG_ELECTRICAL", OutputField = "LoadClassification" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_-1010103", OutputField = "Description" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_-1140422", OutputField = "KeynoteNote" });

                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1114240", OutputField = "SizeText" });
                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1004005", OutputField = "LengthByInstanceMl" });
                fields.Add(new PropertyFieldRequest { SourceNode = "custom", CategoryInternalName = "LcRevitData_Custom", PropertyInternalName = "lcldrevit_parameter_TR3Z - Partida_PG_TEXT", OutputField = "CustomPartida" });

                return fields;
            }

            if (profile == DetailProfileNames.FixtureFocus)
            {
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "LcOaSceneBaseUserName", OutputField = "FamilyTypeName" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1002503", OutputField = "OmniClassTitle" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1114206", OutputField = "PieceType" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1010105", OutputField = "TypeComments" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1010103", OutputField = "Description" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1010104", OutputField = "Url" });

                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1140104", OutputField = "PanelInstance" });
                fields.Add(new PropertyFieldRequest { SourceNode = "custom", CategoryInternalName = "LcRevitData_Custom", PropertyInternalName = "lcldrevit_parameter_TR3Z - Partida_PG_TEXT", OutputField = "CustomPartida" });

                return fields;
            }

            if (profile == DetailProfileNames.ElectricalFull)
            {
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "LcOaSceneBaseUserName", OutputField = "FamilyTypeName" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1002503", OutputField = "OmniClassTitle" });
                fields.Add(new PropertyFieldRequest { SourceNode = "family", CategoryInternalName = "lcldrevit_tab_family", PropertyInternalName = "lcldrevit_parameter_-1114206", OutputField = "PieceType" });

                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementName", OutputField = "TypeNodeName" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementCategory", OutputField = "CategoryDisplay" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_A_PG_DIMENSIONS", OutputField = "DimensionA" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_B_PG_DIMENSIONS", OutputField = "DimensionB" });
                fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "lcldrevit_parameter_-1010103", OutputField = "Description" });

                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1114241", OutputField = "ElectricalData" });
                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1140078", OutputField = "PanelName" });
                fields.Add(new PropertyFieldRequest { SourceNode = "instance", CategoryInternalName = "LcRevitData_Element", PropertyInternalName = "lcldrevit_parameter_-1140140", OutputField = "MainBreakerPower" });
                fields.Add(new PropertyFieldRequest { SourceNode = "custom", CategoryInternalName = "LcRevitData_Custom", PropertyInternalName = "lcldrevit_parameter_TR3Z - Partida_PG_TEXT", OutputField = "CustomPartida" });

                return fields;
            }

            fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementName", OutputField = "TypeNodeName" });
            fields.Add(new PropertyFieldRequest { SourceNode = "type", CategoryInternalName = "lcldrevit_tab_type", PropertyInternalName = "LcRevitPropertyElementCategory", OutputField = "CategoryDisplay" });

            return fields;
        }
    }
}
