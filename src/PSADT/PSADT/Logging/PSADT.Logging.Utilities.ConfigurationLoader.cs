using System;
using System.IO;
using System.Collections.Generic;
using PSADT.Logging.Models;
using PSADT.Diagnostics.StackTraces;

namespace PSADT.Logging.Utilities
{
    /// <summary>
    /// ConfigurationLoader is responsible for loading LogOptions and ErrorParserConfig from a configuration file.
    /// The configuration file should have key=value pairs, one per line.
    /// </summary>
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Loads LogOptions from the specified configuration file.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file.</param>
        /// <returns>An instance of LogOptions.</returns>
        public static LogOptions LoadLogOptions(string configFilePath)
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new ArgumentException("Configuration file path must be provided.", nameof(configFilePath));

            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"Configuration file not found at path: {configFilePath}");

            try
            {
                var configLines = File.ReadAllLines(configFilePath);
                var logOptionsBuilder = LogOptions.CreateBuilder();
                var errorParserConfigBuilder = StackParserConfig.Create();

                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                        continue; // Skip empty lines and comments

                    var config = line.Split('=');
                    if (config.Length != 2) continue;

                    var key = config[0].Trim();
                    var value = config[1].Trim();

                    switch (key)
                    {
                        // LogOptions properties
                        case "LogDirectory":
                            logOptionsBuilder.SetLogDirectory(value);
                            break;
                        case "LogFileNamePrefix":
                            logOptionsBuilder.SetLogFileNamePrefix(value);
                            break;
                        case "LogFileNameTimestamp":
                            if (DateTime.TryParse(value, out DateTime timestamp))
                                logOptionsBuilder.SetLogFileNameTimestamp(timestamp);
                            break;
                        case "LogFileNameTimestampFormat":
                            logOptionsBuilder.SetLogFileNameTimestampFormat(value);
                            break;
                        case "LogFileNameSuffix":
                            logOptionsBuilder.SetLogFileNameSuffix(value);
                            break;
                        case "LogFileExtension":
                            logOptionsBuilder.SetLogFileExtension(value);
                            break;
                        case "LogFileNameWithoutExtension":
                            logOptionsBuilder.SetLogFileNameWithoutExtension(value);
                            break;
                        case "LogFormat":
                            if (Enum.TryParse<TextLogFormat>(value, true, out var logFormat))
                                logOptionsBuilder.SetLogFormat(logFormat);
                            break;
                        case "MinimumLogLevel":
                            if (Enum.TryParse<LogLevel>(value, true, out var logLevel))
                                logOptionsBuilder.SetMinimumLogLevel(logLevel);
                            break;
                        case "TextSeparator":
                            logOptionsBuilder.SetTextSeparator(value);
                            break;
                        case "MaxQueueSize":
                            if (uint.TryParse(value, out uint maxQueueSize))
                                logOptionsBuilder.SetMaxQueueSize(maxQueueSize);
                            break;
                        case "MaxRepeatedMessages":
                            if (int.TryParse(value, out int maxRepeated))
                                logOptionsBuilder.SetMaxRepeatedMessages(maxRepeated);
                            break;
                        case "RetryAttempts":
                            if (uint.TryParse(value, out uint retryAttempts))
                                logOptionsBuilder.SetRetryAttempts(retryAttempts);
                            break;
                        case "RetryTimeoutInMilliseconds":
                            if (uint.TryParse(value, out uint retryTimeout))
                                logOptionsBuilder.SetRetryTimeoutInMilliseconds(retryTimeout);
                            break;
                        case "RetryIntervalInMilliseconds":
                            if (uint.TryParse(value, out uint retryInterval))
                                logOptionsBuilder.SetRetryIntervalInMilliseconds(retryInterval);
                            break;
                        case "MaxRetryDelayInMilliseconds":
                            if (uint.TryParse(value, out uint maxRetryDelay))
                                logOptionsBuilder.SetMaxRetryDelayInMilliseconds(maxRetryDelay);
                            break;
                        case "StartManually":
                            if (bool.TryParse(value, out bool startManually))
                                logOptionsBuilder.SetStartManually(startManually);
                            break;
                        case "StopLoggingTimeoutSeconds":
                            if (double.TryParse(value, out double timeoutSeconds))
                                logOptionsBuilder.SetStopLoggingTimeout(TimeSpan.FromSeconds(timeoutSeconds));
                            break;
                        case "SubscribeToUnhandledException":
                            if (bool.TryParse(value, out bool subscribeUnhandled))
                                logOptionsBuilder.SubscribeToUnhandledException(subscribeUnhandled);
                            break;
                        case "SubscribeToUnobservedTaskException":
                            if (bool.TryParse(value, out bool subscribeUnobserved))
                                logOptionsBuilder.SubscribeToUnobservedTaskException(subscribeUnobserved);
                            break;
                        case "SubscribeToOnProcessExitAndCallDispose":
                            if (bool.TryParse(value, out bool subscribeProcessExit))
                                logOptionsBuilder.SubscribeToOnProcessExitAndCallDispose(subscribeProcessExit);
                            break;
                        case "MaxLogFileSizeInBytes":
                            if (ulong.TryParse(value, out ulong maxLogFileSize))
                                logOptionsBuilder.SetMaxLogFileSizeInBytes(maxLogFileSize);
                            break;

                        // ErrorParserConfig properties
                        case "MaxRecursionDepth":
                            if (int.TryParse(value, out int maxRecursionDepth))
                                errorParserConfigBuilder.SetMaxRecursionDepth(maxRecursionDepth);
                            break;
                        case "StackFramesToSkip":
                            if (int.TryParse(value, out int stackFramesToSkip))
                                errorParserConfigBuilder.SetStackFramesToSkip(stackFramesToSkip);
                            break;
                        case "MethodNamesToSkip":
                            errorParserConfigBuilder.SetMethodNamesToSkip(new List<string>(value.Split(',')));
                            break;
                        case "DeclaringTypesToSkip":
                            // Add logic to handle loading types if needed
                            break;
                        case "OutputJson":
                            if (bool.TryParse(value, out bool outputJson))
                                errorParserConfigBuilder.SetOutputFormat(outputJson);
                            break;
                        case "IndentationSpaces":
                            if (int.TryParse(value, out int indentationSpaces))
                                errorParserConfigBuilder.SetIndentationSpaces(indentationSpaces);
                            break;
                        case "IncludeStackTrace":
                            if (bool.TryParse(value, out bool includeStackTrace))
                                errorParserConfigBuilder.SetIncludeStackTrace(includeStackTrace);
                            break;
                        case "MaxLineLength":
                            if (int.TryParse(value, out int maxLineLength))
                                errorParserConfigBuilder.SetMaxLineLength(maxLineLength);
                            break;
                        case "IncludeTargetSite":
                            if (bool.TryParse(value, out bool includeTargetSite))
                                errorParserConfigBuilder.SetIncludeTargetSite(includeTargetSite);
                            break;
                        case "IncludeHelpLink":
                            if (bool.TryParse(value, out bool includeHelpLink))
                                errorParserConfigBuilder.SetIncludeHelpLink(includeHelpLink);
                            break;
                        case "IncludeCustomProperties":
                            if (bool.TryParse(value, out bool includeCustomProperties))
                                errorParserConfigBuilder.SetIncludeCustomProperties(includeCustomProperties);
                            break;
                        case "PrependString":
                            errorParserConfigBuilder.SetPrependString(value);
                            break;
                        case "AppendString":
                            errorParserConfigBuilder.SetAppendString(value);
                            break;
                        case "SeparatorBetweenFileAndMethod":
                            errorParserConfigBuilder.SetSeparatorBetweenFileAndMethod(value);
                            break;
                        case "SeparatorBetweenMethodAndLine":
                            errorParserConfigBuilder.SetSeparatorBetweenMethodAndLine(value);
                            break;
                        case "MethodNameMaxLength":
                            if (int.TryParse(value, out int methodNameMaxLength))
                                errorParserConfigBuilder.SetMethodNameMaxLength(methodNameMaxLength);
                            break;
                        case "IncludeLineNumberInStackTrace":
                            if (bool.TryParse(value, out bool includeLineNumber))
                                errorParserConfigBuilder.SetIncludeLineNumberInStackTrace(includeLineNumber);
                            break;
                        case "NoLineNumberText":
                            errorParserConfigBuilder.SetNoLineNumberText(value);
                            break;
                        default:
                            // Unknown configuration key; optionally log or ignore
                            break;
                    }
                }

                var errorParserConfig = errorParserConfigBuilder.Build();
                return logOptionsBuilder.SetErrorParserConfig(errorParserConfig).Build();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
