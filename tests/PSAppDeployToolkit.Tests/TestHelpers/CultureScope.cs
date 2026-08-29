using System;
using System.Globalization;
using System.Threading;

namespace PSAppDeployToolkit.Tests.TestHelpers
{
    /// <summary>
    /// Holds a thread's culture for the lifetime of one test and puts back what was there.
    /// </summary>
    /// <remarks>
    /// Several members read <see cref="CultureInfo.CurrentCulture"/>, so the only way to prove what they do on a German
    /// or Japanese machine is to become one. Both the format culture and the UI culture are set, since a member may
    /// read either.
    /// </remarks>
    public sealed class CultureScope : IDisposable
    {
        /// <summary>
        /// Switches the calling thread to the named culture.
        /// </summary>
        /// <param name="name">The culture to adopt, such as <c language="text">de-DE</c>.</param>
        public CultureScope(string name)
        {
            _previousCulture = Thread.CurrentThread.CurrentCulture;
            _previousUICulture = Thread.CurrentThread.CurrentUICulture;
            CultureInfo culture = CultureInfo.GetCultureInfo(name);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }

        /// <summary>
        /// Restores the thread's previous culture.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Thread.CurrentThread.CurrentCulture = _previousCulture;
            Thread.CurrentThread.CurrentUICulture = _previousUICulture;
            _disposed = true;
        }

        /// <summary>
        /// The culture the thread had before.
        /// </summary>
        private readonly CultureInfo _previousCulture;

        /// <summary>
        /// The UI culture the thread had before.
        /// </summary>
        private readonly CultureInfo _previousUICulture;

        /// <summary>
        /// Whether the culture has already been put back.
        /// </summary>
        private bool _disposed;
    }
}
