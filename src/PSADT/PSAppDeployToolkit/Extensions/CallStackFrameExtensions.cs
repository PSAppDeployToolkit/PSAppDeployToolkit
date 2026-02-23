using System.Management.Automation;

namespace PSAppDeployToolkit.Extensions
{
    /// <summary>
    /// Extension methods for <see cref="CallStackFrame"/> objects.
    /// </summary>
    internal static class CallStackFrameExtensions
    {
        /// <summary>
        /// Retrieves the name of the command associated with the specified call stack frame.
        /// </summary>
        /// <remarks>If the invocation information or command name is unavailable, the function name is
        /// returned instead. This method is intended for internal use when analyzing or displaying call stack
        /// information.</remarks>
        /// <param name="frame">The call stack frame from which to obtain the command name. This parameter cannot be null.</param>
        /// <returns>The name of the command associated with the call stack frame, or the function name if no command is found.</returns>
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
