using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    /// <summary>
    /// Opciones de ejecución para las corridas y extracciones.
    /// </summary>
    public class RunOptions
    {
        /// <summary>Método de alcance: "all", "selection", "selection_set" o "level".</summary>
        public string ScopeMode { get; set; } = "all";

        /// <summary>Nombre del Selection Set cuando ScopeMode = "selection_set".</summary>
        public string SelectionSet { get; set; } = "";

        /// <summary>Nivel (capa Revit/Navis) cuando ScopeMode = "level".</summary>
        public string Level { get; set; } = "";

        /// <summary>Modo de salida: "auto", "summary" o "detail".</summary>
        public string OutputMode { get; set; } = "auto";

        /// <summary>Límite de elementos (instancias) a cuantificar antes de forzar resumen.</summary>
        public int MaxItems { get; set; } = 12_000;

        /// <summary>Límite de nodos totales que se recorrerán en el árbol de Navisworks.</summary>
        public int MaxNodes { get; set; } = 50_000;

        /// <summary>Si es verdadero, el plugin cancelará la corrida cuando se excedan los límites.</summary>
        public bool StrictLimits { get; set; } = true;
    }
}