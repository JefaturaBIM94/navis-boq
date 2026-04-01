using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NavisBOQ.Plugin.Models;

namespace NavisBOQ.Plugin.Services
{
    public interface IExecutionModePolicyService
    {
        ExecutionModeDecision EvaluateForRun(string runName, RunOptions options, ScopePreflight preflight);
    }
}
