using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IElectricalQuantityMapperService
    {
        ElectricalRunRow Map(ElementSnapshot snapshot, string boqCategory, string unit);
    }
}
