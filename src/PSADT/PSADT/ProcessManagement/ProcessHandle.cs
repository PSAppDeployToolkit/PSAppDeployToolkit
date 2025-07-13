using System.Diagnostics;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents a handle to a process, encapsulating its optional process ID and an asynchronous task that provides
    /// the process result.
    /// </summary>
    /// <remarks>The <see cref="ProcessHandle"/> type is immutable and is used to track the state of a process.
    /// The <see cref="Task"/> property provides a mechanism to asynchronously retrieve the result of the process
    /// execution.</remarks>
    /// <param name="Process"></param>
    /// <param name="ModuleInfo"></param>
    /// <param name="LaunchInfo"></param>
    /// <param name="Task"></param>
    public sealed record ProcessHandle(Process Process, ProcessModule ModuleInfo, ProcessLaunchInfo LaunchInfo, Task<ProcessResult> Task);
}
