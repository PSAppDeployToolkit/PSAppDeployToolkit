using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;
using Windows.Win32.System.Threading;
using Windows.Win32.UI.WindowsAndMessaging;
using Xunit;

namespace PSADT.Invoke.Tests
{
    /// <summary>
    /// Tests the launcher's wrappers over the native functions it calls.
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
        private const int ErrorAccessDenied = 5;
        private const int ErrorInvalidHandle = 6;
        private const int ErrorInvalidParameter = 87;
        private const int ErrorInvalidMessageBoxStyle = 1438;

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

        /// <summary>
        /// Verifies that the current process can be opened.
        /// </summary>
        [Fact]
        public void OpenProcess_OpensTheCurrentProcess()
        {
            using SafeFileHandle hProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, PInvoke.GetCurrentProcessId());

            Assert.False(hProcess.IsInvalid);
        }

        /// <summary>
        /// Verifies that opening the System Idle Process surfaces the native failure.
        /// </summary>
        [Fact]
        public void OpenProcess_ThrowsForTheIdleProcess()
        {
            Win32Exception ex = Assert.Throws<Win32Exception>(static () => NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, 0).Dispose());
            Assert.Equal(ErrorInvalidParameter, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that the basic information of the current process reports its own ID.
        /// </summary>
        [Fact]
        public void NtQueryInformationProcess_ReportsTheProcessId()
        {
            using SafeFileHandle hProcess = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, PInvoke.GetCurrentProcessId());

            Assert.Equal(0, NativeMethods.NtQueryInformationProcess(hProcess, out PROCESS_BASIC_INFORMATION pbi));
            Assert.Equal(PInvoke.GetCurrentProcessId(), pbi.UniqueProcessId);
        }

        /// <summary>
        /// Verifies that querying an invalid handle surfaces the native failure.
        /// </summary>
        [Fact]
        public void NtQueryInformationProcess_ThrowsForAnInvalidHandle()
        {
            using SafeWaitHandle hProcess = new(IntPtr.Zero, ownsHandle: false);

            Win32Exception ex = Assert.Throws<Win32Exception>(() => NativeMethods.NtQueryInformationProcess(hProcess, out _));
            Assert.Equal(ErrorInvalidHandle, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that blank message text is refused.
        /// </summary>
        [Fact]
        public void MessageBox_ThrowsForBlankText()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(static () => NativeMethods.MessageBox(hWnd: null, " ", "Caption", MESSAGEBOX_STYLE.MB_OK));
            Assert.Equal("lpText", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a blank caption is refused.
        /// </summary>
        [Fact]
        public void MessageBox_ThrowsForBlankCaption()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(static () => NativeMethods.MessageBox(hWnd: null, "Text", " ", MESSAGEBOX_STYLE.MB_OK));
            Assert.Equal("lpCaption", ex.ParamName);
        }

        /// <summary>
        /// Verifies that a style naming no valid message box type surfaces the native failure, which comes before
        /// anything is shown.
        /// </summary>
        [Fact]
        public void MessageBox_ThrowsForAnInvalidStyle()
        {
            Win32Exception ex = Assert.Throws<Win32Exception>(static () => NativeMethods.MessageBox(hWnd: null, "Text", "Caption", MESSAGEBOX_STYLE.MB_TYPEMASK));
            Assert.Equal(ErrorInvalidMessageBoxStyle, ex.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a process can allocate a console once it has left its own, and that the new console has a window.
        /// </summary>
        [Fact]
        public void AllocConsole_AllocatesAConsole()
        {
            int?[] expected = [null, null, null];

            Assert.Equal(expected, CallInHiddenPowerShell(nameof(NativeMethods.FreeConsole), nameof(NativeMethods.AllocConsole), nameof(NativeMethods.GetConsoleWindow)));
        }

        /// <summary>
        /// Verifies that allocating a console while attached to one surfaces the native failure.
        /// </summary>
        [Fact]
        public void AllocConsole_ThrowsWhenAlreadyAttached()
        {
            int?[] expected = [ErrorAccessDenied];

            Assert.Equal(expected, CallInHiddenPowerShell(nameof(NativeMethods.AllocConsole)));
        }

        /// <summary>
        /// Verifies that a process leaves its console, after which it has no console window.
        /// </summary>
        [Fact]
        public void FreeConsole_DetachesFromTheConsole()
        {
            int?[] expected = [null, ErrorInvalidHandle];

            Assert.Equal(expected, CallInHiddenPowerShell(nameof(NativeMethods.FreeConsole), nameof(NativeMethods.GetConsoleWindow)));
        }

        /// <summary>
        /// Verifies that the window of the console a process is attached to is returned.
        /// </summary>
        [Fact]
        public void GetConsoleWindow_ReturnsTheConsoleWindow()
        {
            int?[] expected = [null];

            Assert.Equal(expected, CallInHiddenPowerShell(nameof(NativeMethods.GetConsoleWindow)));
        }

        /// <summary>
        /// Calls parameterless native methods of the launcher in a hidden Windows PowerShell, as the console they change
        /// belongs to the whole process.
        /// </summary>
        /// <remarks>The child loads a copy of the launcher by reflection, as its types are internal. ShellExecute passes
        /// SW_HIDE on where the framework otherwise drops it, so a console the child allocates is hidden too.</remarks>
        /// <param name="methodNames">The methods to call, in order.</param>
        /// <returns>For each call, null if it succeeded, otherwise the native error code it failed with.</returns>
        /// <exception cref="InvalidOperationException">Thrown if Windows PowerShell cannot be started.</exception>
        private static int?[] CallInHiddenPowerShell(params string[] methodNames)
        {
            string assemblyPath = Path.GetTempFileName();
            string resultsPath = Path.GetTempFileName();
            try
            {
                File.Copy(typeof(NativeMethods).Assembly.Location, assemblyPath, overwrite: true);
                string script = string.Join(
                    Environment.NewLine,
                    "$ErrorActionPreference = 'Stop'",
                    $"$type = [System.Reflection.Assembly]::LoadFrom('{assemblyPath.Replace("'", "''")}').GetType('{typeof(NativeMethods).FullName}', $true)",
                    $"foreach ($name in {string.Join(", ", methodNames.Select(static name => "'" + name + "'"))})",
                    "{",
                    "    try",
                    "    {",
                    "        $null = $type.GetMethod($name, [System.Reflection.BindingFlags]'NonPublic, Static').Invoke($null, $null)",
                    "        $result = 'OK'",
                    "    }",
                    "    catch",
                    "    {",
                    "        $exception = $_.Exception",
                    "        while ($exception -is [System.Management.Automation.MethodInvocationException] -or $exception -is [System.Reflection.TargetInvocationException])",
                    "        {",
                    "            $exception = $exception.InnerException",
                    "        }",
                    "        $result = $exception.NativeErrorCode",
                    "    }",
                    $"    Add-Content -LiteralPath '{resultsPath.Replace("'", "''")}' -Value $result",
                    "}");
                ProcessStartInfo startInfo = new()
                {
                    FileName = Path.Join(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe"),
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                };
                using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Windows PowerShell.");
                if (!process.WaitForExit((int)ProcessTimeoutMilliseconds))
                {
                    process.Kill();
                    process.WaitForExit();
                    Assert.Fail($"Windows PowerShell did not exit within {ProcessTimeoutMilliseconds}ms.");
                }
                Assert.Equal(0, process.ExitCode);
                return [.. File.ReadAllLines(resultsPath).Select(static line => line.Equals("OK", StringComparison.Ordinal) ? (int?)null : int.Parse(line, CultureInfo.InvariantCulture))];
            }
            finally
            {
                File.Delete(assemblyPath);
                File.Delete(resultsPath);
            }
        }
    }
}
