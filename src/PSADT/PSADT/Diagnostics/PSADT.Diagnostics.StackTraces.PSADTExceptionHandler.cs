using System;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// PSADTExceptionHandler custom exception class to propagate filtered stack traces.
    /// </summary>
    public class PSADTExceptionHandler : Exception
    {
        public PSADTExceptionHandler(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
