using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using PSADT.PathEx;
using PSADT.Impersonation;


namespace PSADT.PowerShellHost
{
    /// <summary>
    /// Provides methods to execute PowerShell scripts with various configuration options.
    /// </summary>
    public static class PSADTShell
    {
        /// <summary>
        /// Executes a PowerShell script synchronously using the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing configuration options, impersonator, and cancellation token.</param>
        /// <returns>A collection of <see cref="PSObject"/> representing the output of the PowerShell script.</returns>
        public static PSDataCollection<PSObject> Execute(ExecutionContext context)
        {
            return ExecuteAsync(context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a PowerShell script asynchronously using the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing configuration options, impersonator, and cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a collection of <see cref="PSObject"/> as the result.</returns>
        public static async Task<PSDataCollection<PSObject>> ExecuteAsync(ExecutionContext context)
        {
            ValidateOptions(context.Options);

            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            await Task.Run(async () =>
            {
                await ExecuteWithMTAAsync(async () =>
                {
                    using Runspace runspace = CreateRunspace(context);
                    using PowerShell powershell = System.Management.Automation.PowerShell.Create();
                    powershell.Runspace = runspace;

                    ConfigureExecutionPolicy(powershell, context.Options);
                    ConfigureEnvironment(powershell, context);

                    try
                    {
                        output = await InvokePowerShellAsync(powershell, context).ConfigureAwait(false);

                        if (context.Options.CollectStreams)
                        {
                            CollectStreams(powershell, ref output);
                        }

                        if (context.Options.ErrorActionPreference == ActionPreference.Stop && powershell.HadErrors)
                        {
                            throw new InvalidOperationException("PowerShell execution encountered errors.");
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("PowerShell invocation failed.", ex);
                    }
                }, context.Impersonator);
            }, context.CancellationToken).ConfigureAwait(false);

            return output;
        }

        /// <summary>
        /// Executes an action within a new thread configured to use the Multi-Threaded Apartment (MTA) model asynchronously.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="impersonator">The impersonator object used to run the action under a different security context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private static async Task ExecuteWithMTAAsync(
            Func<Task> action,
            Impersonator? impersonator = null)
        {
            Exception? exception = null;
            var taskCompletionSource = new TaskCompletionSource<object?>();

            var thread = new Thread(async () =>
            {
                try
                {
                    if (impersonator != null)
                    {
                        impersonator.Impersonate(async () =>
                        {
                            await action().ConfigureAwait(false);
                        });
                    }
                    else
                    {
                        await action().ConfigureAwait(false);
                    }

                    taskCompletionSource.SetResult(null);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    taskCompletionSource.SetException(exception);
                }
            });

            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();

            await taskCompletionSource.Task.ConfigureAwait(false);

            if (exception != null)
            {
                throw new InvalidOperationException("Execution in MTA thread failed.", exception);
            }
        }

        /// <summary>
        /// Executes an action within a new thread configured to use the Multi-Threaded Apartment (MTA) model synchronously.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="InvalidOperationException">Thrown if the action execution encounters errors.</exception>
        public static void ExecuteWithMTA(Action action)
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw new InvalidOperationException("Execution in MTA thread failed.", exception);
            }
        }


        /// <summary>
        /// Invokes the PowerShell script asynchronously using the provided PowerShell instance and execution context.
        /// </summary>
        /// <param name="powershell">The <see cref="PowerShell"/> instance to use for script execution.</param>
        /// <param name="context">The execution context containing configuration options and cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a collection of <see cref="PSObject"/> as the result.</returns>
        private static async Task<PSDataCollection<PSObject>> InvokePowerShellAsync(
            PowerShell powershell,
            ExecutionContext context)
        {
            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            if (context.Options.Timeout.HasValue)
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                cts.CancelAfter(context.Options.Timeout.Value);
                using (cts.Token.Register(() => powershell.Stop()))
                {
                    await powershell.InvokeAsync<PSObject, PSObject>(null, output, null, null, null).ConfigureAwait(false);
                }
            }
            else
            {
                await powershell.InvokeAsync<PSObject, PSObject>(null, output, null, null, null).ConfigureAwait(false);
            }

            return output;
        }

        /// <summary>
        /// Creates and configures a PowerShell runspace based on the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing configuration options.</param>
        /// <returns>A configured <see cref="Runspace"/> object.</returns>
        private static Runspace CreateRunspace(ExecutionContext context)
        {
            Runspace runspace;

            if (context.Options.IsOutOfProcessRunspace == true)
            {
                string psFilePath = GetPowerShellPath(context.Options);
                var psi = new PowerShellProcessInstance
                {
                    Process =
                    {
                        StartInfo = { FileName = psFilePath }
                    }
                };

                runspace = RunspaceFactory.CreateOutOfProcessRunspace(TypeTable.LoadDefaultTypeFiles(), psi);
            }
            else
            {
                InitialSessionState iss = context.Options.InitialSessionState ?? InitialSessionState.CreateDefault();
                ConfigureInitialSessionState(iss, context.Options);
                runspace = RunspaceFactory.CreateRunspace(context.Options.PSADTHost ?? new DefaultHost(), iss);
            }

            runspace.ThreadOptions = context.Options.ThreadOptions;
            if (context.Options.ApartmentState != ApartmentState.Unknown)
            {
                runspace.ApartmentState = context.Options.ApartmentState;
            }

            runspace.Open();
            return runspace;
        }

        /// <summary>
        /// Configures the execution policy for the PowerShell session.
        /// </summary>
        /// <param name="powershell">The <see cref="PowerShell"/> instance to configure.</param>
        /// <param name="options">The execution options containing execution policy settings.</param>
        private static void ConfigureExecutionPolicy(
            PowerShell powershell,
            PSExecutionOptions options)
        {
            powershell.AddCommand("Set-ExecutionPolicy")
                      .AddParameter("ExecutionPolicy", options.ExecutionPolicy)
                      .AddParameter("Scope", options.ExecutionPolicyScope)
                      .AddParameter("Force", options.ForceExecutionPolicy)
                      .AddParameter("ErrorAction", options.ExecutionPolicyErrorAction)
                      .Invoke();
            powershell.Commands.Clear();
        }

        /// <summary>
        /// Configures the environment for the PowerShell session based on the execution context.
        /// </summary>
        /// <param name="context">The execution context containing configuration options.</param>
        /// <param name="powershell">The <see cref="PowerShell"/> instance to configure.</param>
        private static void ConfigureEnvironment(
            PowerShell powershell,
            ExecutionContext context)
        {
            if (!string.IsNullOrEmpty(context.Options.WorkingDirectory))
            {
                powershell.AddCommand("Set-Location").AddParameter("Path", context.Options.WorkingDirectory).Invoke();
                powershell.Commands.Clear();
            }

            if (context.Options.Culture != null)
            {
                powershell.AddScript($"[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('{context.Options.Culture.Name}')").Invoke();
                powershell.Commands.Clear();
            }

            if (context.Options.UICulture != null)
            {
                powershell.AddScript($"[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('{context.Options.UICulture.Name}')").Invoke();
                powershell.Commands.Clear();
            }

            LoadAssemblies(context.Options, powershell);
        }

        /// <summary>
        /// Loads the specified assemblies into the PowerShell session using Add-Type.
        /// </summary>
        /// <param name="options">The execution options containing assembly loading settings.</param>
        /// <param name="powershell">The <see cref="PowerShell"/> instance to configure.</param>
        private static void LoadAssemblies(PSExecutionOptions options, PowerShell powershell)
        {
            if (options.AssembliesToLoad != null)
            {
                foreach (var assembly in options.AssembliesToLoad)
                {
                    powershell.AddCommand("Add-Type")
                              .AddParameter("Path", assembly)
                              .Invoke();
                    powershell.Commands.Clear();
                }
            }
        }

        /// <summary>
        /// Configures the initial session state for the PowerShell runspace.
        /// </summary>
        /// <param name="iss">The <see cref="InitialSessionState"/> to configure.</param>
        /// <param name="options">The execution options containing session state configuration settings.</param>
        private static void ConfigureInitialSessionState(
            InitialSessionState iss,
            PSExecutionOptions options)
        {
            if (options.ModulesToImport != null)
            {
                foreach (var module in options.ModulesToImport)
                {
                    iss.ImportPSModule(module);
                }
            }

            if (options.Variables != null)
            {
                foreach (var variable in options.Variables)
                {
                    iss.Variables.Add(new SessionStateVariableEntry(variable.Key, variable.Value, string.Empty));
                }
            }

            if (options.Functions != null)
            {
                foreach (var function in options.Functions)
                {
                    iss.Commands.Add(new SessionStateFunctionEntry(function.Key, function.Value));
                }
            }
        }

        /// <summary>
        /// Collects additional streams (Error, Warning, Verbose, Debug, Information) from the PowerShell execution.
        /// </summary>
        /// <param name="powershell">The <see cref="PowerShell"/> instance from which to collect streams.</param>
        /// <param name="output">The collection to which the stream outputs will be added.</param>
        private static void CollectStreams(
            PowerShell powershell,
            ref PSDataCollection<PSObject> output)
        {
            output.Add(new PSObject(powershell.Streams.Error));
            output.Add(new PSObject(powershell.Streams.Warning));
            output.Add(new PSObject(powershell.Streams.Verbose));
            output.Add(new PSObject(powershell.Streams.Debug));
            output.Add(new PSObject(powershell.Streams.Information));
        }

        /// <summary>
        /// Validates the execution options to ensure that required fields are set.
        /// </summary>
        /// <param name="options">The execution options to validate.</param>
        /// <exception cref="ArgumentException">Thrown if neither ScriptPath nor ScriptText is provided.</exception>
        private static void ValidateOptions(PSExecutionOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.ScriptText) && string.IsNullOrWhiteSpace(options.ScriptPath))
            {
                throw new ArgumentException("Either ScriptPath or ScriptText must be provided in the options.");
            }

            if (!string.IsNullOrWhiteSpace(options.ScriptPath))
            {
                options.ScriptText = File.ReadAllText(options.ScriptPath);
            }
        }

        /// <summary>
        /// Gets the appropriate PowerShell executable path based on the provided execution options.
        /// </summary>
        /// <param name="options">The execution options containing the PowerShell version and architecture preferences.</param>
        /// <returns>The path to the PowerShell executable based on the specified options.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified PowerShell executable cannot be found.</exception>
        public static string GetPowerShellPath(PSExecutionOptions options)
        {
            string psFilePath = options.PowerShellVersion switch
            {
                PSEdition.WindowsPowerShell => GetWindowsPowerShellPath(options.PSArchitecture),
                PSEdition.PowerShellCore => GetPowerShellCorePath(options.PSArchitecture),
                _ => GetDefaultPowerShellPath(options.PSArchitecture)
            };

            return psFilePath;
        }


        /// <summary>
        /// Gets the path to the PowerShell Core executable based on the specified architecture.
        /// </summary>
        /// <param name="architecture">The architecture to use (x64 or x86).</param>
        /// <returns>The path to the PowerShell Core executable.</returns>
        private static string GetPowerShellCorePath(PSArchitecture architecture)
        {
            string programFiles = architecture switch
            {
                PSArchitecture.X86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                PSArchitecture.X64 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                PSArchitecture.CurrentProcess => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                _ => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
            };

            string psCorePath = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");

            if (!File.Exists(psCorePath))
            {
                psCorePath = Path.Combine(programFiles, "PowerShell", "6", "pwsh.exe");
            }

            if (!File.Exists(psCorePath))
            {
                psCorePath = Path.Combine(programFiles, "PowerShell", "pwsh.exe");
            }

            if (!File.Exists(psCorePath))
            {
                psCorePath = PathHelper.ResolveExecutableFullPath("pwsh.exe") ?? throw new FileNotFoundException("PowerShell Core executable [pwsh.exe] not found.", psCorePath);
            }

            return psCorePath;
        }

        /// <summary>
        /// Gets the path to the Windows PowerShell executable based on the specified architecture.
        /// </summary>
        /// <param name="architecture">The architecture to use (x64 or x86).</param>
        /// <returns>The path to the Windows PowerShell executable.</returns>
        private static string GetWindowsPowerShellPath(PSArchitecture architecture)
        {
            string psFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe");

            switch (architecture)
            {
                case PSArchitecture.X64:
                    if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                    {
                        psFilePath = psFilePath.Replace(@"\SysWOW64\", @"\Sysnative\", StringComparison.OrdinalIgnoreCase);
                    }
                    break;
                case PSArchitecture.X86:
                    if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
                    {
                        psFilePath = psFilePath.Replace(@"\System32\", @"\SysWOW64\", StringComparison.OrdinalIgnoreCase);
                    }
                    break;
                case PSArchitecture.CurrentProcess:
                    break;
            }

            if (!File.Exists(psFilePath))
            {
                psFilePath = PathHelper.ResolveExecutableFullPath("powershell.exe") ?? throw new FileNotFoundException("Windows PowerShell executable [powershell.exe] not found.", psFilePath);
            }

            return psFilePath;
        }

        /// <summary>
        /// Gets the default PowerShell executable path based on the current runtime environment.
        /// </summary>
        /// <param name="architecture">The architecture to use (x64 or x86).</param>
        /// <returns>The default PowerShell executable path.</returns>
        private static string GetDefaultPowerShellPath(PSArchitecture architecture)
        {
            if (Environment.Version.Major >= 5)
            {
                return GetPowerShellCorePath(architecture);
            }

            return GetWindowsPowerShellPath(architecture);
        }
    }
}
