using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using PSADT.PathEx;
using PSADT.Logging;


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
        public static PSDataCollection<PSObject> ExecutePS(Shared.ExecutionContext context)
        {
            return ExecutePSAsync(context).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes a PowerShell script asynchronously using the provided execution context.
        /// </summary>
        /// <param name="executionContext">The execution context containing configuration options, impersonator, and cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a collection of <see cref="PSObject"/> as the result.</returns>
        public static async Task<PSDataCollection<PSObject>> ExecutePSAsync(Shared.ExecutionContext executionContext)
        {
            ValidateCommandOptions(executionContext.PSOptions);

            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            await Task.Run(async () =>
            {
                using Runspace runspace = CreateRunspace(executionContext);
                using PowerShell powershell = System.Management.Automation.PowerShell.Create();
                powershell.Runspace = runspace;

                ConfigureExecutionPolicy(powershell, executionContext.PSOptions);
                ConfigureEnvironment(powershell, executionContext);

                Command command = CreateCommand(executionContext.PSOptions);
                powershell.Commands.AddCommand(command);

                try
                {
                    if (executionContext.Impersonator == null)
                    {
                        // Simply execute the provided action directly without impersonation
                        await Shared.ExecuteMethod.RunMethodAsync(async () =>
                        {
                            output = await InvokePowerShellAsync(powershell, executionContext).ConfigureAwait(false);
                        });
                    }
                    else
                    {
                        // Execute the provided action in the MTA thread with impersonation. MTA is required for impersonation if executing this method from PowerShell.
                        await executionContext.Impersonator.RunImpersonatedMethodWithMTAAsync(async () =>
                        {
                            output = await InvokePowerShellAsync(powershell, executionContext).ConfigureAwait(false);
                        });
                    }

                    if (executionContext.PSOptions.ErrorActionPreference == ActionPreference.Stop && powershell.HadErrors)
                    {
                        throw new InvalidOperationException("PowerShell execution encountered errors.");
                    }
                }
                catch (Exception ex)
                {
                    UnifiedLogger.Create()
                        .Message($@"PowerShell invocation failed:{Environment.NewLine}{ex.Message}")
                        .Error(ex)
                        .Severity(LogLevel.Error)
                        .ErrorCategory(ErrorType.NotSpecified)
                        .Log();
                    throw;
                }
            }, executionContext.CancellationToken).ConfigureAwait(false);

            return output;
        }

        /// <summary>
        /// Invokes the PowerShell script asynchronously using the provided PowerShell instance and execution context.
        /// </summary>
        /// <param name="powershell">The <see cref="PowerShell"/> instance to use for script execution.</param>
        /// <param name="context">The execution context containing configuration options and cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a collection of <see cref="PSObject"/> as the result.</returns>
        private static async Task<PSDataCollection<PSObject>> InvokePowerShellAsync(
            PowerShell powershell,
            Shared.ExecutionContext context)
        {
            PSDataCollection<PSObject> output = new PSDataCollection<PSObject>();

            // Error Stream: Captures and logs any errors that occur during PowerShell execution.
            powershell.Streams.Error.DataAdded += (sender, e) =>
            {
                foreach (ErrorRecord? errorRecord in powershell.Streams.Error.ReadAll())
                {
                    UnifiedLogger.Create()
                        .Message($@"[PowerShell Error]{Environment.NewLine}{errorRecord.Exception.Message}")
                        .Error(errorRecord)
                        .Severity(LogLevel.Error)
                        .ErrorCategory((ErrorType)errorRecord.CategoryInfo.Category)
                        .Log();
                }
            };

            // Warning Stream: Logs any warnings generated by the PowerShell script.
            powershell.Streams.Warning.DataAdded += (sender, e) =>
            {
                foreach (WarningRecord? warningRecord in powershell.Streams.Warning.ReadAll())
                {
                    UnifiedLogger.Create()
                        .Parse(warningRecord)
                        .Severity(LogLevel.Warning)
                        .Log();
                }
            };

            // Verbose Stream: Captures verbose messages if PowerShell's verbose output is enabled.
            powershell.Streams.Verbose.DataAdded += (sender, e) =>
            {
                foreach (VerboseRecord verboseRecord in powershell.Streams.Verbose.ReadAll())
                {
                    UnifiedLogger.Create()
                        .Parse(verboseRecord)
                        .Severity(LogLevel.Verbose)
                        .Log();
                }
            };

            // Debug Stream: Logs debug messages.
            powershell.Streams.Debug.DataAdded += (sender, e) =>
            {
                foreach (DebugRecord? debugRecord in powershell.Streams.Debug.ReadAll())
                {
                    UnifiedLogger.Create()
                        .Parse(debugRecord)
                        .Severity(LogLevel.Debug)
                        .Log();
                }
            };

            // Information Stream: Captures informational messages from the PowerShell session.
            powershell.Streams.Information.DataAdded += (sender, e) =>
            {
                foreach (InformationRecord infoRecord in powershell.Streams.Information.ReadAll())
                {
                    UnifiedLogger.Create()
                        .Parse(infoRecord)
                        .Severity(LogLevel.Information)
                        .Log();
                }
            };

            if (context.PSOptions.Timeout.HasValue)
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);
                cts.CancelAfter(context.PSOptions.Timeout.Value);
                using (cts.Token.Register(() => powershell.Stop()))
                {
#if !CoreCLR
                    IAsyncResult asyncResult = powershell.BeginInvoke<PSObject, PSObject>(null, output, null, null, null);
                    await Task.Factory.FromAsync(asyncResult, ar => powershell.EndInvoke(ar)).ConfigureAwait(false);

#else
                    await powershell.InvokeAsync<PSObject, PSObject>(null, output, null, null, null).ConfigureAwait(false);
#endif
                }
            }
            else
            {
#if !CoreCLR
                IAsyncResult asyncResult = powershell.BeginInvoke<PSObject, PSObject>(null, output, null, null, null);
                await Task.Factory.FromAsync(asyncResult, ar => powershell.EndInvoke(ar)).ConfigureAwait(false);
#else
                await powershell.InvokeAsync<PSObject, PSObject>(null, output, null, null, null).ConfigureAwait(false);
#endif
            }

            return output;
        }

        /// <summary>
        /// Creates and configures a PowerShell runspace based on the provided execution context.
        /// </summary>
        /// <param name="context">The execution context containing configuration options.</param>
        /// <returns>A configured <see cref="Runspace"/> object.</returns>
        private static Runspace CreateRunspace(Shared.ExecutionContext context)
        {
            Runspace runspace;

            if (context.PSOptions.IsOutOfProcessRunspace == true)
            {
                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GetPowerShellPath(context.PSOptions),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                if (context.PSOptions.RunAsAdministrator)
                {
                    processStartInfo.Verb = "runas";
                }

                string psFilePath = GetPowerShellPath(context.PSOptions);
                var psi = new PowerShellProcessInstance
                {
                    Process =
                    {
                        StartInfo = processStartInfo
                    }
                };

                runspace = RunspaceFactory.CreateOutOfProcessRunspace(TypeTable.LoadDefaultTypeFiles(), psi);
            }
            else
            {
                InitialSessionState iss = context.PSOptions.InitialSessionState ?? InitialSessionState.CreateDefault();
                ConfigureInitialSessionState(iss, context.PSOptions);
                runspace = RunspaceFactory.CreateRunspace(context.PSOptions.PSADTHost ?? new DefaultHost(), iss);
            }

            runspace.ThreadOptions = context.PSOptions.ThreadOptions;
            if (context.PSOptions.ApartmentState != ApartmentState.Unknown)
            {
                runspace.ApartmentState = context.PSOptions.ApartmentState;
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
            Shared.ExecutionContext context)
        {
            if (!string.IsNullOrWhiteSpace(context.PSOptions.WorkingDirectory))
            {
                powershell.AddCommand("Set-Location").AddParameter("Path", context.PSOptions.WorkingDirectory).Invoke();
                powershell.Commands.Clear();
            }

            if (context.PSOptions.Culture != null)
            {
                powershell.AddScript($"[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::GetCultureInfo('{context.PSOptions.Culture.Name}')").Invoke();
                powershell.Commands.Clear();
            }

            if (context.PSOptions.UICulture != null)
            {
                powershell.AddScript($"[System.Threading.Thread]::CurrentThread.CurrentUICulture = [System.Globalization.CultureInfo]::GetCultureInfo('{context.PSOptions.UICulture.Name}')").Invoke();
                powershell.Commands.Clear();
            }

            LoadAssemblies(context.PSOptions, powershell);
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
                    iss.ImportPSModule(new[] { module });
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
        /// Validates the execution options to ensure that required fields are set.
        /// </summary>
        /// <param name="options">The execution options to validate.</param>
        /// <exception cref="ArgumentException">Thrown if neither ScriptPath nor ScriptText is provided.</exception>
        private static void ValidateCommandOptions(PSExecutionOptions options)
        {
            if (String.IsNullOrWhiteSpace(options.ScriptText) && String.IsNullOrWhiteSpace(options.ScriptPath))
            {
                throw new ArgumentException("Either 'ScriptPath' or 'ScriptText' must be provided as a configuration option.");
            }

            if (!String.IsNullOrWhiteSpace(options.ScriptText) && !String.IsNullOrWhiteSpace(options.ScriptPath))
            {
                throw new ArgumentException("Both 'ScriptPath' and 'ScriptText' cannot be provided as a configuration option.");
            }
        }

        private static Command CreateCommand(PSExecutionOptions options)
        {
            if (!String.IsNullOrWhiteSpace(options.ScriptPath))
            {
                options.ScriptText = File.ReadAllText(options.ScriptPath);
                if (String.IsNullOrWhiteSpace(options.ScriptText))
                {
                    throw new ArgumentException($"The file specified in the 'ScriptPath' option [{options.ScriptPath}] contains no data.");
                }
            }

            var command = new Command(options.ScriptText!, true);
            if (options.Parameters != null)
            {
                foreach (var parameter in options.Parameters)
                {
                    command.Parameters.Add(parameter.Key, parameter.Value);
                }
            }

            return command;
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
                        psFilePath = PathHelper.Replace(psFilePath, @"\SysWOW64\", @"\Sysnative\");
                    }
                    break;
                case PSArchitecture.X86:
                    if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
                    {
                        psFilePath = PathHelper.Replace(psFilePath, @"\System32\", @"\SysWOW64\");
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
