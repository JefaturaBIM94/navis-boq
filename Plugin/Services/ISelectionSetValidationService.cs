using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface ISelectionSetValidationService
    {
        SelectionSetValidationResult ValidateForSteel(RunOptions options);
        SelectionSetValidationResult ValidateForElectrical(RunOptions options);
    }
}
