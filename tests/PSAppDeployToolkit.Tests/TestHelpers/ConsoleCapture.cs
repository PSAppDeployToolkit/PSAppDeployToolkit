using System;
using System.IO;
using System.Text;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// Redirects the standard streams for the lifetime of one test and puts back what was there.
    /// </summary>
    /// <remarks>
    /// The console path writes straight to <see cref="Console.Out"/> and <see cref="Console.Error"/>, choosing between
    /// them by severity, so reading them back is the only way to test which one a message went to.
    /// </remarks>
    public sealed class ConsoleCapture : IDisposable
    {
        /// <summary>
        /// Redirects both standard streams.
        /// </summary>
        public ConsoleCapture()
        {
            _previousOut = Console.Out;
            _previousError = Console.Error;
            Console.SetOut(_out);
            Console.SetError(_error);
        }

        /// <summary>
        /// What has been written to standard output.
        /// </summary>
        public string Output => _out.ToString();

        /// <summary>
        /// What has been written to standard error.
        /// </summary>
        public string Error => _error.ToString();

        /// <summary>
        /// Puts the previous streams back.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Console.SetOut(_previousOut);
            Console.SetError(_previousError);
            _out.Dispose();
            _error.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// The writer standing in for standard output.
        /// </summary>
        private readonly StringWriter _out = new(new StringBuilder());

        /// <summary>
        /// The writer standing in for standard error.
        /// </summary>
        private readonly StringWriter _error = new(new StringBuilder());

        /// <summary>
        /// The writer that was standard output before.
        /// </summary>
        private readonly TextWriter _previousOut;

        /// <summary>
        /// The writer that was standard error before.
        /// </summary>
        private readonly TextWriter _previousError;

        /// <summary>
        /// Whether the streams have already been put back.
        /// </summary>
        private bool _disposed;
    }
}
