using System.Management.Automation;

namespace PSAppDeployToolkit.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="CallStackFrame"/> objects.
    /// </summary>
    internal static class CallStackFrameExtensions
    {
        /// <summary>
        /// Gets the command name from the <see cref="CallStackFrame"/> object.
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Enforcing this rule just makes a mess.")]
        internal static string GetCommand(this CallStackFrame frame)
        {
            if (frame.InvocationInfo is null)
            {
                return frame.FunctionName;
            }
            if (frame.InvocationInfo.MyCommand is null)
            {
                return frame.InvocationInfo.InvocationName;
            }
            if (!string.IsNullOrWhiteSpace(frame.InvocationInfo.MyCommand.Name))
            {
                return frame.InvocationInfo.MyCommand.Name;
            }
            return frame.FunctionName;
        }
    }
}
