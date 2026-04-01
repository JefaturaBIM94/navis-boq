using System;
using System.Collections.Generic;

namespace NavisBOQ.Plugin.Services
{
    public class ModelCategoryAliasService : IModelCategoryAliasService
    {
        private static readonly HashSet<string> StructuralFramingAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Structural Framing",
                "Armazón estructural",
                "Marcos estructurales",
                "Vigas estructurales",
                "OST_StructuralFraming",
                "Vigas",
                "Framing"
            };

        private static readonly HashSet<string> StructuralColumnAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Structural Columns",
                "Columnas estructurales",
                "Pilares estructurales",
                "OST_StructuralColumns",
                "Columnas",
                "Columns"
            };

        private static readonly HashSet<string> StructuralFoundationAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Structural Foundations",
                "Cimentaciones estructurales",
                "Cimentaciones",
                "Foundations",
                "Wall Foundation",
                "OST_StructuralFoundation",
                "Pile Foundations",
                "Cimentaciones por pilotes",
                "Pilotes"
            };

        private static readonly HashSet<string> GenericModelAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Generic Models",
                "Modelos genéricos",
                "OST_GenericModel"
            };

        private static readonly HashSet<string> ElectricalEquipmentAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Electrical Equipment",
                "Equipos Eléctricos",
                "OST_ElectricalEquipment"
            };

        private static readonly HashSet<string> ElectricalFixturesAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Electrical Fixtures",
                "Aparatos Eléctricos",
                "OST_ElectricalFixtures"
            };

        private static readonly HashSet<string> LightingFixturesAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Lighting Fixtures",
                "Luminarias",
                "OST_LightingFixtures"
            };

        private static readonly HashSet<string> ConduitsAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Conduits",
                "Tubo",
                "Tubos",
                "Conduit",
                "OST_Conduit"
            };

        private static readonly HashSet<string> ConduitFittingsAliases =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Conduit Fittings",
                "Accesorios de tubo",
                "Accesorios de conduits",
                "OST_ConduitFitting"
            };

        public bool IsStructuralFraming(string category, string categoryId = "")
        {
            if (StructuralFramingAliases.Contains((category ?? "").Trim()))
                return true;

            return string.Equals((categoryId ?? "").Trim(), "-2001320", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsStructuralColumn(string category, string categoryId = "")
        {
            if (StructuralColumnAliases.Contains((category ?? "").Trim()))
                return true;

            return string.Equals((categoryId ?? "").Trim(), "-2001330", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsStructuralFoundation(string category, string categoryId = "")
        {
            if (StructuralFoundationAliases.Contains((category ?? "").Trim()))
                return true;

            return string.Equals((categoryId ?? "").Trim(), "-2001300", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsGenericModel(string category, string categoryId = "")
        {
            if (GenericModelAliases.Contains((category ?? "").Trim()))
                return true;

            return string.Equals((categoryId ?? "").Trim(), "-2000151", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsElectricalEquipment(string category)
        {
            return ElectricalEquipmentAliases.Contains((category ?? "").Trim());
        }

        public bool IsElectricalFixture(string category)
        {
            return ElectricalFixturesAliases.Contains((category ?? "").Trim());
        }

        public bool IsLightingFixture(string category)
        {
            return LightingFixturesAliases.Contains((category ?? "").Trim());
        }

        public bool IsConduit(string category)
        {
            return ConduitsAliases.Contains((category ?? "").Trim());
        }

        public bool IsConduitFitting(string category)
        {
            return ConduitFittingsAliases.Contains((category ?? "").Trim());
        }

        public string NormalizeStructuralCategory(string category, string categoryId = "")
        {
            if (IsStructuralFraming(category, categoryId)) return "Structural Framing";
            if (IsStructuralColumn(category, categoryId)) return "Structural Columns";
            if (IsStructuralFoundation(category, categoryId)) return "Structural Foundations";
            if (IsGenericModel(category, categoryId)) return "Generic Models";
            return (category ?? "").Trim();
        }
    }
}
