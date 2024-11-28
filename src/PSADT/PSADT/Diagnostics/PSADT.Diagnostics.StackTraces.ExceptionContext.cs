using System;

namespace PSADT.Diagnostics.StackTraces
{
    public class ExceptionContext
    {
        public Exception Exception { get; }
        public int Depth { get; }

        public ExceptionContext(Exception exception, int depth)
        {
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
            Depth = depth;
        }
    }
}