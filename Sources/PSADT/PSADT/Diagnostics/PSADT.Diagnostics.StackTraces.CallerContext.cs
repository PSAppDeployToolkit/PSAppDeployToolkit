using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PSADT.Diagnostics.StackTraces
{
    /// <summary>
    /// Represents caller information, including method name, file path, and line number.
    /// Provides utility methods to format and retrieve caller information.
    /// </summary>
    public class CallerContext
    {
        /// <summary>
        /// Gets the method name where the caller information was captured.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Gets the file path where the method was declared.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the line number in the file where the method is located.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallerContext"/> class.
        /// If caller information is not explicitly provided, it is automatically retrieved using <see cref="GetCallerInfo"/>.
        /// </summary>
        /// <param name="methodName">Optional: The name of the method where the caller information is retrieved from.</param>
        /// <param name="fileName">Optional: The file path where the method is located.</param>
        /// <param name="lineNumber">Optional: The line number within the file where the method is defined.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CallerContext(
            string? methodName = null,
            string? fileName = null,
            int lineNumber = 0)
        {
            // Retrieve caller information if not provided
            if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(fileName) || lineNumber == 0)
            {
                var callerInfo = GetCallerInfo();
                MethodName = callerInfo.MethodName;
                FileName = callerInfo.FileName;
                LineNumber = callerInfo.LineNumber;
            }
            else
            {
                MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
                FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
                LineNumber = lineNumber;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CallerContext"/> class using the provided <see cref="StackFrame"/>.
        /// </summary>
        /// <param name="frame">The <see cref="StackFrame"/> object containing caller information.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CallerContext(StackFrame? frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));

            var method = frame.GetMethod();
            MethodName = method != null ? $"{method.DeclaringType?.FullName}.{method.Name}" : "[UnknownMethod]";
            FileName = frame?.GetFileName() ?? "[UnknownFile]";
            LineNumber = frame?.GetFileLineNumber() > 0 ? frame.GetFileLineNumber() : -1;
        }

        /// <summary>
        /// Retrieves the caller information without using a <see cref="StackFrame"/>. This method uses
        /// compiler attributes like <see cref="CallerMemberName"/>, <see cref="CallerFilePath"/>, and <see cref="CallerLineNumber"/>.
        /// </summary>
        /// <param name="callerMethodName">The name of the calling method. This parameter is automatically set by the compiler.</param>
        /// <param name="callerFilePath">The file path where the calling method is located. This parameter is automatically set by the compiler.</param>
        /// <param name="callerLineNumber">The line number within the file where the calling method is defined. This parameter is automatically set by the compiler.</param>
        /// <returns>A new instance of <see cref="CallerContext"/> with the collected caller information.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CallerContext GetCallerInfo(
            [System.Runtime.CompilerServices.CallerMemberName] string callerMethodName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string callerFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int callerLineNumber = 0)
        {
            return new CallerContext(callerMethodName, callerFilePath, callerLineNumber);
        }

        /// <summary>
        /// Overload for <see cref="FormatCallerContext"/> that uses the class's internal properties for caller information.
        /// </summary>
        /// <param name="prependString">A string to prepend to the formatted output. Default is "[".</param>
        /// <param name="separatorBetweenFileAndMethod">Separator between the file name and method name. Default is "::".</param>
        /// <param name="separatorBetweenMethodAndLine">Separator between the method name and line number. Default is ":".</param>
        /// <param name="appendString">A string to append to the formatted output. Default is "]".</param>
        /// <param name="includeFullPath">If true, includes the full file path. If false, includes only the file name. Default is false.</param>
        /// <param name="fileNameParts">The number of parts of the file name to include. Default is 2 (the last two parts).</param>
        /// <param name="methodNameMaxLength">The maximum length to truncate the method name to. Default is -1 (no truncation).</param>
        /// <param name="truncateAtChar">Optional: If provided, truncates the method name at the first occurrence of this character.</param>
        /// <returns>A formatted string representing the caller context using the class instance's properties.</returns>
        public string FormatCallerContextDefault(
            string prependString = "[",
            string separatorBetweenFileAndMethod = "::",
            string separatorBetweenMethodAndLine = ":",
            string appendString = "]",
            bool includeFullPath = false,
            int fileNameParts = 2,
            int methodNameMaxLength = -1,
            char? truncateAtChar = null)
        {
            return FormatCallerContext(
                prependString: prependString,
                separatorBetweenFileAndMethod: separatorBetweenFileAndMethod,
                separatorBetweenMethodAndLine: separatorBetweenMethodAndLine,
                appendString: appendString,
                includeFullPath: includeFullPath,
                fileNameParts: fileNameParts,
                methodNameMaxLength: methodNameMaxLength,
                truncateAtChar: truncateAtChar);
        }

        /// <summary>
        /// Formats the caller context information with customizable separators, number of file name parts, and optional
        /// prepend/append strings. This method automatically uses the caller information stored in the class instance.
        /// </summary>
        /// <param name="prependString">A string to prepend to the formatted output. Default is "[".</param>
        /// <param name="separatorBetweenFileAndMethod">Separator between the file name and method name. Default is "::".</param>
        /// <param name="separatorBetweenMethodAndLine">Separator between the method name and line number. Default is ":".</param>
        /// <param name="appendString">A string to append to the formatted output. Default is "]".</param>
        /// <param name="includeFullPath">If true, includes the full file path. If false, includes only the file name. Default is false.</param>
        /// <param name="fileNameParts">The number of parts of the file name to include. Default is 2 (the last two parts).</param>
        /// <param name="methodNameMaxLength">The maximum length to truncate the method name to. Default is -1 (no truncation).</param>
        /// <param name="truncateAtChar">Optional: If provided, truncates the method name at the first occurrence of this character.</param>
        /// <returns>A formatted string representing the caller context.</returns>
        private string FormatCallerContext(
            string prependString = "[",
            string separatorBetweenFileAndMethod = "::",
            string separatorBetweenMethodAndLine = ":",
            string appendString = "]",
            bool includeFullPath = false,
            int fileNameParts = 2,
            int methodNameMaxLength = -1,
            char? truncateAtChar = null)
        {
            var formattedFileName = FormatFileName(includeFullPath ? FileName : Path.GetFileName(FileName), fileNameParts);
            var formattedMethodName = TruncateMethodName(MethodName, methodNameMaxLength, truncateAtChar);

            return $"{prependString}{formattedFileName}{separatorBetweenFileAndMethod}{formattedMethodName}{separatorBetweenMethodAndLine}{LineNumber}{appendString}";
        }

        /// <summary>
        /// Formats the caller context information with customizable separators, number of file name parts, and optional
        /// prepend/append strings. If no caller information is provided, the information from the class instance is used.
        /// </summary>
        /// <param name="methodName">Optional: The method name to format. Defaults to the method name stored in the instance.</param>
        /// <param name="filePath">Optional: The file path to format. Defaults to the file path stored in the instance.</param>
        /// <param name="lineNumber">Optional: The line number to format. Defaults to the line number stored in the instance.</param>
        /// <param name="prependString">A string to prepend to the formatted output. Default is "[".</param>
        /// <param name="separatorBetweenFileAndMethod">Separator between the file name and method name. Default is "::".</param>
        /// <param name="separatorBetweenMethodAndLine">Separator between the method name and line number. Default is ":".</param>
        /// <param name="appendString">A string to append to the formatted output. Default is "]".</param>
        /// <param name="includeFullPath">If true, includes the full file path. If false, includes only the file name. Default is false.</param>
        /// <param name="fileNameParts">The number of parts of the file name to include. Default is 2 (the last two parts).</param>
        /// <param name="methodNameMaxLength">The maximum length to truncate the method name to. Default is -1 (no truncation).</param>
        /// <param name="truncateAtChar">Optional: If provided, truncates the method name at the first occurrence of this character.</param>
        /// <returns>A formatted string representing the caller context.</returns>
        public string FormatCallerContext(
            string? methodName = null,
            string? filePath = null,
            int? lineNumber = null,
            string prependString = "[",
            string separatorBetweenFileAndMethod = "::",
            string separatorBetweenMethodAndLine = ":",
            string appendString = "]",
            bool includeFullPath = false,
            int fileNameParts = 2,
            int methodNameMaxLength = -1,
            char? truncateAtChar = null)
        {
            // Use instance values if not provided
            methodName ??= MethodName;
            filePath ??= FileName;
            lineNumber ??= LineNumber;

            var formattedFileName = FormatFileName(includeFullPath ? filePath : Path.GetFileName(filePath), fileNameParts);
            var formattedMethodName = TruncateMethodName(methodName, methodNameMaxLength, truncateAtChar);

            return $"{prependString}{formattedFileName}{separatorBetweenFileAndMethod}{formattedMethodName}{separatorBetweenMethodAndLine}{lineNumber}{appendString}";
        }

        /// <summary>
        /// Truncates the method name to the specified maximum length, or at the first occurrence of the specified character if provided.
        /// </summary>
        /// <param name="methodName">The method name to truncate.</param>
        /// <param name="maxLength">The maximum length of the method name. If -1, no truncation is applied.</param>
        /// <param name="truncateAtChar">Optional: If provided, truncates the method name at the first occurrence of this character.</param>
        /// <returns>The truncated method name or the original if no truncation is needed.</returns>
        public static string TruncateMethodName(string methodName, int maxLength = -1, char? truncateAtChar = null)
        {
            if (truncateAtChar.HasValue)
            {
                var index = methodName.IndexOf(truncateAtChar.Value);
                if (index >= 0)
                {
                    methodName = methodName.Substring(0, index);
                }
            }

            if (maxLength >= 0 && methodName.Length > maxLength)
            {
                methodName = methodName.Substring(0, maxLength) + "...";
            }

            return methodName;
        }

        /// <summary>
        /// Formats the file name to include the last 'n' parts, separated by dots.
        /// </summary>
        /// <param name="fileName">The file name to format.</param>
        /// <param name="fileNameParts">The number of parts to include from the file name. Default is 2.</param>
        /// <returns>The formatted file name including the last 'n' parts.</returns>
        public static string FormatFileName(string? fileName, int fileNameParts = 2)
        {
            if (string.IsNullOrEmpty(fileName)) return "[UnknownFile]";

            var fileNamePartsArray = fileName!.Split('.');
            if (fileNamePartsArray.Length <= fileNameParts)
            {
                // If there are fewer parts than requested, return the full file name
                return fileName;
            }

            // Get the last 'n' parts
            int startIndex = fileNamePartsArray.Length - fileNameParts;
            string[] lastParts = new string[fileNameParts];
            Array.Copy(fileNamePartsArray, startIndex, lastParts, 0, fileNameParts);

            return string.Join(".", lastParts);
        }
    }
}
