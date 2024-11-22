using System;
using System.Globalization;
using System.Collections.Generic;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// Configuration settings for the ExceptionUtility class, supporting fluent configuration.
    /// </summary>
    public class StackParserConfig
    {
        // Core settings
        public int MaxRecursionDepth { get; private set; } = 10;
        public int StackFramesToSkip { get; private set; } = 0;
        public List<string>? MethodNamesToSkip { get; private set; }
        public List<Type>? DeclaringTypesToSkip { get; private set; }
        public bool OutputJson { get; private set; } = false;

        // Formatting options
        public int IndentationSpaces { get; private set; } = 4;
        public bool IncludeStackTrace { get; private set; } = true;
        public int MaxLineLength { get; private set; } = 80;

        // Additional exception properties
        public bool IncludeTargetSite { get; private set; } = true;
        public bool IncludeHelpLink { get; private set; } = true;
        public bool IncludeCustomProperties { get; private set; } = false;
        public List<string>? AdditionalPropertiesToInclude { get; private set; }

        // Culture information
        public CultureInfo CultureInfo { get; private set; } = CultureInfo.CurrentCulture;

        // Sensitive data handling
        public bool IncludeSensitiveData { get; private set; } = true;

        // Error severity mapping
        public Func<Exception, string>? ExceptionSeverityMapper { get; private set; }

        // StackTraceContext-specific properties
        public string PrependString { get; private set; } = "[";
        public string AppendString { get; private set; } = "]";
        public string SeparatorBetweenFileAndMethod { get; private set; } = "::";
        public string SeparatorBetweenMethodAndLine { get; private set; } = ":";
        public int MethodNameMaxLength { get; private set; } = 100;

        public bool IncludeLineNumberInStackTrace { get; private set; } = true;
        public string NoLineNumberText { get; private set; } = "[NoLineNumber]";

        // Exception message combination options
        public Func<string, string, string>? CombineMessageWithStackTrace { get; private set; }

        // Fluent configuration builder methods
        private StackParserConfig() { }

        /// <summary>
        /// Creates a new instance of ErrorParserConfig for fluent configuration.
        /// </summary>
        public static StackParserConfig Create() => new StackParserConfig();

        public StackParserConfig SetMaxRecursionDepth(int maxRecursionDepth)
        {
            if (maxRecursionDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRecursionDepth), "MaxRecursionDepth cannot be negative.");

            MaxRecursionDepth = maxRecursionDepth;
            return this;
        }

        public StackParserConfig SetStackFramesToSkip(int stackFramesToSkip)
        {
            if (stackFramesToSkip < 0)
                throw new ArgumentOutOfRangeException(nameof(stackFramesToSkip), "StackFramesToSkip cannot be negative.");

            StackFramesToSkip = stackFramesToSkip;
            return this;
        }

        public StackParserConfig SetMethodNamesToSkip(List<string>? methodNamesToSkip)
        {
            MethodNamesToSkip = methodNamesToSkip;
            return this;
        }

        public StackParserConfig SetDeclaringTypesToSkip(List<Type>? declaringTypesToSkip)
        {
            DeclaringTypesToSkip = declaringTypesToSkip;
            return this;
        }

        public StackParserConfig SetOutputFormat(bool outputJson)
        {
            OutputJson = outputJson;
            return this;
        }

        public StackParserConfig SetIndentationSpaces(int indentationSpaces)
        {
            if (indentationSpaces < 0)
                throw new ArgumentOutOfRangeException(nameof(indentationSpaces), "IndentationSpaces cannot be negative.");
            IndentationSpaces = indentationSpaces;
            return this;
        }

        public StackParserConfig SetIncludeStackTrace(bool includeStackTrace)
        {
            IncludeStackTrace = includeStackTrace;
            return this;
        }

        public StackParserConfig SetMaxLineLength(int maxLineLength)
        {
            if (maxLineLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLineLength), "MaxLineLength must be positive.");
            MaxLineLength = maxLineLength;
            return this;
        }

        public StackParserConfig SetIncludeTargetSite(bool includeTargetSite)
        {
            IncludeTargetSite = includeTargetSite;
            return this;
        }

        public StackParserConfig SetIncludeHelpLink(bool includeHelpLink)
        {
            IncludeHelpLink = includeHelpLink;
            return this;
        }

        public StackParserConfig SetIncludeCustomProperties(bool includeCustomProperties)
        {
            IncludeCustomProperties = includeCustomProperties;
            return this;
        }

        public StackParserConfig SetAdditionalPropertiesToInclude(List<string>? additionalPropertiesToInclude)
        {
            AdditionalPropertiesToInclude = additionalPropertiesToInclude;
            return this;
        }

        public StackParserConfig SetCultureInfo(CultureInfo cultureInfo)
        {
            CultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
            return this;
        }

        public StackParserConfig SetIncludeSensitiveData(bool includeSensitiveData)
        {
            IncludeSensitiveData = includeSensitiveData;
            return this;
        }

        public StackParserConfig SetExceptionSeverityMapper(Func<Exception, string> exceptionSeverityMapper)
        {
            ExceptionSeverityMapper = exceptionSeverityMapper ?? throw new ArgumentNullException(nameof(exceptionSeverityMapper));
            return this;
        }

        // New properties added from StackTraceContext
        public StackParserConfig SetPrependString(string prependString)
        {
            PrependString = prependString ?? throw new ArgumentNullException(nameof(prependString));
            return this;
        }

        public StackParserConfig SetAppendString(string appendString)
        {
            AppendString = appendString ?? throw new ArgumentNullException(nameof(appendString));
            return this;
        }

        public StackParserConfig SetSeparatorBetweenFileAndMethod(string separator)
        {
            SeparatorBetweenFileAndMethod = separator ?? throw new ArgumentNullException(nameof(separator));
            return this;
        }

        public StackParserConfig SetSeparatorBetweenMethodAndLine(string separator)
        {
            SeparatorBetweenMethodAndLine = separator ?? throw new ArgumentNullException(nameof(separator));
            return this;
        }

        public StackParserConfig SetMethodNameMaxLength(int maxLength)
        {
            if (maxLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), "MethodNameMaxLength must be positive.");
            MethodNameMaxLength = maxLength;
            return this;
        }

        public StackParserConfig SetIncludeLineNumberInStackTrace(bool includeLineNumber)
        {
            IncludeLineNumberInStackTrace = includeLineNumber;
            return this;
        }

        public StackParserConfig SetNoLineNumberText(string noLineNumberText)
        {
            NoLineNumberText = noLineNumberText ?? throw new ArgumentNullException(nameof(noLineNumberText));
            return this;
        }

        public StackParserConfig SetCombineMessageWithStackTrace(Func<string, string, string> combineFunc)
        {
            CombineMessageWithStackTrace = combineFunc ?? throw new ArgumentNullException(nameof(combineFunc));
            return this;
        }

        /// <summary>
        /// Finalizes the configuration. Use this method to validate and build the configuration.
        /// </summary>
        public StackParserConfig Build()
        {
            // Additional validation logic can be added here
            return this;
        }
    }
}
