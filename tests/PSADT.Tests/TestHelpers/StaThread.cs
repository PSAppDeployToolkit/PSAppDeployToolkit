using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PSADT.Tests.TestHelpers
{
    /// <summary>
    /// Runs a test body on a single-threaded apartment.
    /// </summary>
    /// <remarks>
    /// Some of the COM objects this assembly wraps can only be created on a single-threaded apartment.
    /// The internet shortcut is one: creating it from a multi-threaded apartment puts the object in the
    /// host apartment and then needs a marshalling proxy for <c>IUniformResourceLocatorW</c>, which has
    /// none registered, so the creation fails with "Interface not registered" rather than with anything
    /// that names the real cause.
    /// <para>
    /// That is a constraint on the caller rather than a defect. Both Windows PowerShell and PowerShell 7
    /// run their main thread as a single-threaded apartment, so the module always satisfies it. A test
    /// runner does not - xunit runs test bodies on thread pool threads, which are multi-threaded - so the
    /// affected tests hop onto an apartment of their own through here.
    /// </para>
    /// <para>
    /// xunit.v3 has no apartment-aware fact attribute, hence doing it by hand. Exceptions are captured and
    /// rethrown with their original stack trace so an assertion failure inside still reads normally.
    /// </para>
    /// </remarks>
    public static class StaThread
    {
        /// <summary>
        /// Runs the given action on a single-threaded apartment and waits for it to finish.
        /// </summary>
        /// <param name="action">The action to run.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Everything the action can throw has to cross back to the calling thread, including assertion failures, so the catch is deliberately unfiltered and the exception is rethrown with its original stack trace.")]
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            Thread thread = new(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    captured = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }
    }
}
