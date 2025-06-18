using System.Management.Automation;

namespace PSADT.Extensions
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
        internal static string GetCommand(this CallStackFrame frame)
        {
            if (null == frame.InvocationInfo)
            {
                return frame.FunctionName;
            }
            if (null == frame.InvocationInfo.MyCommand)
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
