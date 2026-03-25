using System;
using System.Collections.Generic;

namespace NavisBOQ.Plugin.Constants
{
    /// <summary>
    /// Define mappings between Revit/Navisworks category names (both English and Spanish)
    /// and the internal names and default units used by the BOQ system.  This class
    /// centralises category configuration so that all runs and services reference
    /// the same definitions.  For each category, the tuple (Name, Unit) identifies
    /// the canonical Spanish name used in reports and the primary measurement unit.
    /// </summary>
    public static class CategoryConstants
    {
        /// <summary>
        /// Map of category names (case‑insensitive) to a tuple of internal name and default unit.
        /// If you supply either the English or Spanish category key, you will get
        /// the same internal name and unit.  Units are "m2", "m3", "ml" or "pza"
        /// (piezas) depending on whether the category is typically quantified by area,
        /// volume, length or count.  See DefaultMeasureTypes for additional metrics.
        /// </summary>
        public static readonly Dictionary<string, (string Name, string Unit)> Mapa =
            new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            // Constructive categories with surface area units
            {"Walls", ("Muros", "m2")},
            {"Muros", ("Muros", "m2")},
            {"Floors", ("Losas", "m2")},
            {"Suelos", ("Losas", "m2")},
            {"Losas", ("Losas", "m2")},
            {"Ceilings", ("Plafones", "m2")},
            {"Techos", ("Plafones", "m2")},
            {"Roofs", ("Cubiertas", "m2")},
            {"Cubiertas", ("Cubiertas", "m2")},

            // Openings
            {"Doors", ("Puertas", "pza")},
            {"Puertas", ("Puertas", "pza")},
            {"Windows", ("Ventanas", "pza")},
            {"Ventanas", ("Ventanas", "pza")},

            // Vertical circulation
            {"Stairs", ("Escaleras", "pza")},
            {"Escaleras", ("Escaleras", "pza")},

            // Railings
            {"Railings", ("Barandales", "ml")},
            {"Barandillas", ("Barandales", "ml")},

            // Structural elements
            {"Structural Framing", ("Vigas", "ml")},
            {"Vigas estructurales", ("Vigas", "ml")},
            {"Structural Columns", ("Columnas", "ml")},
            {"Columnas estructurales", ("Columnas", "ml")},
            {"Structural Foundations", ("Cimentacion", "m3")},
            {"Cimentaciones", ("Cimentacion", "m3")},

            // Curtain systems
            {"Curtain Wall Panels", ("Fachada", "m2")},
            {"Curtain Panels", ("Fachada", "m2")},
            {"Curtain Wall Mullions", ("Montantes", "ml")},

            // HVAC and piping
            {"Ducts", ("Ductos", "ml")},
            {"Duct Fittings", ("Conex Ducto", "pza")},
            {"Pipes", ("Tuberias", "ml")},
            {"Pipe Fittings", ("Conex Tubo", "pza")},

            // Plumbing fixtures
            {"Plumbing Fixtures", ("Sanitarios", "pza")},
            {"Aparatos sanitarios", ("Sanitarios", "pza")},

            // Mechanical equipment
            {"Mechanical Equipment", ("Eq Mecanico", "pza")},
            {"Air Terminals", ("Difusores", "pza")},

            // Electrical equipment and lighting
            {"Electrical Equipment", ("Tableros", "pza")},
            {"Lighting Fixtures", ("Luminarias", "pza")},

            // Cable trays and conduits
            {"Cable Trays", ("Charolas", "ml")},
            {"Conduits", ("Conduits", "ml")},

            // Miscellaneous models
            {"Generic Models", ("Generico", "pza")},
            {"Modelos genéricos", ("Generico", "pza")},
            {"Specialty Equipment", ("Eq Especial", "pza")},

            // Furniture and casework
            {"Furniture", ("Mobiliario", "pza")},
            {"Mobiliario", ("Mobiliario", "pza")},
            {"Casework", ("Carpinteria", "pza")},
        };

        /// <summary>
        /// Categories that should always be counted by piece (unidad "pza") regardless of
        /// other geometric measures.  When a category appears in this set, the quantity
        /// returned by the BOQ will default to 1 per element, and the unit will be "pza".
        /// </summary>
        public static readonly HashSet<string> EsPza = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Individual openings and fixtures
            "Doors", "Puertas",
            "Windows", "Ventanas",
            "Stairs", "Escaleras",
            "Plumbing Fixtures", "Sanitarios", "Aparatos sanitarios",
            "Mechanical Equipment", "Eq Mecanico",
            "Air Terminals", "Difusores",
            "Electrical Equipment", "Tableros",
            "Lighting Fixtures", "Luminarias",

            // Generic items
            "Generic Models", "Generico", "Modelos genéricos",
            "Specialty Equipment", "Eq Especial",
            "Furniture", "Mobiliario",
            "Casework", "Carpinteria",

            // Small fittings and components
            "Duct Fittings", "Conex Ducto",
            "Pipe Fittings", "Conex Tubo",
            "Railings", "Barandales"
        };

        /// <summary>
        /// Types of measurements supported for quantifying elements.  A category may
        /// support multiple measurement types; for example, walls are reported by
        /// surface area, volume and length (perimeter), while doors are counted by
        /// pieces only.  The BOQ logic selects the appropriate measurement type
        /// based on available Revit parameters.
        /// </summary>
        public enum MeasureType
        {
            /// <summary>Count the number of instances (pieces).</summary>
            Piece,
            /// <summary>Surface area in square meters.</summary>
            Area,
            /// <summary>Volume in cubic meters.</summary>
            Volume,
            /// <summary>Length in meters (linear measurement).</summary>
            Length,
            /// <summary>Perimeter in meters (primarily for slabs/roofs).</summary>
            Perimeter
        }

        /// <summary>
        /// Default measurement types per internal category name.  Constructive categories
        /// (e.g. Muros, Losas, Cubiertas, Columnas, Vigas, Cimentacion) use area,
        /// volume and/or length to compute quantities.  Other categories default
        /// to counting pieces.  These definitions reflect how the BOQ plugin
        /// interprets Revit parameters for each category.
        /// </summary>
        public static readonly Dictionary<string, MeasureType[]> DefaultMeasureTypes =
            new Dictionary<string, MeasureType[]>(StringComparer.OrdinalIgnoreCase)
        {
            // Muros (Walls): area, volume and linear length
            {"Muros", new[]{MeasureType.Area, MeasureType.Volume, MeasureType.Length}},
            // Losas/Suelos/Floors: area, volume and perimeter
            {"Losas", new[]{MeasureType.Area, MeasureType.Volume, MeasureType.Perimeter}},
            // Plafones/Ceilings: area and volume
            {"Plafones", new[]{MeasureType.Area, MeasureType.Volume}},
            // Cubiertas/Roofs: area, volume and perimeter
            {"Cubiertas", new[]{MeasureType.Area, MeasureType.Volume, MeasureType.Perimeter}},
            // Vigas (Structural Framing): linear length and volume
            {"Vigas", new[]{MeasureType.Length, MeasureType.Volume}},
            // Columnas (Structural Columns): linear length and volume
            {"Columnas", new[]{MeasureType.Length, MeasureType.Volume}},
            // Cimentacion (Structural Foundations): volume only
            {"Cimentacion", new[]{MeasureType.Volume}},
            // Fachada (Curtain Wall Panels): area only
            {"Fachada", new[]{MeasureType.Area}},
            // Montantes (Curtain Wall Mullions): linear length only
            {"Montantes", new[]{MeasureType.Length}},
            // Ductos, Tuberias, Charolas, Conduits: linear length only
            {"Ductos", new[]{MeasureType.Length}},
            {"Tuberias", new[]{MeasureType.Length}},
            {"Charolas", new[]{MeasureType.Length}},
            {"Conduits", new[]{MeasureType.Length}},
            // Conex Ducto, Conex Tubo: pieces
            {"Conex Ducto", new[]{MeasureType.Piece}},
            {"Conex Tubo", new[]{MeasureType.Piece}},
            // Sanitarios (Plumbing Fixtures): pieces
            {"Sanitarios", new[]{MeasureType.Piece}},
            // Equipment and fixtures: pieces
            {"Eq Mecanico", new[]{MeasureType.Piece}},
            {"Difusores", new[]{MeasureType.Piece}},
            {"Tableros", new[]{MeasureType.Piece}},
            {"Luminarias", new[]{MeasureType.Piece}},
            // Generico/Generic Models: pieces
            {"Generico", new[]{MeasureType.Piece}},
            // Specialty equipment, furniture, casework: pieces
            {"Eq Especial", new[]{MeasureType.Piece}},
            {"Mobiliario", new[]{MeasureType.Piece}},
            {"Carpinteria", new[]{MeasureType.Piece}},
        };

        /// <summary>
        /// Resolve a given category name (English or Spanish) to the internal Spanish
        /// name and its default unit.  If the category is not found in the map,
        /// the original string is returned with an empty unit.
        /// </summary>
        /// <param name="category">The English or Spanish category name.</param>
        /// <returns>A tuple containing the internal name and unit.</returns>
        public static (string Name, string Unit) Resolve(string category)
        {
            return Mapa.TryGetValue(category, out var tuple) ? tuple : (category, "");
        }
    }
}