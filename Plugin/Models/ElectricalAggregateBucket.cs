using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Models
{
    public class ElectricalAggregateBucket
    {
        public string Nivel { get; set; } = "";
        public string Sistema { get; set; } = "";
        public string CategoriaBoq { get; set; } = "";
        public string CategoriaRevit { get; set; } = "";
        public string Familia { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Unidad { get; set; } = "";

        public int NumElementos { get; set; }
        public double CantidadTotal { get; set; }
        public double LongitudTotalMl { get; set; }
        public int NumTramos { get; set; }
    }
}
