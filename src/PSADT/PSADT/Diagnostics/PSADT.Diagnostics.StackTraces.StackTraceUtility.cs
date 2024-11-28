using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// Provides utility methods for filtering, manipulating, and formatting stack traces.
    /// </summary>
    public static class StackTraceUtility
    {
        /// <summary>
        /// Retrieves the first relevant stack frame after applying filtering criteria based on the provided configuration.
        /// </summary>
        /// <param name="caughtException">The caught exception from which the stack frames are filtered.</param>
        /// <param name="config">The configuration object containing the filtering criteria (method names, declaring types, etc.).</param>
        /// <param name="framesToSkip">The number of frames to skip before filtering begins.</param>
        /// <param name="maxRecursionDepth">The maximum recursion depth to search for relevant stack frames.</param>
        /// <returns>Returns a <see cref="StackTraceContext"/> containing the first relevant stack frame and its details.
        /// If no relevant stack frames are found, an empty <see cref="StackTraceContext"/> is returned.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StackFrame GetFirstRelevantStackFrame(
            [Optional, DefaultParameterValue(null)] Exception? caughtException,
            int? framesToSkip = 0,
            int? maxRecursionDepth = 0,
            StackParserConfig? config = null)
        {
            if (caughtException == null)
            {
                config = StackParserConfig.Create()
                .SetStackFramesToSkip((int)framesToSkip!)
                .SetMaxRecursionDepth((int)maxRecursionDepth!);
            }

            return FilterStackTrace(caughtException, config!)[0];
        }

        /// <summary>
        /// Creates a string representation of the filtered stack trace from the given frames.
        /// </summary>
        /// <param name="filteredFrames">An array of <see cref="StackFrame"/> objects representing the filtered stack trace.</param>
        /// <returns>A formatted string representing the filtered stack trace.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CreateFilteredStackTrace(StackFrame[] filteredFrames)
        {
            StringBuilder stackTraceBuilder = new StringBuilder();

            // Retrieve all filtered frames
            foreach (StackFrame frame in filteredFrames)
            {
                stackTraceBuilder.AppendLine(frame.ToString());
            }

            return stackTraceBuilder.Length > 0
                ? stackTraceBuilder.ToString()
                : string.Empty;
        }

        /// <summary>
        /// Combines the original exception message with the filtered stack trace to create a detailed exception message.
        /// </summary>
        /// <param name="exceptionMessage">The original exception message.</param>
        /// <param name="filteredStackTrace">The filtered stack trace to append to the exception message.</param>
        /// <returns>A string combining the original exception message with the filtered stack trace.</returns>
        public static string CombineExceptionMessageAndFilteredStackTrace(string exceptionMessage, string filteredStackTrace)
        {
            return $"{exceptionMessage}{Environment.NewLine}Filtered Stack Trace:{Environment.NewLine}{filteredStackTrace}";
        }

        /// <summary>
        /// Rethrows an exception with a filtered stack trace while preserving the original exception as the inner exception.
        /// </summary>
        /// <param name="combinedMessageAndFilteredStackTrace">The combined exception message and filtered stack trace.</param>
        /// <param name="originalException">The original exception to preserve as the inner exception.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RethrowExceptionWithFilteredStackTrace(string combinedMessageAndFilteredStackTrace, Exception originalException)
        {
            // Create an ErrorRecord to represent the error
            ErrorRecord errorRecord = new ErrorRecord(
                originalException,                  // The underlying exception that caused the error
                "FilteredStackTraceError",          // A unique identifier for the error
                ErrorCategory.NotSpecified,         // The error category (choose appropriately)
                null);                              // Optional: A target object related to the error

            // Set detailed error message (combined message and filtered stack trace)
            errorRecord.ErrorDetails = new ErrorDetails(combinedMessageAndFilteredStackTrace);

            // Throw a RuntimeException with the ErrorRecord
            throw new RuntimeException(combinedMessageAndFilteredStackTrace, originalException, errorRecord);
        }

        /// <summary>
        /// Rethrows the specified exception after modifying its stack trace to a custom stack trace string.
        /// </summary>
        /// <param name="exception">The exception to rethrow with a modified stack trace.</param>
        /// <param name="customStackTrace">The custom stack trace string to set on the exception.</param>
        /// <param name="internalStackTraceStringFieldName">The name of the exception's internal field used to store the stack trace string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        /// <remarks>
        /// This method uses reflection to get the internal <c>_stackTraceString</c> field of the exception.
        /// </remarks>
        public static string? GetStackTraceString(Exception exception, string customStackTrace, string internalStackTraceStringFieldName = "_stackTraceString")
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            string? stackTraceString = null;
            try
            {
                // Use reflection to get the StackTrace property
                stackTraceString = typeof(Exception)
                                    .GetField(internalStackTraceStringFieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                                    ?.GetValue(exception)
                                    ?.ToString();
            }
            catch
            {
                // Suppress any error
            }

            return stackTraceString;
        }

        /// <summary>
        /// Rethrows the specified exception after modifying its stack trace to a custom stack trace string.
        /// </summary>
        /// <param name="exception">The exception to rethrow with a modified stack trace.</param>
        /// <param name="customStackTrace">The custom stack trace string to set on the exception.</param>
        /// <param name="internalStackTraceStringFieldName">The name of the exception's internal field used to store the stack trace string.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
        /// <remarks>
        /// This method uses reflection to modify the internal <c>_stackTraceString</c> field of the exception.
        /// </remarks>
        public static void RethrowWithModifiedStackTrace(Exception exception, string customStackTrace, string internalStackTraceStringFieldName = "_stackTraceString")
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            try
            {
                // Use reflection to set the StackTrace property
                typeof(Exception)
                 .GetField(internalStackTraceStringFieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                 ?.SetValue(exception, customStackTrace);
            }
            catch
            {
                // Suppress any error
            }

            throw exception;
        }

        /// <summary>
        /// Filters the stack trace frames based on the provided configuration, skipping frames, 
        /// method names, and declaring types specified in the configuration.
        /// </summary>
        /// <param name="caughtException">The exception from which to extract and filter the stack trace.</param>
        /// <param name="config">Configuration used to determine which frames to skip.</param>
        /// <returns>An array of <see cref="StackFrame"/> objects representing the filtered stack trace.</returns>
        private static StackFrame[] FilterStackTrace([Optional, DefaultParameterValue(null)] Exception? caughtException, StackParserConfig config)
        {
            if (caughtException == null || config == null)
                return Array.Empty<StackFrame>();

            StackTrace stackTrace;
            try
            {
                if (caughtException == null)
                {
                    stackTrace = new StackTrace(config.StackFramesToSkip, true);
                }
                else
                {
                    stackTrace = new StackTrace(caughtException, config.StackFramesToSkip, true);
                }
            }
            catch
            {
                return Array.Empty<StackFrame>();
            }

            StackFrame[] frames = stackTrace.GetFrames();
            if (frames == null || frames.Length == 0)
                return Array.Empty<StackFrame>();

            return FilterStackFrames(frames, config);
        }

        /// <summary>
        /// Filters the provided stack frames based on the configuration criteria, skipping 
        /// frames as necessary according to the settings for method names and declaring types.
        /// </summary>
        /// <param name="frames">The array of <see cref="StackFrame"/> objects to filter.</param>
        /// <param name="config">Configuration used to determine which frames to skip.</param>
        /// <returns>An array of filtered <see cref="StackFrame"/> objects.</returns>
        private static StackFrame[] FilterStackFrames(StackFrame[] frames, StackParserConfig config)
        {
            List<StackFrame> filteredFrames = new List<StackFrame>();
            int framesToSkip = config.StackFramesToSkip;
            int maxRecursionDepth = config.MaxRecursionDepth;

            int recursionDepth = -1;
            foreach (StackFrame frame in frames)
            {
                if (recursionDepth++ > maxRecursionDepth)
                    break;

                if (ShouldSkipFrame(frame, ref framesToSkip, config))
                    continue;

                filteredFrames.Add(frame);
            }

            return filteredFrames.ToArray();
        }

        /// <summary>
        /// Determines whether a stack frame should be skipped based on the configuration settings.
        /// </summary>
        /// <param name="frame">The <see cref="StackFrame"/> to evaluate.</param>
        /// <param name="framesToSkip">The number of frames to skip before evaluation begins.</param>
        /// <param name="config">Configuration used to determine the criteria for skipping frames.</param>
        /// <returns><c>true</c> if the frame should be skipped, <c>false</c> otherwise.</returns>
        private static bool ShouldSkipFrame(StackFrame frame, ref int framesToSkip, StackParserConfig config)
        {
            if (framesToSkip > 0)
            {
                framesToSkip--;
                return true;
            }

            var method = frame.GetMethod();
            return method == null || ShouldSkipByMethodOrType(method, config);
        }

        /// <summary>
        /// Determines whether the method associated with a stack frame should be skipped based on the 
        /// method name or declaring type specified in the configuration.
        /// </summary>
        /// <param name="method">The <see cref="MethodBase"/> object representing the method in the stack frame.</param>
        /// <param name="config">Configuration used to determine the criteria for skipping methods.</param>
        /// <returns><c>true</c> if the method should be skipped, <c>false</c> otherwise.</returns>
        private static bool ShouldSkipByMethodOrType(MethodBase method, StackParserConfig config)
        {
            if (config.MethodNamesToSkip != null && config.MethodNamesToSkip.Contains(method.Name))
                return true;

            var declaringType = method.DeclaringType;
            return declaringType != null && config.DeclaringTypesToSkip != null && config.DeclaringTypesToSkip.Contains(declaringType);
        }
    }
}
