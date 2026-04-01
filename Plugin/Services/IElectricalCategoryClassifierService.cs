using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavisBOQ.Plugin.Services
{
    public interface IElectricalCategoryClassifierService
    {
        bool TryClassify(string revitCategory, out string boqCategory, out string unit);
    }
}
