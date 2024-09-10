using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSADT.Shared
{
    /// <summary>
    /// Custom attribute to handle a PowerShell ScriptBlock.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class PowerShellScriptBlockAttribute : Attribute
    {
        // This can be extended if needed for metadata, but the real work happens in the method.
    }
}
