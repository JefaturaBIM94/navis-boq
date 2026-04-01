using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System;
using System.Collections.Generic;

namespace NavisBOQ.Plugin.Constants
{
    public static class ElectricalCategoryConstants
    {
        public const string DefaultNoSystem = "Sin sistema MEP";

        public static readonly Dictionary<string, ElectricalBoqDefinition> BoqMap =
            new Dictionary<string, ElectricalBoqDefinition>(StringComparer.OrdinalIgnoreCase)
            {
                ["Conexiones y accesorios"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Conexiones y accesorios",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Conduit Fittings",
                        "Uniones de tubo",
                        "Unión de tubo",
                        "Accesorios de tubo",
                        "Accesorios de tubería",
                        "Generic Models",
                        "Electrical Fixtures",
                        "Aparatos eléctricos",
                        "Electrical Equipment"
                    }
                },

                ["Interruptores"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Interruptores",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Electrical Equipment",
                        "Equipos Eléctricos",
                        "Electrical Fixtures",
                        "Aparatos eléctricos",
                        "Generic Models"
                    }
                },

                ["Luminarias"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Luminarias",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Lighting Fixtures",
                        "Luminarias"
                    }
                },

                ["Transformadores secos"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Transformadores secos",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Electrical Equipment",
                        "Equipos Eléctricos",
                        "Specialty Equipment",
                        "Equipos Especiales",
                        "Mechanical Equipment",
                        "Equipos Mecánicos",
                        "Generic Models"
                    }
                },

                ["Subestaciones"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Subestaciones",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Electrical Equipment",
                        "Equipos Eléctricos",
                        "Specialty Equipment",
                        "Equipos Especiales",
                        "Mechanical Equipment",
                        "Equipos Mecánicos",
                        "Generic Models"
                    }
                },

                ["Tableros secundarios"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Tableros secundarios",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Electrical Equipment",
                        "Equipos Eléctricos"
                    }
                },

                ["Tableros principales"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Tableros principales",
                    Unit = "pza",
                    RevitCategories = new[]
                    {
                        "Electrical Equipment",
                        "Equipos Eléctricos"
                    }
                },

                ["Tubos"] = new ElectricalBoqDefinition
                {
                    CanonicalName = "Tubos",
                    Unit = "ml",
                    RevitCategories = new[]
                    {
                        "Conduits",
                        "Conduit",
                        "Tubos",
                        "Tubo"
                    }
                }
            };

        public static readonly HashSet<string> StrictlyForbiddenPlumbingCategories =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // TEMPORALMENTE NO metemos "Tubos" ni "Uniones de tubo"
                // porque el usuario quiere aceptarlos en Corrida 4 por ahora.
                "Pipes",
                "Pipe Fittings",
                "Plumbing Fixtures",
                "Flex Pipes",
                "Pipe Accessories"
            };
    }

    public class ElectricalBoqDefinition
    {
        public string CanonicalName { get; set; } = "";
        public string Unit { get; set; } = "pza";
        public string[] RevitCategories { get; set; } = Array.Empty<string>();
    }
}
