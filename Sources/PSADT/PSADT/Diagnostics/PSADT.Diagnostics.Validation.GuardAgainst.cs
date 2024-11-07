using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace PSADT.Diagnostics.Validation
{
    /// <summary>
    /// Provides guard clause methods for validating arguments and ensuring they meet certain conditions.
    /// </summary>
    public static class GuardAgainst
    {
        /// <summary>
        /// Throws an <see cref="ArgumentNullException"/> if the specified argument is <c>null</c>.
        /// </summary>
        /// <typeparam name="T">The type of the argument to check.</typeparam>
        /// <param name="argument">The argument to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="argument"/> is <c>null</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>(T argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified string is <c>null</c> or empty.
        /// </summary>
        /// <param name="argument">The string to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argument"/> is <c>null</c> or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrEmpty(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified string is <c>null</c>, empty, or consists only of white-space characters.
        /// </summary>
        /// <param name="argument">The string to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argument"/> is <c>null</c>, empty, or only white-space.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNullOrWhiteSpace(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("Argument cannot be null, empty, or only whitespace.", nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified integer is negative.
        /// </summary>
        /// <param name="argument">The integer to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="argument"/> is negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNegative(int argument)
        {
            if (argument < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(argument), "Argument cannot be negative.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified integer is outside the specified range.
        /// </summary>
        /// <param name="argument">The integer to validate.</param>
        /// <param name="min">The minimum allowable value.</param>
        /// <param name="max">The maximum allowable value.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="argument"/> is outside the specified range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfOutOfRange(int argument, int min, int max)
        {
            if (argument < min || argument > max)
            {
                throw new ArgumentOutOfRangeException(nameof(argument), $"Argument must be between {min} and {max}.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified argument is the default value of its type.
        /// </summary>
        /// <typeparam name="T">The value type to check.</typeparam>
        /// <param name="argument">The argument to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argument"/> is the default value for its type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfDefault<T>(T argument) where T : struct
        {
            if (EqualityComparer<T>.Default.Equals(argument, default))
            {
                throw new ArgumentException("Argument cannot be the default value.", nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified condition is <c>false</c>.
        /// </summary>
        /// <param name="condition">The condition to validate.</param>
        /// <param name="message">The message to include in the exception if the condition is <c>false</c>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="condition"/> is <c>false</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFalse(bool condition, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified enum value is not defined in the enum.
        /// </summary>
        /// <typeparam name="TEnum">The enum type to validate.</typeparam>
        /// <param name="enumValue">The enum value to check.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="enumValue"/> is not defined in the enum.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfInvalidEnumValue<TEnum>(TEnum enumValue) where TEnum : struct, Enum
        {
            if (!Enum.IsDefined(typeof(TEnum), enumValue))
            {
                throw new ArgumentException($"Invalid enum value: {enumValue}", nameof(enumValue));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified value is not positive.
        /// </summary>
        /// <param name="argument">The numeric value to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="argument"/> is not positive.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotPositive(int argument)
        {
            if (argument <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(argument), "Argument must be positive.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified date is not in the future.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="date"/> is not in the future.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotInFuture(DateTime date)
        {
            if (date <= DateTime.Now)
            {
                throw new ArgumentOutOfRangeException(nameof(date), "The date must be in the future.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentOutOfRangeException"/> if the specified date is not in the past.
        /// </summary>
        /// <param name="date">The date to validate.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="date"/> is not in the past.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotInPast(DateTime date)
        {
            if (date >= DateTime.Now)
            {
                throw new ArgumentOutOfRangeException(nameof(date), "The date must be in the past.");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified collection is <c>null</c> or empty.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="collection">The collection to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="collection"/> is <c>null</c> or empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfCollectionNullOrEmpty<T>(IEnumerable<T> collection)
        {
            if (collection == null || !collection.Any())
            {
                throw new ArgumentException("Collection cannot be null or empty.", nameof(collection));
            }
        }

        /// <summary>
        /// Throws an <see cref="FileNotFoundException"/> if the specified file does not exist.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <exception cref="FileNotFoundException">Thrown when the <paramref name="filePath"/> does not point to an existing file.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFileNotExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File does not exist.", filePath);
            }
        }

        /// <summary>
        /// Throws an <see cref="DirectoryNotFoundException"/> if the specified directory does not exist.
        /// </summary>
        /// <param name="directoryPath">The directory path to validate.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown when the <paramref name="directoryPath"/> does not point to an existing directory.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfDirectoryNotExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified string exceeds the maximum allowable length.
        /// </summary>
        /// <param name="argument">The string to validate.</param>
        /// <param name="maxLength">The maximum allowable length for the string.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argument"/> exceeds the maximum allowable length.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfStringExceedsMaxLength(string argument, int maxLength)
        {
            if (argument?.Length > maxLength)
            {
                throw new ArgumentException($"Argument exceeds maximum length of {maxLength}.", nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified string does not match the provided regular expression pattern.
        /// </summary>
        /// <param name="argument">The string to validate.</param>
        /// <param name="pattern">The regular expression pattern that the string must match.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argument"/> does not match the specified pattern.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfStringDoesNotMatchPattern(string argument, string pattern)
        {
            if (!Regex.IsMatch(argument, pattern))
            {
                throw new ArgumentException($"Argument does not match the required pattern: {pattern}.", nameof(argument));
            }
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> if the specified GUID is <see cref="Guid.Empty"/>.
        /// </summary>
        /// <param name="guid">The GUID to validate.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="guid"/> is <see cref="Guid.Empty"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfGuidEmpty(Guid guid)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentException("GUID cannot be empty.", nameof(guid));
            }
        }
    }
}
