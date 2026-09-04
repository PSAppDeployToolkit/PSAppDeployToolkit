using System;
using System.Globalization;
using System.Threading;

namespace PSADT.Interop.Tests.TestHelpers
{
    /// <summary>
    /// Sets the current thread's culture for the lifetime of the scope and restores it afterwards.
    /// Culture is thread-local, so this affects only the test that creates the scope and never the
    /// machine.
    /// </summary>
    internal sealed class CultureScope : IDisposable
    {
        /// <summary>
        /// The culture in place when the scope was created.
        /// </summary>
        private readonly CultureInfo _previousCulture;

        /// <summary>
        /// Tracks whether the scope has already restored the previous culture.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Switches the current thread to the named culture.
        /// </summary>
        /// <param name="name">The culture name, for example "tr-TR".</param>
        internal CultureScope(string name)
        {
            _previousCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(name);
        }

        /// <summary>
        /// Restores the culture that was in place when the scope was created.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Thread.CurrentThread.CurrentCulture = _previousCulture;
                _disposed = true;
            }
        }
    }
}
