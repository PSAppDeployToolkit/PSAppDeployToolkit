using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Verifies dispatcher shutdown in disposable child processes so the shared dialog host stays alive.
    /// </summary>
    public sealed class DialogManagerShutdownTests
    {
        /// <summary>
        /// Verifies actual process-exit ordering, an existing shutdown, and invocation from the UI thread.
        /// </summary>
        /// <param name="mode">The isolated shutdown scenario.</param>
        /// <param name="expectedMarker">The result written by the child after the required condition holds.</param>
        [Theory]
        [InlineData("process-exit", "joined")]
        [InlineData("already-started", "started-joined")]
        [InlineData("ui-thread", "self-returned;joined")]
        public async Task Shutdown_CompletesInIsolatedChildAsync(string mode, string expectedMarker)
        {
            string windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string powershellPath = Path.Join(windowsDirectory, "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
            Assert.True(File.Exists(powershellPath), $"Windows PowerShell is required for the net472 child host: {powershellPath}");

            string directory = Path.Join(Path.GetTempPath(), "PSADT-shutdown-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string scriptPath = Path.Join(directory, "child.ps1");
            string markerPath = Path.Join(directory, "result.txt");
            File.WriteAllText(scriptPath, ChildScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // The parent process only passes the DLL path. It never initializes or shuts down its DialogManager.
            string assemblyPath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "PSADT.UserInterface.Interfaces.dll");
            Assert.True(File.Exists(assemblyPath), $"The net472 UI assembly was not found: {assemblyPath}");
            string arguments = "-NoProfile -NonInteractive -STA -ExecutionPolicy Bypass -File " + Quote(scriptPath)
                + " -AssemblyPath " + Quote(assemblyPath)
                + " -MarkerPath " + Quote(markerPath)
                + " -Mode " + Quote(mode);
            using Process child = new()
            {
                StartInfo = new ProcessStartInfo(powershellPath, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(assemblyPath) ?? directory,
                },
            };

            bool started = false;
            try
            {
                started = child.Start();
                Assert.True(started, $"Unable to start child process for {mode}.");
                Task<string> outputTask = child.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
                Task<string> errorTask = child.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
                bool exited = child.WaitForExit(milliseconds: 10000);
                if (!exited)
                {
                    child.Kill();
                    _ = child.WaitForExit(milliseconds: 2000);
                }

                string output = await outputTask;
                string error = await errorTask;
                Assert.True(exited, $"Child timed out in {mode}; temporary directory: {directory}. stdout: {output} stderr: {error}");
                Assert.True(child.ExitCode is 0, $"Child failed in {mode} with exit {child.ExitCode.ToString(CultureInfo.InvariantCulture)}. stdout: {output} stderr: {error}");
                Assert.True(File.Exists(markerPath), $"Child did not write its {mode} completion marker. stdout: {output} stderr: {error}");
                Assert.Equal(expectedMarker, File.ReadAllText(markerPath));
            }
            finally
            {
                if (started && !ProcessUtilities.HasProcessExited(child))
                {
                    child.Kill();
                    _ = child.WaitForExit(milliseconds: 2000);
                }
                if (!started || ProcessUtilities.HasProcessExited(child))
                {
                    File.Delete(scriptPath);
                    File.Delete(markerPath);
                    Directory.Delete(directory);
                }
            }
        }

        private static string Quote(string value)
        {
            return '"' + value.Replace("\"", "\\\"", StringComparison.Ordinal) + '"';
        }

        private static readonly string ChildScript = string.Join(Environment.NewLine,
            "param(",
            "    [Parameter(Mandatory = $true)][string]$AssemblyPath,",
            "    [Parameter(Mandatory = $true)][string]$MarkerPath,",
            "    [Parameter(Mandatory = $true)][ValidateSet('process-exit', 'already-started', 'ui-thread')][string]$Mode",
            ")",
            "$ErrorActionPreference = 'Stop'",
            "if ($ExecutionContext.SessionState.LanguageMode -ne 'FullLanguage') {",
            "    throw 'The isolated shutdown probe requires a FullLanguage Windows PowerShell child.'",
            "}",
            "",
            "Add-Type -AssemblyName PresentationFramework",
            "Add-Type -TypeDefinition @'",
            "using System;",
            "using System.IO;",
            "using System.Reflection;",
            "using System.Threading;",
            "",
            "public static class ShutdownProbe",
            "{",
            "    public static Thread Owner;",
            "    public static string MarkerPath;",
            "    public static MethodInfo ExitMethod;",
            "    public static readonly ManualResetEventSlim ShutdownStarted = new ManualResetEventSlim(false);",
            "    public static readonly ManualResetEventSlim ReleaseShutdown = new ManualResetEventSlim(false);",
            "",
            "    public static void ObserveExit(object sender, EventArgs e)",
            "    {",
            "        File.WriteAllText(MarkerPath, Owner != null && !Owner.IsAlive ? \"joined\" : \"alive\");",
            "    }",
            "",
            "    public static void ObserveBeforeExit(object sender, EventArgs e)",
            "    {",
            "        File.WriteAllText(MarkerPath, \"before:alive=\" + Owner.IsAlive);",
            "    }",
            "",
            "    public static void RegisterBeforeObserver()",
            "    {",
            "        AppDomain.CurrentDomain.ProcessExit += ObserveBeforeExit;",
            "    }",
            "",
            "    public static void RegisterExitObserver()",
            "    {",
            "        AppDomain.CurrentDomain.ProcessExit += ObserveExit;",
            "    }",
            "",
            "    public static void HoldShutdown(object sender, EventArgs e)",
            "    {",
            "        ShutdownStarted.Set();",
            "        ReleaseShutdown.Wait(TimeSpan.FromSeconds(5));",
            "    }",
            "",
            "    public static void RegisterShutdownHold(object dispatcher)",
            "    {",
            "        EventInfo shutdownFinished = dispatcher.GetType().GetEvent(\"ShutdownFinished\");",
            "        shutdownFinished.AddEventHandler(dispatcher, new EventHandler(HoldShutdown));",
            "    }",
            "",
            "    public static void ReleaseSoon()",
            "    {",
            "        Thread release = new Thread(() => { Thread.Sleep(150); ReleaseShutdown.Set(); });",
            "        release.IsBackground = true;",
            "        release.Start();",
            "    }",
            "",
            "    public static void InvokeFromUi()",
            "    {",
            "        ExitMethod.Invoke(null, new object[] { null, EventArgs.Empty });",
            "        File.WriteAllText(MarkerPath, \"self-returned\");",
            "    }",
            "",
            "    public static void InvokeOnUi(object dispatcher)",
            "    {",
            "        MethodInfo invoke = dispatcher.GetType().GetMethod(\"Invoke\", new[] { typeof(Action) });",
            "        invoke.Invoke(dispatcher, new object[] { new Action(InvokeFromUi) });",
            "    }",
            "}",
            "'@",
            "",
            "$assembly = [System.Reflection.Assembly]::LoadFrom($AssemblyPath)",
            "$type = $assembly.GetType('PSADT.UserInterface.Interfaces.DialogManager', $true)",
            "$flags = [System.Reflection.BindingFlags]::Static -bor [System.Reflection.BindingFlags]::NonPublic",
            "[AppDomain]::CurrentDomain.SetData('PSADT.UserInterface.DialogManager.UnhandledExceptionHandler', [Action[Exception]] { param($exception) throw $exception })",
            "if ($Mode -eq 'process-exit') {",
            "    [ShutdownProbe]::MarkerPath = $MarkerPath",
            "    [ShutdownProbe]::RegisterBeforeObserver()",
            "}",
            "$null = $type.GetMethod('ProgressDialogOpen', $flags).Invoke($null, @())",
            "$thread = [System.Threading.Thread]$type.GetField('appThread', $flags).GetValue($null)",
            "$dispatcher = [System.Windows.Application]::Current.Dispatcher",
            "$exitMethod = $type.GetMethod('OnProcessExit', $flags)",
            "if ($null -eq $thread -or $null -eq $dispatcher -or $null -eq $exitMethod) {",
            "    throw 'The dedicated thread, dispatcher, or shutdown handler was not initialized.'",
            "}",
            "",
            "if ($Mode -eq 'process-exit') {",
            "    [ShutdownProbe]::Owner = $thread",
            "    [ShutdownProbe]::MarkerPath = $MarkerPath",
            "    [ShutdownProbe]::RegisterExitObserver()",
            "    [Environment]::Exit(0)",
            "}",
            "",
            "if ($Mode -eq 'already-started') {",
            "    [ShutdownProbe]::RegisterShutdownHold($dispatcher)",
            "    $dispatcher.BeginInvokeShutdown([System.Windows.Threading.DispatcherPriority]::Send)",
            "    if (-not [ShutdownProbe]::ShutdownStarted.Wait(5000)) {",
            "        throw 'The dispatcher did not enter ShutdownStarted.'",
            "    }",
            "    if (-not $thread.IsAlive -or -not $dispatcher.HasShutdownStarted) {",
            "        Write-Output ('alive={0}; started={1}; finished={2}' -f $thread.IsAlive, $dispatcher.HasShutdownStarted, $dispatcher.HasShutdownFinished)",
            "        throw 'ShutdownStarted did not hold a live dispatcher thread.'",
            "    }",
            "    [ShutdownProbe]::ReleaseSoon()",
            "    $null = $exitMethod.Invoke($null, @($null, [EventArgs]::Empty))",
            "    if ($thread.IsAlive) {",
            "        throw 'OnProcessExit returned before the already-shutting-down thread exited.'",
            "    }",
            "    [IO.File]::WriteAllText($MarkerPath, 'started-joined')",
            "    exit 0",
            "}",
            "",
            "[ShutdownProbe]::ExitMethod = $exitMethod",
            "[ShutdownProbe]::MarkerPath = $MarkerPath",
            "[ShutdownProbe]::InvokeOnUi($dispatcher)",
            "if (-not $thread.Join(5000)) {",
            "    throw 'The UI thread did not exit after its own OnProcessExit invocation.'",
            "}",
            "if ([IO.File]::ReadAllText($MarkerPath) -ne 'self-returned') {",
            "    throw 'The UI-thread invocation did not return from OnProcessExit.'",
            "}",
            "[IO.File]::WriteAllText($MarkerPath, 'self-returned;joined')",
            "exit 0");
    }
}
