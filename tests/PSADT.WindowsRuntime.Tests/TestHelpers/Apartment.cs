using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PSADT.WindowsRuntime.Tests.TestHelpers
{
    /// <summary>
    /// Runs a delegate on a thread whose COM apartment the caller chooses.
    /// </summary>
    /// <remarks>
    /// The Windows Runtime is reached through COM, and the two apartment models initialise it
    /// differently. The test runner's threads are one model and the client that consumes this assembly
    /// runs its user interface on the other, so a wrapper that works in one apartment and throws in the
    /// other would go unnoticed if every test ran wherever the runner happened to put it.
    /// </remarks>
    internal static class Apartment
    {
        /// <summary>
        /// Runs a delegate on a new thread in the requested apartment and returns its result.
        /// </summary>
        /// <remarks>
        /// Whatever the delegate threw is rethrown here with its original stack trace, so a test sees
        /// the failure the code under test produced rather than a thread that quietly died.
        /// </remarks>
        /// <typeparam name="T">The value type the delegate returns. Constrained so that the result can be
        /// given a starting value without the null-forgiving operator; every caller here returns a bool.</typeparam>
        /// <param name="state">The apartment to run in.</param>
        /// <param name="work">The delegate to run.</param>
        /// <returns>Whatever the delegate returned.</returns>
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The exception is captured and rethrown unchanged on the calling thread; narrowing it here would lose failures instead of reporting them.")]
        public static T Run<T>(ApartmentState state, Func<T> work) where T : struct
        {
            T result = default;
            ExceptionDispatchInfo? failure = null;
            Thread thread = new(() =>
            {
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            })
            { IsBackground = true };
            thread.SetApartmentState(state);
            thread.Start();
            thread.Join();
            failure?.Throw();
            return result;
        }
    }
}
