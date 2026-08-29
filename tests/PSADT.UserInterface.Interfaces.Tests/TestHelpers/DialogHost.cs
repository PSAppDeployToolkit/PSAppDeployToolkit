using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// The single-threaded apartment every dialog in this assembly is built on, and the one place
    /// <see cref="DialogManager"/> is allowed to start up.
    /// </summary>
    /// <remarks>
    /// Both UI stacks in this project are thread-affine and have process-global setup that happens once
    /// and cannot be undone, so the tests cannot each stand up their own environment. Three constraints
    /// force the shape of this class.
    /// <para>
    /// First, everything has to share one apartment. Windows Forms controls and WPF windows are owned by
    /// the thread that creates them, and a WPF element resolving a DynamicResource against an
    /// <c language="csharp">Application</c> living on a different thread fails. So there is one apartment here and every
    /// test body that touches a dialog runs on it.
    /// </para>
    /// <para>
    /// Second, that apartment has to be the one <see cref="DialogManager"/> creates. Its static
    /// constructor starts a WPF <c language="csharp">Application</c> on a dedicated thread of its own and there is no way
    /// to hand it one; whichever thread it picks is where <c language="csharp">Application.Current</c> lives for the rest
    /// of the process. Rather than fight that, this class touches <see cref="DialogManager"/> first and
    /// then adopts its dispatcher as the shared apartment.
    /// </para>
    /// <para>
    /// Third, the order is not negotiable. <see cref="DialogManager"/>'s static constructor calls
    /// <c language="csharp">Application.SetCompatibleTextRenderingDefault</c>, which throws once any Windows Forms control
    /// exists in the process. A Classic dialog test that ran first would poison every DialogManager test
    /// for the rest of the run. Going through <see cref="Run(Action)"/> for all of them makes the boot
    /// happen on first use whatever order the runner picks, so no test has to know about the rule. The
    /// assembly also disables parallelism in xunit.runner.json, so first use is genuinely first.
    /// </para>
    /// <para>
    /// Starting <see cref="DialogManager"/> does reach outside the process once: it refreshes the shell's
    /// desktop icons. That is a transient broadcast that stores nothing, and it is unavoidable if the
    /// type is to be tested at all - it happens in a static constructor, so merely naming any member of
    /// the class triggers it.
    /// </para>
    /// </remarks>
    [SuppressMessage("Usage", "VSTHRD001:Await JoinableTaskFactory.SwitchToMainThreadAsync() to switch to the UI thread instead of APIs that can deadlock or require specifying a priority", Justification = "The dialog manager marshals to its own dedicated WPF dispatcher thread outside any JoinableTaskFactory context, and these tests have to reach the same one.")]
    [SuppressMessage("Design", "MA0045:Do not use blocking calls in a sync method", Justification = "Test bodies are synchronous and have to observe the dialog they built before returning, so the marshalling here is deliberately blocking.")]
    internal static class DialogHost
    {
        /// <summary>
        /// The shared apartment's dispatcher, started on first use.
        /// </summary>
        private static readonly Lazy<Dispatcher> LazyDispatcher = new(Boot, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// The dispatcher owning the apartment every dialog is built on.
        /// </summary>
        public static Dispatcher Dispatcher => LazyDispatcher.Value;

        /// <summary>
        /// Runs a test body on the shared apartment and waits for it to finish.
        /// </summary>
        /// <remarks>
        /// Exceptions cross back with their original stack traces, so an assertion failing inside the body
        /// reads the way it would if the test had run on the calling thread.
        /// </remarks>
        /// <param name="action">The body to run.</param>
        public static void Run(Action action)
        {
            Dispatcher.Invoke(action, DispatcherPriority.Normal, TestContext.Current.CancellationToken);
        }

        /// <summary>
        /// Runs a test body on the shared apartment and returns what it produced.
        /// </summary>
        /// <typeparam name="TResult">The type the body produces.</typeparam>
        /// <param name="callback">The body to run.</param>
        /// <returns>Whatever the body returned.</returns>
        public static TResult Run<TResult>(Func<TResult> callback)
        {
            return Dispatcher.Invoke(callback, DispatcherPriority.Normal, TestContext.Current.CancellationToken);
        }

        /// <summary>
        /// Runs an asynchronous test body on the shared apartment.
        /// </summary>
        /// <remarks>
        /// Continuations resume on the apartment through its synchronization context, so a body that
        /// awaits in the middle still touches every dialog from the owning thread.
        /// </remarks>
        /// <param name="body">The body to run.</param>
        /// <returns>A task that completes when the body and everything it awaited have finished.</returns>
        public static Task RunAsync(Func<Task> body)
        {
            return Dispatcher.InvokeAsync(body, DispatcherPriority.Normal, TestContext.Current.CancellationToken).Task.Unwrap();
        }

        /// <summary>
        /// Builds a dialog on the shared apartment, runs a test body against it, and disposes it, all
        /// without leaving the apartment.
        /// </summary>
        /// <remarks>
        /// WPF is stricter about its apartment than Windows Forms is. Every property of a window or of
        /// anything in it is a dependency property, and reading one from another thread throws rather
        /// than returning a stale value - so a Fluent dialog cannot be built here and asserted on over
        /// there. Disposing has the same constraint, because detaching a routed event handler and
        /// stopping a dispatcher timer are both apartment-bound.
        /// <para>
        /// Passing the body in rather than handing the dialog back is what keeps all three on the right
        /// thread without every test having to remember to arrange it.
        /// </para>
        /// </remarks>
        /// <typeparam name="TDialog">The type of dialog to build.</typeparam>
        /// <param name="factory">Builds the dialog.</param>
        /// <param name="body">The assertions to run against it.</param>
        public static void WithDialog<TDialog>(Func<TDialog> factory, Action<TDialog> body)
            where TDialog : IDisposable
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentNullException.ThrowIfNull(body);
            Run(() =>
            {
                using TDialog dialog = factory();
                body(dialog);
            });
        }

        /// <summary>
        /// Drains the dispatcher queue so queued layout, binding and idle callbacks have run before a
        /// test samples the state they affect.
        /// </summary>
        /// <remarks>
        /// Invoking at <see cref="DispatcherPriority.ApplicationIdle"/> blocks until everything queued
        /// above that priority has been processed, which subsumes the render and binding priorities in
        /// one call.
        /// </remarks>
        public static void Drain()
        {
            Dispatcher.Invoke(static () => { }, DispatcherPriority.ApplicationIdle, TestContext.Current.CancellationToken);
        }

        /// <summary>
        /// Starts <see cref="DialogManager"/> and hands back the dispatcher it created.
        /// </summary>
        /// <remarks>
        /// The handler in AppDomain data is what the static constructor looks for before it will do
        /// anything, and it refuses to start without one. Production installs it from the client's module
        /// initializer; here it records the exception so a WPF failure inside a dialog can be asserted on
        /// rather than tearing down the run.
        /// </remarks>
        /// <returns>The dispatcher for the apartment <see cref="DialogManager"/> started.</returns>
        /// <exception cref="InvalidOperationException">Thrown if DialogManager started without leaving an application behind.</exception>
        [SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "The ban steers production code to the dialog manager's own marshalling helpers. This is the one place that has to read the application the dialog manager created, in order to adopt its dispatcher.")]
        private static Dispatcher Boot()
        {
            AppDomain.CurrentDomain.SetData(
                "PSADT.UserInterface.DialogManager.UnhandledExceptionHandler",
                static void (Exception ex) => UnhandledDispatcherException = ex);

            // Naming any member is enough to run the static constructor. ProgressDialogOpen is the
            // cheapest one that neither throws nor shows anything.
            _ = DialogManager.ProgressDialogOpen();

            return System.Windows.Application.Current?.Dispatcher
                ?? throw new InvalidOperationException("DialogManager started without leaving an application behind.");
        }

        /// <summary>
        /// The last exception WPF reported through the dialog manager's unhandled exception handler.
        /// </summary>
        /// <remarks>
        /// Recorded rather than rethrown because the handler runs on the dispatcher thread, where a throw
        /// would take down the apartment every remaining test needs.
        /// </remarks>
        public static Exception? UnhandledDispatcherException { get; private set; }
    }
}
