using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace PSADT.Interop.Extensions
{
    /// <summary>
    /// Provides extension methods for performing common string validation and manipulation tasks.
    /// </summary>
    /// <remarks>The methods in this class extend the functionality of string objects, enabling convenient
    /// validation and utility operations. These extensions are intended to simplify input checking and other
    /// string-related logic throughout an application.</remarks>
    internal static class StringExtensions
    {
        /// <summary>
        /// Throws an exception if the specified string is null, empty, or consists only of white-space characters.
        /// </summary>
        /// <remarks>Use this method to enforce that string parameters are not null, empty, or white-space
        /// in method calls. This is useful for validating input and ensuring that required string values are
        /// provided.</remarks>
        /// <param name="value">The string to validate. This value must not be null, empty, or contain only white-space characters.</param>
        /// <param name="name">The name of the parameter or member invoking this method. Used to identify the argument in the exception
        /// message.</param>
        /// <returns>The original string value if it is not null, empty, or white-space.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null, empty, or consists only of white-space characters.</exception>
        internal static string ThrowIfNullOrWhiteSpace(this string? value, [CallerMemberName] string name = null!)
        {
            return string.IsNullOrWhiteSpace(value) ? throw new ArgumentNullException(name) : value!;
        }

        /// <summary>
        /// Validates that the specified directory path exists and throws an exception if it does not.
        /// </summary>
        /// <remarks>Use this method to ensure a directory exists before performing operations that
        /// require its presence. This helps prevent errors caused by missing directories and provides clear exception
        /// messages for troubleshooting.</remarks>
        /// <param name="value">The directory path to validate. Must not be null, empty, or consist only of white-space characters.</param>
        /// <param name="name">The name of the member invoking this method, used for error reporting.</param>
        /// <returns>The original directory path if it exists.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="value"/> does not point to an existing directory.</exception>
        internal static string ThrowIfDirectoryDoesNotExist(this string? value, [CallerMemberName] string name = null!)
        {
            return !Directory.Exists(value.ThrowIfNullOrWhiteSpace(name))
                ? throw new DirectoryNotFoundException($"The specified directory '{value}' does not exist.")
                : value!;
        }

        /// <summary>
        /// Validates that the specified file path refers to an existing file and throws an exception if the file does
        /// not exist.
        /// </summary>
        /// <remarks>Use this method to ensure a file exists before performing file operations. This
        /// extension method is intended for validating file paths and will throw an exception if the file is missing,
        /// preventing further processing.</remarks>
        /// <param name="value">The path of the file to check for existence. Must not be null, empty, or consist solely of whitespace.</param>
        /// <param name="name">The name of the calling member. Automatically supplied by the CallerMemberName attribute.</param>
        /// <returns>The original file path if the file exists.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by <paramref name="value"/> does not exist.</exception>
        internal static string ThrowIfFileDoesNotExist(this string? value, [CallerMemberName] string name = null!)
        {
            return !File.Exists(value.ThrowIfNullOrWhiteSpace(name))
                ? throw new FileNotFoundException($"The specified file '{value}' does not exist.", value)
                : value!;
        }

        /// <summary>
        /// Validates that the specified file path is not null or whitespace and that its directory exists.
        /// </summary>
        /// <remarks>Use this method to ensure that a file path is valid and its directory is present
        /// before performing file operations. This helps prevent errors related to missing directories at
        /// runtime.</remarks>
        /// <param name="value">The file path to validate. This value must not be null or consist only of white-space characters.</param>
        /// <param name="name">The name of the calling member, used for error reporting if the validation fails.</param>
        /// <returns>The original file path if the file exists or its directory exists; otherwise, an exception is thrown.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory for the specified file path does not exist.</exception>
        internal static string ThrowIfFileDirectoryDoesNotExist(this string? value, [CallerMemberName] string name = null!)
        {
            return !File.Exists(value.ThrowIfNullOrWhiteSpace(name)) && (Path.GetDirectoryName(value) is not string directory || !Directory.Exists(directory))
                ? throw new DirectoryNotFoundException($"The specified directory for path '{value}' does not exist.")
                : value!;
        }
    }
}
