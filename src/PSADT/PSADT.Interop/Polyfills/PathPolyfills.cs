#if !NETCOREAPP2_1_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Win32;

/// <summary>
/// Polyfills for modern <see cref="Path"/> APIs on .NET Framework 4.7.2,
/// including <c>IsPathFullyQualified</c> and <c>Join</c>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182: Avoid unused internal types.", Justification = "This is used across InternalsVisibleTo boundaries.")]
internal static class PathPolyfills
{
    /// <summary>
    /// Provides extension methods for path qualification checks.
    /// </summary>
    extension(Path)
    {
        /// <summary>
        /// Concatenates an array of path components into a single path.
        /// </summary>
        /// <param name="paths">An array of path components to concatenate. Null or empty components are ignored.</param>
        /// <returns>A string representing the combined path of all provided segments, separated by directory separators as appropriate.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="paths"/> array is null.</exception>
        public static string Join(params IReadOnlyList<string?> paths)
        {
            // Validate input.
            ArgumentNullException.ThrowIfNull(paths);
            if (paths.Count == 0)
            {
                return string.Empty;
            }

            // Internal worker method to determining if a path starts/ends with a directory separator.
            static bool StartsWithDirectorySeparator(ReadOnlySpan<char> path)
            {
                return path.Length > 0 && (path[0] == Path.DirectorySeparatorChar || path[0] == Path.AltDirectorySeparatorChar);
            }
            static bool EndsInDirectorySeparator(ReadOnlySpan<char> path)
            {
                return path.Length > 0 && (path[^1] == Path.DirectorySeparatorChar || path[^1] == Path.AltDirectorySeparatorChar);
            }

            // Calculate buffer size.
            int maxSize = 0;
            foreach (string? path in paths)
            {
                maxSize += path?.Length ?? 0;
            }
            maxSize += paths.Count - 1;

            // Write paths to buffer.
            Span<char> buffer = maxSize <= (int)PInvoke.MAX_PATH * 4 ? stackalloc char[maxSize] : new char[maxSize];
            int written = 0;
            for (int i = 0; i < paths.Count; i++)
            {
                ReadOnlySpan<char> segment = paths[i].AsSpan();
                if (segment.Length == 0)
                {
                    continue;
                }
                if (written > 0 && !StartsWithDirectorySeparator(segment) && !EndsInDirectorySeparator(buffer[..written]))
                {
                    buffer[written++] = Path.DirectorySeparatorChar;
                }
                segment.CopyTo(buffer[written..]);
                written += segment.Length;
            }
            return buffer[..written].ToString();
        }
    }
}
#endif
