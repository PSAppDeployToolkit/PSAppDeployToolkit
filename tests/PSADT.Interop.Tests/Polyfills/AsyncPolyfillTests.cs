using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PSADT.Interop.Tests.Polyfills
{
    /// <summary>
    /// Tests the asynchronous polyfills: Task.WaitAsync, CancellationTokenSource.CancelAsync,
    /// StreamReader.ReadLineAsync and Process.WaitForExitAsync.
    /// </summary>
    /// <remarks>
    /// Two of these cannot fully reproduce the framework, because .NET Framework offers no underlying
    /// cancellable primitive to build on. The tests below assert the contract that does hold and name the
    /// limitation where one exists, rather than pretending the shapes are identical.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "RCS1046:Add suffix 'Async' to asynchronous method name", Justification = "Test names describe the scenario under test; the async suffix would obscure them.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Awaiting a task the test itself created is the mechanism under test.")]
    public sealed class AsyncPolyfillTests
    {
        /// <summary>
        /// Verifies that a task which has already completed is returned as-is, regardless of whether the
        /// token could be cancelled.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitAsync_CompletedTask_CompletesImmediately()
        {
            // Arrange
            using CancellationTokenSource source = new();

            // Act & Assert
            Assert.Null(await Record.ExceptionAsync(static () => Task.CompletedTask.WaitAsync(CancellationToken.None)).ConfigureAwait(true));
            Assert.Null(await Record.ExceptionAsync(() => Task.CompletedTask.WaitAsync(source.Token)).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a token already cancelled at the point of the call cancels the wait rather than
        /// waiting for a task that will never finish.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitAsync_AlreadyCancelledToken_Cancels()
        {
            // Arrange
            using CancellationTokenSource source = new();
            await source.CancelAsync().ConfigureAwait(true);
            TaskCompletionSource<bool> pending = new();

            // Act & Assert
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.Task.WaitAsync(source.Token)).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that cancelling after the wait has begun also cancels it, which is the path that needs
        /// a registration rather than an up-front check.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitAsync_CancelledWhileWaiting_Cancels()
        {
            // Arrange
            using CancellationTokenSource source = new();
            TaskCompletionSource<bool> pending = new();
            Task wait = pending.Task.WaitAsync(source.Token);

            // Act
            await source.CancelAsync().ConfigureAwait(true);

            // Assert
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait).ConfigureAwait(true);
        }

        /// <summary>
        /// Verifies that a task completing before any cancellation wins, so the wait does not leak the
        /// cancellation of a token that was never triggered.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitAsync_TaskCompletesFirst_Completes()
        {
            // Arrange
            using CancellationTokenSource source = new();
            TaskCompletionSource<bool> pending = new();
            Task wait = pending.Task.WaitAsync(source.Token);

            // Act
            pending.SetResult(true);

            // Assert
            Assert.Null(await Record.ExceptionAsync(() => wait).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a faulted task surfaces its own exception rather than being masked by the wait.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitAsync_FaultedTask_PropagatesOriginalException()
        {
            // Arrange
            using CancellationTokenSource source = new();
            TaskCompletionSource<bool> pending = new();
            Task wait = pending.Task.WaitAsync(source.Token);

            // Act
            pending.SetException(new InvalidTimeZoneException("expected"));

            // Assert
            InvalidTimeZoneException exception = await Assert.ThrowsAsync<InvalidTimeZoneException>(() => wait).ConfigureAwait(true);
            Assert.Equal("expected", exception.Message);
        }

        /// <summary>
        /// Verifies that CancelAsync cancels the token and runs registered callbacks. The polyfill runs
        /// them synchronously rather than on the thread pool, so a caller depending on the framework's
        /// asynchronous dispatch would see a different ordering; the observable cancellation state, which
        /// is what the toolkit relies on, is the same.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CancelAsync_CancelsTheToken()
        {
            // Arrange
            using CancellationTokenSource source = new();
            bool callbackRan = false;
            using CancellationTokenRegistration registration = source.Token.Register(() => callbackRan = true);

            // Act
            await source.CancelAsync().ConfigureAwait(true);

            // Assert
            Assert.True(source.IsCancellationRequested);
            Assert.True(source.Token.IsCancellationRequested);
            Assert.True(callbackRan);
        }

        /// <summary>
        /// Verifies that cancelling twice is harmless, since disposal paths often cancel defensively.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task CancelAsync_IsIdempotent()
        {
            // Arrange
            using CancellationTokenSource source = new();

            // Act
            await source.CancelAsync().ConfigureAwait(true);
            await source.CancelAsync().ConfigureAwait(true);

            // Assert
            Assert.True(source.IsCancellationRequested);
        }

        /// <summary>
        /// Verifies that lines are read in order across both line-ending forms, and that the end of the
        /// stream is reported as null rather than an empty string, which is the distinction callers loop on.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadLineAsync_ReadsLinesThenReturnsNull()
        {
            // Arrange
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("first\r\nsecond\nthird"));
            using StreamReader reader = new(stream);

            // Act & Assert
            Assert.Equal("first", await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(true));
            Assert.Equal("second", await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(true));
            Assert.Equal("third", await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(true));
            Assert.Null(await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(true));
        }

        /// <summary>
        /// Verifies that a token already cancelled prevents the read from starting. The polyfill checks
        /// cancellation only on entry, because .NET Framework's ReadLineAsync takes no token, so a read
        /// already under way cannot be interrupted; the framework can cancel mid-read.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task ReadLineAsync_AlreadyCancelledToken_DoesNotRead()
        {
            // Arrange
            using MemoryStream stream = new(Encoding.UTF8.GetBytes("first\r\n"));
            using StreamReader reader = new(stream);
            using CancellationTokenSource source = new();
            await source.CancelAsync().ConfigureAwait(true);

            // Act & Assert
            _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await reader.ReadLineAsync(source.Token).ConfigureAwait(true)).ConfigureAwait(true);
            Assert.Equal(0, stream.Position);
        }

        /// <summary>
        /// Verifies that waiting on a process that has already exited completes rather than hanging, which
        /// is the race the framework implementation is careful about and the polyfill mirrors.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S6966:Awaitable method should be used", Justification = "The synchronous wait is what establishes the already-exited state under test.")]
        [Fact]
        public async Task WaitForExitAsync_ProcessAlreadyExited_Completes()
        {
            // Arrange
            using Process process = StartTrivialProcess();
            process.WaitForExit();

            // Act & Assert
            Assert.Null(await Record.ExceptionAsync(() => process.WaitForExitAsync(CancellationToken.None)).ConfigureAwait(true));
            Assert.True(process.HasExited);
        }

        /// <summary>
        /// Verifies that waiting on a running process completes once it exits.
        /// </summary>
        /// <returns>A task that represents the asynchronous test.</returns>
        [Fact]
        public async Task WaitForExitAsync_RunningProcess_CompletesOnExit()
        {
            // Arrange
            using Process process = StartTrivialProcess();

            // Act
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(true);

            // Assert
            Assert.True(process.HasExited);
            Assert.Equal(0, process.ExitCode);
        }

        /// <summary>
        /// Starts a short-lived child process that exits immediately and touches nothing. Spawning the
        /// test's own child is the only way to exercise this member, and it leaves no state behind.
        /// </summary>
        /// <returns>The started process.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the child process could not be started.</exception>
        private static Process StartTrivialProcess()
        {
            ProcessStartInfo startInfo = new("cmd.exe", "/c exit 0")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            return Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the child process.");
        }
    }
}
