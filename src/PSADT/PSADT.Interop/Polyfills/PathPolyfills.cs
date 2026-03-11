#if !NETCOREAPP2_1_OR_GREATER
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.IO
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for Path.IsPathFullyQualified on .NET Framework 4.7.2.
    /// Provides a static method to determine whether a path is fully qualified.
    /// </summary>
    internal static class PathPolyfills
    {
        /// <summary>
        /// Provides extension methods for path qualification checks.
        /// </summary>
        extension(Path)
        {
            /// <summary>
            /// Returns true if the path is fixed to a specific drive or UNC path. This method does no
            /// validation of the path (URIs will be returned as relative as a result).
            /// Returns false if the path specified is relative to the current drive or working directory.
            /// </summary>
            /// <remarks>
            /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
            /// assume that rooted paths <see cref="Path.IsPathRooted(string)"/> are not relative.  This isn't the case.
            /// "C:a" is drive relative- meaning that it will be resolved against the current directory
            /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
            /// will not be used to modify the path).
            /// </remarks>
            /// <exception cref="ArgumentException">
            /// Thrown if <paramref name="path"/> is empty or whitespace.
            /// </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static bool IsPathFullyQualified(string path)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(path);
                return !IsPartiallyQualified(path.AsSpan());
            }

            /// <summary>
            /// Returns true if the path is fixed to a specific drive or UNC path. This method does no
            /// validation of the path (URIs will be returned as relative as a result).
            /// Returns false if the path specified is relative to the current drive or working directory.
            /// </summary>
            /// <remarks>
            /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
            /// assume that rooted paths <see cref="Path.IsPathRooted(string)"/> are not relative.  This isn't the case.
            /// "C:a" is drive relative- meaning that it will be resolved against the current directory
            /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
            /// will not be used to modify the path).
            /// </remarks>
            /// <exception cref="ArgumentException">
            /// Thrown if <paramref name="path"/> is empty or whitespace.
            /// </exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static bool IsPathFullyQualified(ReadOnlySpan<char> path)
            {
                return !IsPartiallyQualified(path);
            }
        }

        /// <summary>
        /// Returns true if the path specified is relative to the current drive or working directory.
        /// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
        /// validation of the path (URIs will be returned as relative as a result).
        /// </summary>
        /// <remarks>
        /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
        /// assume that rooted paths <see cref="Path.IsPathRooted(string)"/> are not relative.  This isn't the case.
        /// "C:a" is drive relative- meaning that it will be resolved against the current directory
        /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
        /// will not be used to modify the path).
        /// </remarks>
        [Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Visual Studio 18.4.0 thinks this is unused...")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPartiallyQualified(ReadOnlySpan<char> path)
        {
            ArgumentException.ThrowIfEmptyOrWhiteSpace(path);
            if (path.Length < 2)
            {
                // It isn't fixed, it must be relative.  There is no way to specify a fixed
                // path with one character (or less).
                return true;
            }

            if (IsDirectorySeparator(path[0]))
            {
                // There is no valid way to specify a relative path with two initial slashes or
                // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
                return !(path[1] == '?' || IsDirectorySeparator(path[1]));
            }

            // The only way to specify a fixed path that doesn't begin with two slashes
            // is the drive, colon, slash format- i.e. C:\
            return !((path.Length >= 3)
                && (path[1] == Path.VolumeSeparatorChar)
                && IsDirectorySeparator(path[2])
                // To match old behavior we'll check the drive character for validity as the path is technically
                // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
                && IsValidDriveChar(path[0]));
        }

        /// <summary>
        /// True if the given character is a directory separator.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirectorySeparator(char c)
        {
            return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
        }

        /// <summary>
        /// Returns true if the given character is a valid drive letter
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidDriveChar(char value)
        {
            return (uint)((value | 0x20) - 'a') <= ('z' - 'a');
        }
    }
}
#endif
