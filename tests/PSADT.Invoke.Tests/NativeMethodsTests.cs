using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;
using Windows.Win32.System.Threading;
using Xunit;

namespace PSADT.Invoke.Tests
{
    /// <summary>
    /// Tests the launcher's wrappers over the native functions it runs PowerShell with.
    /// </summary>
    public sealed class NativeMethodsTests
    {
        /// <summary>
        /// How long a started process is given to exit. A guard against a hang, not an assertion about speed.
        /// </summary>
        private const uint ProcessTimeoutMilliseconds = 120000;

        /// <summary>
        /// The exit code GetExitCodeProcess reports for a process that is still running.
        /// </summary>
        private const uint StillActive = 259;

        private const int ErrorFileNotFound = 2;
        private const int ErrorInvalidHandle = 6;
        private const int ErrorInvalidParameter = 87;

        /// <summary>
        /// A handler routine that leaves every event to the next one. Removal must pass the same instance that was added.
        /// </summary>
        private static readonly PHANDLER_ROUTINE PassThroughHandlerRoutine = static _ => false;

        /// <summary>
        /// Verifies that a created process runs its command line, and that waiting on it then yields its exit code.
        /// </summary>
        [Fact]
        public void CreateProcess_RunsTheCommandLine()
        {
            string cmdPath = Path.Join(Environment.SystemDirectory, "cmd.exe");
            STARTUPINFOW startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };
            _ = NativeMethods.CreateProcess(cmdPath, $"\"{cmdPath}\" /c exit 3\0".ToCharArray(), bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW, Environment.SystemDirectory, in startupInfo, out PROCESS_INFORMATION pi);
            using SafeProcessHandle hProcess = new(pi.hProcess, ownsHandle: true);
            using SafeWaitHandle hThread = new(pi.hThread, ownsHandle: true);

            Assert.Equal(WAIT_EVENT.WAIT_OBJECT_0, NativeMethods.WaitForSingleObject(hProcess, ProcessTimeoutMilliseconds));
            _ = NativeMethods.GetExitCodeProcess(hProcess, out uint exitCode);
            Assert.Equal(3u, exitCode);
        }

        /// <summary>
        /// Verifies that a module that does not exist surfaces the native failure.
        /// </summary>
        [Fact]
        public void CreateProcess_ThrowsForAMissingModule()
        {
            string modulePath = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".exe");
            STARTUPINFOW startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };

            Win32Exception ex = Assert.Throws<Win32Exception>(() => NativeMethods.CreateProcess(modulePath, $"\"{modulePath}\"\0".ToCharArray(), bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW, Environment.SystemDirectory, in startupInfo, out _));
            Assert.Equal(ErrorFileNotFound, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a command line without a terminator is refused before the native call can read past its end.
        /// </summary>
        [Fact]
        public void CreateProcess_ThrowsForAnUnterminatedCommandLine()
        {
            string cmdPath = Path.Join(Environment.SystemDirectory, "cmd.exe");
            STARTUPINFOW startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };

            ArgumentException ex = Assert.Throws<ArgumentException>(() => NativeMethods.CreateProcess(cmdPath, $"\"{cmdPath}\" /c exit 3".ToCharArray(), bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW, Environment.SystemDirectory, in startupInfo, out _));
            Assert.Equal("lpCommandLine", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a blank module path is refused.
        /// </summary>
        [Fact]
        public void CreateProcess_ThrowsForABlankApplicationName()
        {
            STARTUPINFOW startupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };

            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => NativeMethods.CreateProcess(" ", "\0".ToCharArray(), bInheritHandles: false, PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW, Environment.SystemDirectory, in startupInfo, out _));
            Assert.Equal("lpApplicationName", ex.ParamName);
        }

        /// <summary>
        /// Verifies that waiting on a process that is still running reports the time-out.
        /// </summary>
        [Fact]
        public void WaitForSingleObject_ReportsTimeoutForARunningProcess()
        {
            using SafeFileHandle hProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE, bInheritHandle: false, PInvoke.GetCurrentProcessId());

            Assert.Equal(WAIT_EVENT.WAIT_TIMEOUT, NativeMethods.WaitForSingleObject(hProcess, 0));
        }

        /// <summary>
        /// Verifies that waiting on an invalid handle surfaces the native failure.
        /// </summary>
        [Fact]
        public void WaitForSingleObject_ThrowsForAnInvalidHandle()
        {
            using SafeWaitHandle hHandle = new(IntPtr.Zero, ownsHandle: false);

            Win32Exception ex = Assert.Throws<Win32Exception>(() => NativeMethods.WaitForSingleObject(hHandle, 0));
            Assert.Equal(ErrorInvalidHandle, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a process that is still running reports STILL_ACTIVE.
        /// </summary>
        [Fact]
        public void GetExitCodeProcess_ReportsStillActiveForARunningProcess()
        {
            using SafeFileHandle hProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, PInvoke.GetCurrentProcessId());

            _ = NativeMethods.GetExitCodeProcess(hProcess, out uint exitCode);
            Assert.Equal(StillActive, exitCode);
        }

        /// <summary>
        /// Verifies that querying an invalid handle surfaces the native failure.
        /// </summary>
        [Fact]
        public void GetExitCodeProcess_ThrowsForAnInvalidHandle()
        {
            using SafeWaitHandle hProcess = new(IntPtr.Zero, ownsHandle: false);

            Win32Exception ex = Assert.Throws<Win32Exception>(() => NativeMethods.GetExitCodeProcess(hProcess, out _));
            Assert.Equal(ErrorInvalidHandle, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a handler routine can be added and then removed again.
        /// </summary>
        [Fact]
        public void SetConsoleCtrlHandler_AddsAndRemovesAHandlerRoutine()
        {
            Assert.True(NativeMethods.SetConsoleCtrlHandler(PassThroughHandlerRoutine, Add: true));
            Assert.True(NativeMethods.SetConsoleCtrlHandler(PassThroughHandlerRoutine, Add: false));
        }

        /// <summary>
        /// Verifies that removing a handler routine that was never added surfaces the native failure.
        /// </summary>
        [Fact]
        public void SetConsoleCtrlHandler_ThrowsForAHandlerRoutineNeverAdded()
        {
            Win32Exception ex = Assert.Throws<Win32Exception>(static () => NativeMethods.SetConsoleCtrlHandler(static _ => false, Add: false));
            Assert.Equal(ErrorInvalidParameter, ex.NativeErrorCode);
        }
    }
}
