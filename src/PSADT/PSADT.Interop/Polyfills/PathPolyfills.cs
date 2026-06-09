#if !NETCOREAPP2_1_OR_GREATER
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.IO
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for modern <see cref="Path"/> APIs on .NET Framework 4.7.2,
    /// including <c>IsPathFullyQualified</c> and <c>Join</c>.
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
                // Internal workers to avoid IDE0051 false positives.
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static bool IsDirectorySeparator(char c)
                {
                    return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static bool IsValidDriveChar(char value)
                {
                    return (uint)((value | 0x20) - 'a') <= ('z' - 'a');
                }

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
            /// Concatenates two path components into a single path, inserting a directory separator between them if necessary.
            /// Unlike <see cref="Path.Combine(string, string)"/>, this method does not root the result when <paramref name="path2"/> is absolute.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(string? path1, string? path2)
            {
                return Join(path1.AsSpan(), path2.AsSpan());
            }

            /// <summary>
            /// Concatenates three path components into a single path, inserting directory separators between them as necessary.
            /// Unlike <see cref="Path.Combine(string, string, string)"/>, this method does not root the result when a later component is absolute.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(string? path1, string? path2, string? path3)
            {
                return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan());
            }

            /// <summary>
            /// Concatenates four path components into a single path, inserting directory separators between them as necessary.
            /// Unlike <see cref="Path.Combine(string, string, string, string)"/>, this method does not root the result when a later component is absolute.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(string? path1, string? path2, string? path3, string? path4)
            {
                return Join(path1.AsSpan(), path2.AsSpan(), path3.AsSpan(), path4.AsSpan());
            }

            /// <summary>
            /// Concatenates two path spans into a single path.
            /// </summary>
            [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2234:Arguments should be passed in the same order as the method parameters", Justification = "This is deliberate.")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2)
            {
                return path1.Length == 0
                    ? path2.ToString()
                    : path2.Length == 0
                    ? path1.ToString()
                    : JoinInternal(path1, path2, default, default);
            }

            /// <summary>
            /// Concatenates three path spans into a single path.
            /// </summary>
            [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2234:Arguments should be passed in the same order as the method parameters", Justification = "This is deliberate.")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3)
            {
                return path1.Length == 0
                    ? Join(path2, path3)
                    : path2.Length == 0
                    ? Join(path1, path3)
                    : path3.Length == 0
                    ? Join(path1, path2)
                    : JoinInternal(path1, path2, path3, default);
            }

            /// <summary>
            /// Concatenates four path spans into a single path.
            /// </summary>
            [Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2234:Arguments should be passed in the same order as the method parameters", Justification = "This is deliberate.")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [StackTraceHidden]
            public static string Join(ReadOnlySpan<char> path1, ReadOnlySpan<char> path2, ReadOnlySpan<char> path3, ReadOnlySpan<char> path4)
            {
                return path1.Length == 0
                    ? Join(path2, path3, path4)
                    : path2.Length == 0
                    ? Join(path1, path3, path4)
                    : path3.Length == 0
                    ? Join(path1, path2, path4)
                    : path4.Length == 0
                    ? Join(path1, path2, path3)
                    : JoinInternal(path1, path2, path3, path4);
            }

            /// <summary>
            /// Concatenates an array of path components into a single path.
            /// </summary>
            [StackTraceHidden]
            public static string Join(params string?[] paths)
            {
                // Validate input.
                ArgumentNullException.ThrowIfNull(paths);
                if (paths.Length == 0)
                {
                    return string.Empty;
                }

                // Calculate buffer size.
                int maxSize = 0;
                foreach (string? path in paths)
                {
                    maxSize += path?.Length ?? 0;
                }
                maxSize += paths.Length - 1;

                // Write paths to buffer.
                Span<char> buffer = maxSize <= 260 ? stackalloc char[maxSize] : new char[maxSize];
                int written = 0;
                for (int i = 0; i < paths.Length; i++)
                {
                    ReadOnlySpan<char> segment = paths[i].AsSpan();
                    if (segment.Length == 0)
                    {
                        continue;
                    }
                    if (written > 0 && !EndsInDirectorySeparator(buffer[..written]))
                    {
                        buffer[written++] = Path.DirectorySeparatorChar;
                    }
                    segment.CopyTo(buffer[written..]);
                    written += segment.Length;
                }
                return buffer[..written].ToString();
            }

            /// <summary>
            /// Combines up to four path segments into a single path string, inserting directory separators as needed.
            /// </summary>
            /// <remarks>This method does not validate the existence of the resulting path or its
            /// components. Directory separators are inserted only when necessary to ensure correct path
            /// formatting.</remarks>
            /// <param name="s0">The first path segment. This segment will appear at the start of the combined path.</param>
            /// <param name="s1">The second path segment to append after the first. A directory separator is inserted if required.</param>
            /// <param name="s2">The third path segment to append after the second. If not empty, a directory separator is inserted if
            /// required.</param>
            /// <param name="s3">The fourth path segment to append after the third. If not empty, a directory separator is inserted if
            /// required.</param>
            /// <returns>A string representing the combined path of all provided segments, separated by directory separators as
            /// appropriate.</returns>
            [Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Visual Studio 18.4.0 thinks this is unused...")]
            [Diagnostics.CodeAnalysis.SuppressMessage("Style", "S1144:Remove the unused private method", Justification = "Visual Studio 18.4.0 thinks this is unused...")]
            private static string JoinInternal(ReadOnlySpan<char> s0, ReadOnlySpan<char> s1, ReadOnlySpan<char> s2, ReadOnlySpan<char> s3)
            {
                // Compute total length including separators.
                int length = s0.Length + s1.Length;
                if (!EndsInDirectorySeparator(s0) && !StartsWithDirectorySeparator(s1))
                {
                    length++;
                }
                if (s2.Length > 0)
                {
                    length += s2.Length;
                    if (!EndsInDirectorySeparator(s1) && !StartsWithDirectorySeparator(s2))
                    {
                        length++;
                    }
                }
                if (s3.Length > 0)
                {
                    length += s3.Length;
                    if (!EndsInDirectorySeparator(s2) && !StartsWithDirectorySeparator(s3))
                    {
                        length++;
                    }
                }

                // Write s0.
                Span<char> buffer = length <= 260 ? stackalloc char[length] : new char[length];
                int pos = 0;
                s0.CopyTo(buffer);
                pos += s0.Length;

                // Write s1.
                if (!EndsInDirectorySeparator(s0) && !StartsWithDirectorySeparator(s1))
                {
                    buffer[pos++] = Path.DirectorySeparatorChar;
                }
                s1.CopyTo(buffer[pos..]);
                pos += s1.Length;

                // Write s2.
                if (s2.Length > 0)
                {
                    if (!EndsInDirectorySeparator(s1) && !StartsWithDirectorySeparator(s2))
                    {
                        buffer[pos++] = Path.DirectorySeparatorChar;
                    }
                    s2.CopyTo(buffer[pos..]);
                    pos += s2.Length;
                }

                // Write s3.
                if (s3.Length > 0)
                {
                    if (!EndsInDirectorySeparator(s2) && !StartsWithDirectorySeparator(s3))
                    {
                        buffer[pos++] = Path.DirectorySeparatorChar;
                    }
                    s3.CopyTo(buffer[pos..]);
                    pos += s3.Length;
                }
                return buffer[..pos].ToString();
            }

            /// <summary>
            /// Determines whether the specified path begins with a directory separator character.
            /// </summary>
            /// <remarks>This method considers both the primary and alternate directory separator
            /// characters as defined by the system.</remarks>
            /// <param name="path">A read-only span of characters representing the path to examine.</param>
            /// <returns>true if the first character of the path is a directory separator; otherwise, false.</returns>
            [Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Visual Studio 18.4.0 thinks this is unused...")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path)
            {
                return path.Length > 0 && (path[0] == Path.DirectorySeparatorChar || path[0] == Path.AltDirectorySeparatorChar);
            }

            /// <summary>
            /// Determines whether the specified path ends with a directory separator character.
            /// </summary>
            /// <remarks>Both the default and alternate directory separator characters are considered.
            /// This method does not validate the path or check for its existence.</remarks>
            /// <param name="path">The path to examine, represented as a read-only span of characters.</param>
            /// <returns>true if the last character of the path is a directory separator; otherwise, false.</returns>
            [Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0051:Remove unused private members", Justification = "Visual Studio 18.4.0 thinks this is unused...")]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
            {
                return path.Length > 0 && (path[^1] == Path.DirectorySeparatorChar || path[^1] == Path.AltDirectorySeparatorChar);
            }
        }
    }
}
#endif
