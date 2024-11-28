using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using PSADT.Logging;
using PSADT.PInvoke;
using PSADT.ProcessEx;

namespace PSADT.CommandLine
{
    /// <summary>
    /// Provides functionality to parse command-line arguments into a strongly-typed object.
    /// </summary>
    public static class Arguments
    {
        /// <summary>
        /// A list of all available command-line option mappings.
        /// </summary>
        public static readonly List<OptionMapping> _argumentMappings = new List<OptionMapping>
        {
            new OptionMapping("FilePath", "f"),
            new OptionMapping("ArgumentList", "l"),
            new OptionMapping("WorkingDirectory", "dir"),
            new OptionMapping("HideWindow", "h"),
            new OptionMapping("PrimaryActiveUserSession", "pus"),
            new OptionMapping("SessionId", "sid"),
            new OptionMapping("AllActiveUserSessions", "aus"),
            new OptionMapping("UseLinkedAdminToken", "adm"),
            new OptionMapping("PsExecutionPolicy", "pxp"),
            new OptionMapping("BypassPsExecutionPolicy", "bxp"),
            new OptionMapping("SuccessExitCodes", "ext"),
            new OptionMapping("ConsoleTimeoutInSeconds", "con"),
            new OptionMapping("RedirectOutput", "red"),
            new OptionMapping("OutputDirectory", "out"),
            new OptionMapping("MergeStdErrAndStdOut", "mrg"),
            new OptionMapping("TerminateOnTimeout", "trm"),
            new OptionMapping("InheritEnvironmentVariables", "iev"),
            new OptionMapping("Env", "e"),
            new OptionMapping("Wait", "w"),
            new OptionMapping("WaitType", "wt"),
            new OptionMapping("Verbose", "v"),
            new OptionMapping("Debug", "d"),
            new OptionMapping("Help", "?")
        };

        /// <summary>
        /// A list of all available flag mappings for process creation.
        /// </summary>
        public static readonly List<OptionMapping> _creationFlagMappings = new List<OptionMapping>
        {
            // Process creation flags
            new OptionMapping("BreakAwayFromJob", "bfj", CREATE_PROCESS.CREATE_BREAKAWAY_FROM_JOB),
            new OptionMapping("DefaultErrorMode", "dem", CREATE_PROCESS.CREATE_DEFAULT_ERROR_MODE),
            new OptionMapping("NewConsole", "nc", CREATE_PROCESS.CREATE_NEW_CONSOLE),
            new OptionMapping("NewProcessGroup", "npg", CREATE_PROCESS.CREATE_NEW_PROCESS_GROUP),
            new OptionMapping("NoWindow", "nw", CREATE_PROCESS.CREATE_NO_WINDOW),
            new OptionMapping("ProtectedProcess", "pp", CREATE_PROCESS.CREATE_PROTECTED_PROCESS),
            new OptionMapping("PreserveCodeAuthzLevel", "pcal", CREATE_PROCESS.CREATE_PRESERVE_CODE_AUTHZ_LEVEL),
            new OptionMapping("SecureProcess", "sp", CREATE_PROCESS.CREATE_SECURE_PROCESS),
            new OptionMapping("SeparateWowVdm", "swv", CREATE_PROCESS.CREATE_SEPARATE_WOW_VDM),
            new OptionMapping("SharedWowVdm", "shwv", CREATE_PROCESS.CREATE_SHARED_WOW_VDM),
            new OptionMapping("Suspended", "susp", CREATE_PROCESS.CREATE_SUSPENDED),
            new OptionMapping("UnicodeEnvironment", "ue", CREATE_PROCESS.CREATE_UNICODE_ENVIRONMENT),
            new OptionMapping("DebugOnlyThisProcess", "dotp", CREATE_PROCESS.DEBUG_ONLY_THIS_PROCESS),
            new OptionMapping("DebugProcess", "dbp", CREATE_PROCESS.DEBUG_PROCESS),
            new OptionMapping("DetachedProcess", "dtp", CREATE_PROCESS.DETACHED_PROCESS),
            new OptionMapping("ExtendedStartupInfo", "esi", CREATE_PROCESS.EXTENDED_STARTUPINFO_PRESENT),
            new OptionMapping("InheritParentAffinity", "ipa", CREATE_PROCESS.INHERIT_PARENT_AFFINITY),
            // Priority class flags
            new OptionMapping("NormalPriority", "np", CREATE_PROCESS.NORMAL_PRIORITY_CLASS),
            new OptionMapping("IdlePriority", "ip", CREATE_PROCESS.IDLE_PRIORITY_CLASS),
            new OptionMapping("HighPriority", "hp", CREATE_PROCESS.HIGH_PRIORITY_CLASS),
            new OptionMapping("RealtimePriority", "rtp", CREATE_PROCESS.REALTIME_PRIORITY_CLASS),
            new OptionMapping("BelowNormalPriority", "bnp", CREATE_PROCESS.BELOW_NORMAL_PRIORITY_CLASS),
            new OptionMapping("AboveNormalPriority", "anp", CREATE_PROCESS.ABOVE_NORMAL_PRIORITY_CLASS),
            new OptionMapping("BackgroundBegin", "bgb", CREATE_PROCESS.PROCESS_MODE_BACKGROUND_BEGIN),
            new OptionMapping("BackgroundEnd", "bge", CREATE_PROCESS.PROCESS_MODE_BACKGROUND_END)
        };

        /// <summary>
        /// Parses the command-line arguments into an object of type T.
        /// </summary>
        /// <typeparam name="T">The type of object to parse the arguments into.</typeparam>
        /// <param name="args">The command-line arguments to parse.</param>
        /// <returns>An object of type T with properties set based on the command-line arguments.</returns>
        /// <exception cref="ArgumentException">Thrown when there are duplicate or conflicting arguments.</exception>
        public static T Parse<T>(string[] args) where T : new()
        {
            var result = new T();
            var properties = typeof(T).GetProperties();

            // Validation for uniqueness of short and long names
            ValidateMappings();

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string argName = arg.StartsWith("--") ? arg.Substring(2) : arg.StartsWith("-") ? arg.Substring(1) : string.Empty;
                    argName = argName.ToLowerInvariant();

                    var mapping = _argumentMappings.FirstOrDefault(m => m.LongName.Equals(argName, StringComparison.OrdinalIgnoreCase) ||
                                                                        m.ShortName.Equals(argName, StringComparison.OrdinalIgnoreCase));

                    if (mapping != null)
                    {
                        var property = properties.FirstOrDefault(p => p.Name.Equals(mapping.LongName, StringComparison.OrdinalIgnoreCase));
                        HandleMapping(result, property, args, ref i);
                    }
                    else if (argName.Equals("env", StringComparison.OrdinalIgnoreCase))
                    {
                        // Handle environment variable
                        HandleEnvironmentVariable(result, args, ref i);
                    }
                    else
                    {
                        // Handle process creation flags
                        SetProcessCreationFlags(result as LaunchOptions, argName);
                    }
                }
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"Error parsing command-line arguments:{Environment.NewLine}{ex.Message}").Error(ex);
                UnifiedLogger.Create().Message("Use [--Help] to see usage information.").Severity(LogLevel.Warning);
                Environment.Exit(1);
            }

            ValidateRequiredArguments(result);

            return result;
        }

        /// <summary>
        /// Validates the uniqueness of the short and long names in the mappings.
        /// </summary>
        private static void ValidateMappings()
        {
            var allMappings = _argumentMappings.Concat(_creationFlagMappings).ToList();

            var duplicateShortNames = allMappings.GroupBy(m => m.ShortName.ToLowerInvariant())
                                                 .Where(g => g.Count() > 1)
                                                 .Select(g => g.Key);

            var duplicateLongNames = allMappings.GroupBy(m => m.LongName.ToLowerInvariant())
                                                .Where(g => g.Count() > 1)
                                                .Select(g => g.Key);

            if (duplicateShortNames.Any() || duplicateLongNames.Any())
            {
                throw new ArgumentException($"Duplicate command-line options found: ShortNames [{string.Join(", ", duplicateShortNames)}], LongNames [{string.Join(", ", duplicateLongNames)}]");
            }
        }

        /// <summary>
        /// Handles mapping of a command-line argument to a property in the target object.
        /// </summary>
        private static void HandleMapping<T>(T? result, PropertyInfo? property, string[] args, ref int index) where T : new()
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "Result object cannot be null.");
            }

            if (property != null)
            {
                if (property.PropertyType == typeof(bool))
                {
                    property.SetValue(result, true);
                }
                else if (index + 1 < args.Length && !args[index + 1].StartsWith("-"))
                {
                    SetPropertyValue(property, result, args[index + 1]);
                    index++;
                }
                else
                {
                    throw new ArgumentException($"Missing value for argument [{args[index]}].");
                }
            }
        }

        /// <summary>
        /// Handles environment variable arguments.
        /// </summary>
        private static void HandleEnvironmentVariable<T>(T result, string[] args, ref int index) where T : new()
        {
            if (index + 1 < args.Length && !args[index + 1].StartsWith("-"))
            {
                var envVar = args[index + 1].Split(new[] { '=' }, 2);
                if (envVar.Length == 2)
                {
                    var launchOptions = result as LaunchOptions;
                    launchOptions?.AddEnvironmentVariable(envVar[0], envVar[1]);
                }
                else
                {
                    throw new ArgumentException($"Invalid environment variable format [{args[index + 1]}].");
                }
                index++;
            }
            else
            {
                throw new ArgumentException($"Missing value for environment variable [{args[index]}].");
            }
        }

        /// <summary>
        /// Sets the value of a property on the target object based on the provided string value.
        /// </summary>
        private static void SetPropertyValue(PropertyInfo property, object target, string value)
        {
            try
            {
                if (property.PropertyType == typeof(string))
                {
                    property.SetValue(target, value);
                }
                else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                {
                    if (int.TryParse(value, out int intValue))
                    {
                        property.SetValue(target, intValue);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid integer value [{value}].");
                    }
                }
                else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                {
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        property.SetValue(target, boolValue);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid boolean value [{value}].");
                    }
                }
                else if (property.PropertyType == typeof(List<string>))
                {
                    List<string> list = (List<string>?)property.GetValue(target) ?? new List<string>();
                    list.AddRange(value.Split(','));
                    property.SetValue(target, list);
                }
                else if (property.PropertyType == typeof(List<int>))
                {
                    var list = (List<int>?)property.GetValue(target) ?? new List<int>();
                    var intValues = value.Split(',').Select(v =>
                    {
                        if (int.TryParse(v, out int intValue))
                        {
                            return intValue;
                        }
                        throw new ArgumentException($"Invalid integer value [{v}] in list.");
                    }).ToList();
                    property.SetValue(target, list);
                }
                else if (property.PropertyType.IsEnum)
                {
                    var enumValue = ParseEnum(property.PropertyType, value);
                    if (enumValue != null)
                    {
                        property.SetValue(target, enumValue);
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid value [{value}] for enum [{property.PropertyType.Name}]");
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported property type [{property.PropertyType.Name}].");
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error setting value for property [{property.Name}]: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a string value into an enum of the specified type.
        /// </summary>
        private static object? ParseEnum(Type enumType, string value)
        {
            try
            {
                var tryParseMethod = typeof(Enum).GetMethod("TryParse", new[] { typeof(string), typeof(bool), enumType.MakeByRefType() });
                var parameters = new object?[] { value, true, null };
                var success = (bool?)tryParseMethod?.Invoke(null, parameters);

                return success == true ? parameters[2] : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the process creation flags in the <see cref="LaunchOptions"/> object based on the provided command-line argument.
        /// </summary>
        private static void SetProcessCreationFlags(LaunchOptions? options, string argName)
        {
            if (options == null) return;

            var flagMapping = _creationFlagMappings.FirstOrDefault(fm => fm.LongName.Equals(argName, StringComparison.OrdinalIgnoreCase) ||
                                                                         fm.ShortName.Equals(argName, StringComparison.OrdinalIgnoreCase));

            if (flagMapping != null)
            {
                if (flagMapping?.CorrespondingTypedOption is CREATE_PROCESS flagType && !options.ProcessCreationFlags.Contains(flagType))
                {
                    options.ProcessCreationFlags.Add(flagType);
                }
                else
                {
                    throw new ArgumentException($"Duplicate process creation flag [{argName}] detected.");
                }
            }
            else
            {
                throw new ArgumentException($"Unknown process creation flag [{argName}].");
            }

            ValidateProcessCreationFlags(options.ProcessCreationFlags);
        }

        /// <summary>
        /// Validates the process creation flags to ensure that no conflicting flags are specified.
        /// </summary>
        private static void ValidateProcessCreationFlags(List<CREATE_PROCESS> flags)
        {
            if (flags.Contains(CREATE_PROCESS.CREATE_NO_WINDOW) && flags.Contains(CREATE_PROCESS.CREATE_NEW_CONSOLE))
            {
                throw new ArgumentException("CREATE_NO_WINDOW cannot be used with CREATE_NEW_CONSOLE.");
            }

            if (flags.Contains(CREATE_PROCESS.DETACHED_PROCESS) && flags.Contains(CREATE_PROCESS.CREATE_NEW_CONSOLE))
            {
                throw new ArgumentException("DETACHED_PROCESS cannot be used with CREATE_NEW_CONSOLE.");
            }

            if (flags.Contains(CREATE_PROCESS.CREATE_NEW_PROCESS_GROUP) && flags.Contains(CREATE_PROCESS.CREATE_NEW_CONSOLE))
            {
                throw new ArgumentException("CREATE_NEW_PROCESS_GROUP cannot be used with CREATE_NEW_CONSOLE.");
            }
        }

        /// <summary>
        /// Validates required arguments and ensures they have been set.
        /// </summary>
        private static void ValidateRequiredArguments<T>(T result) where T : new()
        {
            var filePathProperty = typeof(T).GetProperty("FilePath");
            if (filePathProperty != null && string.IsNullOrWhiteSpace((string)filePathProperty!.GetValue(result)!))
            {
                UnifiedLogger.Create().Message("Command-line property [--FilePath] is either not set or is empty after parsing of arguments.").Severity(LogLevel.Warning);
            }
        }
    }
}
