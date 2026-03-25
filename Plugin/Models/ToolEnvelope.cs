using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace NavisBOQ.Plugin.Models
{
    /// <summary>
    /// Envoltura estándar para todas las respuestas del plugin.
    /// </summary>
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
}