using System;
using System.Text;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.CompilerServices;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// A thread-safe class for parsing Exception and ErrorRecord objects into readable text or JSON formats.
    /// Implements the IErrorParser interface for testability.
    /// </summary>
    public static class ErrorParser
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Parse(object input, StackParserConfig? config = null)
        {
            config ??= StackParserConfig.Create().Build();

            if (input is Exception ex)
            {
                // Handle parsing an Exception object
                return config.OutputJson ? ParseExceptionToJson(ex, config) : ParseExceptionToText(ex, config);
            }
            else if (input is ErrorRecord errorRecord)
            {
                // Handle parsing an ErrorRecord object
                return config.OutputJson ? ParseErrorRecordToJson(errorRecord, config) : ParseErrorRecordToText(errorRecord, config);
            }
            else if (input is InformationRecord informationRecord)
            {
                // Handle parsing an InformationRecord object
                return config.OutputJson ? ParseInformationRecordToJson(informationRecord, config) : ParseInformationRecordToText(informationRecord, config);
            }
            else if (input is DebugRecord debugRecord)
            {
                // Handle parsing a DebugRecord object
                return config.OutputJson ? ParseDebugRecordToJson(debugRecord, config) : ParseDebugRecordToText(debugRecord, config);
            }
            else if (input is VerboseRecord verboseRecord)
            {
                // Handle parsing a VerboseRecord object
                return config.OutputJson ? ParseVerboseRecordToJson(verboseRecord, config) : ParseVerboseRecordToText(verboseRecord, config);
            }
            else if (input is WarningRecord warningRecord)
            {
                // Handle parsing a WarningRecord object
                return config.OutputJson ? ParseWarningRecordToJson(warningRecord, config) : ParseWarningRecordToText(warningRecord, config);
            }
            else
            {
                // Handle unknown input type
                return $"Input must be of type [Exception], [ErrorRecord], [InformationRecord], [DebugRecord], [VerboseRecord], or [WarningRecord]. Input type was [{input.GetType()}].";
            }
        }

        #region InformationRecord Parsing

        /// <summary>
        /// Parses a PowerShell InformationRecord object into a text format.
        /// </summary>
        private static string ParseInformationRecordToText(InformationRecord informationRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("INFORMATION RECORD");
            sb.AppendLine($"Source: {informationRecord.Source}");
            sb.AppendLine($"Time Generated: {informationRecord.TimeGenerated}");
            sb.AppendLine($"Message: {informationRecord.MessageData}");
            sb.AppendLine($"Tags: {string.Join(", ", informationRecord.Tags ?? new List<string>())}");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a PowerShell InformationRecord object into a JSON format.
        /// </summary>
        private static string ParseInformationRecordToJson(InformationRecord informationRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Source\": \"{EscapeString(informationRecord.Source)}\", ");
            sb.Append($"\"TimeGenerated\": \"{informationRecord.TimeGenerated:O}\", ");
            sb.Append($"\"Message\": \"{EscapeString(informationRecord.MessageData.ToString())}\", ");
            sb.Append($"\"Tags\": \"{EscapeString(string.Join(", ", informationRecord.Tags ?? new List<string>()))}\"");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region DebugRecord Parsing

        /// <summary>
        /// Parses a PowerShell DebugRecord object into a text format.
        /// </summary>
        private static string ParseDebugRecordToText(DebugRecord debugRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DEBUG RECORD");
            sb.AppendLine($"Message: {debugRecord.Message}");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a PowerShell DebugRecord object into a JSON format.
        /// </summary>
        private static string ParseDebugRecordToJson(DebugRecord debugRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Message\": \"{EscapeString(debugRecord.Message)}\"");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region VerboseRecord Parsing

        /// <summary>
        /// Parses a PowerShell VerboseRecord object into a text format.
        /// </summary>
        private static string ParseVerboseRecordToText(VerboseRecord verboseRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("VERBOSE RECORD");
            sb.AppendLine($"Message: {verboseRecord.Message}");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a PowerShell VerboseRecord object into a JSON format.
        /// </summary>
        private static string ParseVerboseRecordToJson(VerboseRecord verboseRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Message\": \"{EscapeString(verboseRecord.Message)}\"");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        #region WarningRecord Parsing

        /// <summary>
        /// Parses a PowerShell WarningRecord object into a text format.
        /// </summary>
        private static string ParseWarningRecordToText(WarningRecord warningRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("WARNING RECORD");
            sb.AppendLine($"Message: {warningRecord.Message}");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a PowerShell WarningRecord object into a JSON format.
        /// </summary>
        private static string ParseWarningRecordToJson(WarningRecord warningRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"Message\": \"{EscapeString(warningRecord.Message)}\"");
            sb.Append("}");
            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// Parses an Exception object into a well-organized text format.
        /// </summary>
        private static string ParseExceptionToText(Exception ex, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                ParseExceptionToTextIterative(ex, sb, config);
            }
            catch
            {
                // Gracefully handle parsing errors
                return "An error occurred while parsing the exception.";
            }
            return sb.ToString();
        }

        /// <summary>
        /// Iteratively parses an Exception object into text to avoid stack overflows.
        /// </summary>
        private static void ParseExceptionToTextIterative(Exception ex, StringBuilder sb, StackParserConfig config)
        {
            var exceptions = new Stack<ExceptionContext>();
            exceptions.Push(new ExceptionContext(ex, 0));

            while (exceptions.Count > 0)
            {
                var current = exceptions.Pop();
                var currentException = current.Exception;
                var currentDepth = current.Depth;

                string indent = new string(' ', currentDepth * config.IndentationSpaces);

                if (currentDepth > config.MaxRecursionDepth)
                {
                    sb.AppendLine(indent + "Max recursion depth reached.");
                    continue;
                }

                if (currentException == null)
                {
                    sb.AppendLine(indent + "Exception is null.");
                    continue;
                }

                sb.AppendLine(indent + new string('-', 60));

                HandleExceptionType(currentException, sb, config, indent);
                HandleMessage(currentException, sb, config, indent);
                HandleSeverityLevel(currentException, sb, config, indent);
                HandleTargetSite(currentException, sb, config, indent);
                HandleHelpLink(currentException, sb, config, indent);
                HandleStackTrace(currentException, sb, config, indent);
                HandleData(currentException, sb, config, indent);
                HandleCustomProperties(currentException, sb, config, indent);
                HandleSource(currentException, sb, config, indent);

                HandleInnerExceptions(currentException, exceptions, currentDepth, config, sb, indent);
            }
        }

        private static void HandleExceptionType(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                sb.AppendLine(indent + $"EXCEPTION TYPE: {ex.GetType().FullName}");
            }
            catch
            {
                sb.AppendLine(indent + "EXCEPTION TYPE: [Error retrieving exception type]");
            }
        }

        private static void HandleMessage(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                string message = ex.Message;
                if (!config.IncludeSensitiveData)
                {
                    message = "[Sensitive data masked]";
                }
                sb.Append(WrapString(indent + "MESSAGE: " + message, config, indent.Length));
            }
            catch
            {
                sb.AppendLine(indent + "MESSAGE: [Error retrieving message]");
            }
        }

        private static void HandleSeverityLevel(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (config.ExceptionSeverityMapper != null)
                {
                    string severity = config.ExceptionSeverityMapper(ex);
                    sb.AppendLine(indent + $"SEVERITY LEVEL: {severity}");
                }
            }
            catch
            {
                sb.AppendLine(indent + "SEVERITY LEVEL: [Error determining severity level]");
            }
        }

        private static void HandleTargetSite(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (config.IncludeTargetSite)
                {
                    string targetSite = ex.TargetSite?.ToString() ?? "[Null]";
                    sb.AppendLine(indent + $"TARGET SITE: {targetSite}");
                }
            }
            catch
            {
                sb.AppendLine(indent + "TARGET SITE: [Error retrieving target site]");
            }
        }

        private static void HandleHelpLink(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (config.IncludeHelpLink)
                {
                    string helpLink = ex.HelpLink ?? "[Null]";
                    sb.AppendLine(indent + $"HELP LINK: {helpLink}");
                }
            }
            catch
            {
                sb.AppendLine(indent + "HELP LINK: [Error retrieving help link]");
            }
        }

        private static void HandleStackTrace(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (config.IncludeStackTrace && ex.StackTrace != null)
                {
                    sb.AppendLine(indent + "STACK TRACE:");
                    sb.Append(WrapString(indent + ex.StackTrace, config, indent.Length));
                }
            }
            catch
            {
                sb.AppendLine(indent + "STACK TRACE: [Error retrieving stack trace]");
            }
        }

        private static void HandleData(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (ex.Data != null && ex.Data.Count > 0)
                {
                    sb.AppendLine(indent + "DATA:");
                    foreach (System.Collections.DictionaryEntry entry in ex.Data)
                    {
                        if (!config.IncludeSensitiveData)
                        {
                            sb.AppendLine(indent + $"  {entry.Key}: [Sensitive data masked]");
                        }
                        else
                        {
                            sb.Append(WrapString(indent + $"  {entry.Key}: {entry.Value}", config, indent.Length + 2));
                        }
                    }
                }
            }
            catch
            {
                sb.AppendLine(indent + "DATA: [Error retrieving data]");
            }
        }

        private static void HandleCustomProperties(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                if (config.IncludeCustomProperties)
                {
                    var type = ex.GetType();
                    var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                    foreach (var prop in properties)
                    {
                        if (prop.Name == "Message" || prop.Name == "StackTrace" || prop.Name == "InnerException" ||
                            prop.Name == "TargetSite" || prop.Name == "HelpLink" || prop.Name == "Data" || prop.Name == "Source" ||
                            prop.Name == "HResult")
                        {
                            continue; // Skip standard exception properties that are already handled
                        }

                        if (config.AdditionalPropertiesToInclude != null && !config.AdditionalPropertiesToInclude.Contains(prop.Name))
                        {
                            continue; // Skip properties not in the additional properties list
                        }

                        object? value = null;
                        try
                        {
                            value = prop.GetValue(ex);
                        }
                        catch
                        {
                            value = "[Error retrieving property]";
                        }

                        if (value != null)
                        {
                            if (!config.IncludeSensitiveData)
                            {
                                sb.AppendLine(indent + $"  {prop.Name}: [Sensitive data masked]");
                            }
                            else
                            {
                                sb.Append(WrapString(indent + $"  {prop.Name}: {value}", config, indent.Length + 2));
                            }
                        }
                    }
                }
            }
            catch
            {
                sb.AppendLine(indent + "CUSTOM PROPERTIES: [Error retrieving custom properties]");
            }
        }

        private static void HandleSource(Exception ex, StringBuilder sb, StackParserConfig config, string indent)
        {
            try
            {
                sb.AppendLine(indent + $"SOURCE: {ex.Source}");
            }
            catch
            {
                sb.AppendLine(indent + "SOURCE: [Error retrieving source]");
            }
        }

        private static void HandleInnerExceptions(Exception ex, Stack<ExceptionContext> exceptions, int currentDepth, StackParserConfig config, StringBuilder sb, string indent)
        {
            try
            {
                if (ex is AggregateException aggEx)
                {
                    sb.AppendLine(indent + "INNER EXCEPTIONS:");
                    int index = 1;
                    foreach (var inner in aggEx.InnerExceptions)
                    {
                        sb.AppendLine(indent + $"  Inner Exception {index}:");
                        exceptions.Push(new ExceptionContext(inner, currentDepth + 1));
                        index++;
                    }
                }
                else if (ex.InnerException != null)
                {
                    sb.AppendLine(indent + "INNER EXCEPTION:");
                    exceptions.Push(new ExceptionContext(ex.InnerException, currentDepth + 1));
                }
            }
            catch
            {
                sb.AppendLine(indent + "INNER EXCEPTIONS: [Error retrieving inner exceptions]");
            }
        }


        /// <summary>
        /// Parses an ErrorRecord object into a well-organized text format.
        /// </summary>
        private static string ParseErrorRecordToText(ErrorRecord errorRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                ParseErrorRecordToTextInternal(errorRecord, sb, 0, config);
            }
            catch
            {
                return "An error occurred while parsing the error record.";
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses an ErrorRecord object into text format.
        /// </summary>
        private static void ParseErrorRecordToTextInternal(ErrorRecord errorRecord, StringBuilder sb, int currentDepth, StackParserConfig config)
        {
            string indent = new string(' ', currentDepth * config.IndentationSpaces);

            if (currentDepth > config.MaxRecursionDepth)
            {
                sb.AppendLine(indent + "Max recursion depth reached.");
                return;
            }

            if (errorRecord == null)
            {
                sb.AppendLine(indent + "ErrorRecord is null.");
                return;
            }

            sb.AppendLine(indent + new string('=', 60));
            sb.AppendLine(indent + $"ERROR CATEGORY: {errorRecord.CategoryInfo.Category}");
            sb.AppendLine(indent + $"FULLY QUALIFIED ERROR ID: {errorRecord.FullyQualifiedErrorId}");
            sb.Append(WrapString(indent + "MESSAGE: " + errorRecord.Exception?.Message, config, indent.Length));
            sb.Append(WrapString(indent + "SCRIPT STACK TRACE: " + errorRecord.ScriptStackTrace, config, indent.Length));

            // Include InvocationInfo if available
            if (errorRecord.InvocationInfo != null)
            {
                sb.AppendLine(indent + $"SCRIPT NAME: {errorRecord.InvocationInfo.ScriptName}");
                sb.AppendLine(indent + $"LINE NUMBER: {errorRecord.InvocationInfo.ScriptLineNumber.ToString(config.CultureInfo)}");
                sb.AppendLine(indent + $"POSITION MESSAGE: {errorRecord.InvocationInfo.PositionMessage}");
            }

            CallerContext callerContext = new CallerContext(StackTraceUtility.GetFirstRelevantStackFrame(errorRecord.Exception!, null, null, config));
            sb.AppendLine(indent + $"METHOD: {callerContext.MethodName}");
            sb.AppendLine(indent + $"LINE NUMBER: {callerContext.LineNumber.ToString(config.CultureInfo)}");
            sb.AppendLine(indent + $"FILE NAME: {callerContext.FileName}");

            if (config.IncludeStackTrace && errorRecord.Exception?.StackTrace != null)
            {
                sb.AppendLine(indent + "STACK TRACE:");
                sb.Append(WrapString(indent + errorRecord.Exception.StackTrace, config, indent.Length));
            }

            if (errorRecord.Exception != null)
            {
                sb.AppendLine(indent + "EXCEPTION DETAILS:");
                ParseExceptionToTextIterative(errorRecord.Exception, sb, config);
            }

            // Instead, we can include other useful information from ErrorDetails if needed
            if (errorRecord.ErrorDetails != null)
            {
                sb.AppendLine(indent + "ERROR DETAILS:");
                sb.AppendLine(indent + $"  MESSAGE: {errorRecord.ErrorDetails.Message}");
                sb.AppendLine(indent + $"  RECOMMENDED ACTION: {errorRecord.ErrorDetails.RecommendedAction}");
            }
        }

        /// <summary>
        /// Wraps a string to the next line if it exceeds the maximum line length, aligning it appropriately.
        /// </summary>
        private static string WrapString(string input, StackParserConfig config, int currentIndentation)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var sb = new StringBuilder();
            int maxLineLength = config.MaxLineLength;
            string indent = new string(' ', currentIndentation);

            while (input.Length > 0)
            {
                int lineLength = Math.Min(maxLineLength, input.Length);
                string line = input.Substring(0, lineLength);

                // If the line is longer than max and there's no space at the end, try to find a space to wrap
                if (lineLength == maxLineLength && input.Length > maxLineLength && !char.IsWhiteSpace(input[maxLineLength]))
                {
                    int lastSpace = line.LastIndexOf(' ');
                    if (lastSpace > -1)
                    {
                        lineLength = lastSpace;
                        line = input.Substring(0, lineLength);
                    }
                }

                sb.AppendLine(line);
                input = indent + input.Substring(lineLength).TrimStart();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Parses an Exception object into a JSON string.
        /// </summary>
        private static string ParseExceptionToJson(Exception ex, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append("{");
                ParseExceptionToJsonIterative(ex, sb, config);
                sb.Append("}");
            }
            catch
            {
                return "{\"Error\": \"An error occurred while parsing the exception.\"}";
            }
            return sb.ToString();
        }

        /// <summary>
        /// Iteratively parses an Exception object into JSON to avoid stack overflows.
        /// </summary>
        private static void ParseExceptionToJsonIterative(Exception ex, StringBuilder sb, StackParserConfig config)
        {
            var exceptions = new Stack<ExceptionContext>();
            exceptions.Push(new ExceptionContext(ex, 0));

            int exceptionCount = 0;
            while (exceptions.Count > 0)
            {
                var current = exceptions.Pop();
                var currentException = current.Exception;
                var currentDepth = current.Depth;

                if (currentDepth > config.MaxRecursionDepth)
                {
                    sb.Append("\"MaxRecursionDepthReached\": true");
                    continue;
                }

                if (currentException == null)
                {
                    sb.Append("\"Exception\": null");
                    continue;
                }

                if (exceptionCount > 0)
                {
                    sb.Append(", ");
                }

                sb.Append("\"Exception").Append(exceptionCount).Append("\": {");

                var properties = new List<string>();

                try
                {
                    properties.Add($"\"ExceptionType\": \"{EscapeString(currentException.GetType().FullName)}\"");
                }
                catch
                {
                    properties.Add("\"ExceptionType\": \"[Error retrieving exception type]\"");
                }

                try
                {
                    string message = currentException.Message;
                    if (!config.IncludeSensitiveData)
                    {
                        message = "[Sensitive data masked]";
                    }
                    properties.Add($"\"Message\": \"{EscapeString(message)}\"");
                }
                catch
                {
                    properties.Add("\"Message\": \"[Error retrieving message]\"");
                }

                if (config.ExceptionSeverityMapper != null)
                {
                    try
                    {
                        string severity = config.ExceptionSeverityMapper(currentException);
                        properties.Add($"\"SeverityLevel\": \"{EscapeString(severity)}\"");
                    }
                    catch
                    {
                        properties.Add("\"SeverityLevel\": \"[Error determining severity level]\"");
                    }
                }

                try
                {
                    if (config.IncludeTargetSite)
                    {
                        string targetSite = currentException.TargetSite?.ToString() ?? "[Null]";
                        properties.Add($"\"TargetSite\": \"{EscapeString(targetSite)}\"");
                    }
                }
                catch
                {
                    properties.Add("\"TargetSite\": \"[Error retrieving target site]\"");
                }

                try
                {
                    if (config.IncludeHelpLink)
                    {
                        string helpLink = currentException.HelpLink ?? "[Null]";
                        properties.Add($"\"HelpLink\": \"{EscapeString(helpLink)}\"");
                    }
                }
                catch
                {
                    properties.Add("\"HelpLink\": \"[Error retrieving help link]\"");
                }

                try
                {
                    if (config.IncludeStackTrace && currentException.StackTrace != null)
                    {
                        properties.Add($"\"StackTrace\": \"{EscapeString(currentException.StackTrace)}\"");
                    }
                }
                catch
                {
                    properties.Add("\"StackTrace\": \"[Error retrieving stack trace]\"");
                }

                try
                {
                    if (currentException.Data != null && currentException.Data.Count > 0)
                    {
                        StringBuilder dataSb = new StringBuilder();
                        dataSb.Append("{");
                        bool firstEntry = true;
                        foreach (System.Collections.DictionaryEntry entry in currentException.Data)
                        {
                            if (!firstEntry)
                            {
                                dataSb.Append(", ");
                            }
                            if (!config.IncludeSensitiveData)
                            {
                                dataSb.Append($"\"{EscapeString(entry.Key.ToString())}\": \"[Sensitive data masked]\"");
                            }
                            else
                            {
                                dataSb.Append($"\"{EscapeString(entry.Key.ToString())}\": \"{EscapeString(entry.Value?.ToString())}\"");
                            }
                            firstEntry = false;
                        }
                        dataSb.Append("}");
                        properties.Add($"\"Data\": {dataSb}");
                    }
                }
                catch
                {
                    properties.Add("\"Data\": \"[Error retrieving data]\"");
                }

                try
                {
                    properties.Add($"\"Source\": \"{EscapeString(currentException.Source)}\"");
                }
                catch
                {
                    properties.Add("\"Source\": \"[Error retrieving source]\"");
                }

                // Handle custom properties
                if (config.IncludeCustomProperties)
                {
                    try
                    {
                        var type = currentException.GetType();
                        var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                        foreach (var prop in props)
                        {
                            if (prop.Name == "Message" || prop.Name == "StackTrace" || prop.Name == "InnerException" ||
                                prop.Name == "TargetSite" || prop.Name == "HelpLink" || prop.Name == "Data" || prop.Name == "Source" ||
                                prop.Name == "HResult")
                            {
                                continue; // Skip standard exception properties that are already handled
                            }

                            if (config.AdditionalPropertiesToInclude != null && !config.AdditionalPropertiesToInclude.Contains(prop.Name))
                            {
                                continue; // Skip properties not in the additional properties list
                            }

                            object? value = null;
                            try
                            {
                                value = prop.GetValue(currentException);
                            }
                            catch
                            {
                                value = "[Error retrieving property]";
                            }

                            if (value != null)
                            {
                                if (!config.IncludeSensitiveData)
                                {
                                    properties.Add($"\"{prop.Name}\": \"[Sensitive data masked]\"");
                                }
                                else
                                {
                                    properties.Add($"\"{prop.Name}\": \"{EscapeString(value.ToString())}\"");
                                }
                            }
                        }
                    }
                    catch
                    {
                        properties.Add("\"CustomProperties\": \"[Error retrieving custom properties]\"");
                    }
                }

                sb.Append(string.Join(", ", properties));

                // Prepare inner exceptions for processing
                try
                {
                    if (currentException is AggregateException aggEx)
                    {
                        int index = 0;
                        foreach (var inner in aggEx.InnerExceptions)
                        {
                            exceptions.Push(new ExceptionContext(inner, currentDepth + 1));
                            index++;
                        }
                    }
                    else if (currentException.InnerException != null)
                    {
                        exceptions.Push(new ExceptionContext(currentException.InnerException, currentDepth + 1));
                    }
                }
                catch
                {
                    // Handle any errors when processing inner exceptions
                }

                sb.Append("}");
                exceptionCount++;
            }
        }

        /// <summary>
        /// Escapes a string to be JSON-compliant.
        /// </summary>
        private static string EscapeString(string? str)
        {
            if (str == null)
                return "";

            var sb = new StringBuilder();
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '/':
                        sb.Append("\\/");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (char.IsControl(c) || c > 127)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses an ErrorRecord object into a JSON string.
        /// </summary>
        private static string ParseErrorRecordToJson(ErrorRecord errorRecord, StackParserConfig config)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb.Append("{");
                ParseErrorRecordToJsonInternal(errorRecord, sb, 0, config);
                sb.Append("}");
            }
            catch
            {
                return "{\"Error\": \"An error occurred while parsing the error record.\"}";
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parses an ErrorRecord object into JSON format.
        /// </summary>
        private static void ParseErrorRecordToJsonInternal(ErrorRecord errorRecord, StringBuilder sb, int currentDepth, StackParserConfig config)
        {
            if (currentDepth > config.MaxRecursionDepth)
            {
                sb.Append("\"MaxRecursionDepthReached\": true");
                return;
            }

            if (errorRecord == null)
            {
                sb.Append("\"ErrorRecord\": null");
                return;
            }

            var properties = new List<string>
            {
                $"\"ErrorCategory\": \"{EscapeString(errorRecord.CategoryInfo.Category.ToString())}\"",
                $"\"FullyQualifiedErrorId\": \"{EscapeString(errorRecord.FullyQualifiedErrorId)}\"",
                $"\"Message\": \"{EscapeString(errorRecord.Exception?.Message)}\"",
                $"\"ScriptStackTrace\": \"{EscapeString(errorRecord.ScriptStackTrace)}\""
            };

            // Include InvocationInfo if available
            if (errorRecord.InvocationInfo != null)
            {
                properties.Add($"\"ScriptName\": \"{EscapeString(errorRecord.InvocationInfo.ScriptName)}\"");
                properties.Add($"\"LineNumber\": {errorRecord.InvocationInfo.ScriptLineNumber.ToString(config.CultureInfo)}");
                properties.Add($"\"PositionMessage\": \"{EscapeString(errorRecord.InvocationInfo.PositionMessage)}\"");
            }

            CallerContext callerContext = new CallerContext(StackTraceUtility.GetFirstRelevantStackFrame(errorRecord.Exception!, null, null, config));
            properties.Add($"\"Method\": \"{EscapeString(callerContext.MethodName)}\"");
            properties.Add($"\"LineNumber\": {callerContext.LineNumber.ToString(config.CultureInfo)}");
            properties.Add($"\"FileName\": \"{EscapeString(callerContext.FileName)}\"");

            if (config.IncludeStackTrace && errorRecord.Exception?.StackTrace != null)
            {
                properties.Add($"\"StackTrace\": \"{EscapeString(errorRecord.Exception.StackTrace)}\"");
            }

            if (errorRecord.Exception != null)
            {
                StringBuilder innerSb = new StringBuilder();
                innerSb.Append("{");
                ParseExceptionToJsonIterative(errorRecord.Exception, innerSb, config);
                innerSb.Append("}");
                properties.Add($"\"ExceptionDetails\": {innerSb}");
            }

            if (errorRecord.ErrorDetails != null)
            {
                var errorDetailsProperties = new List<string>
            {
                $"\"Message\": \"{EscapeString(errorRecord.ErrorDetails.Message)}\"",
                $"\"RecommendedAction\": \"{EscapeString(errorRecord.ErrorDetails.RecommendedAction)}\""
            };
                properties.Add($"\"ErrorDetails\": {{ {string.Join(", ", errorDetailsProperties)} }}");
            }

            sb.Append(string.Join(", ", properties));
        }
    }
}
