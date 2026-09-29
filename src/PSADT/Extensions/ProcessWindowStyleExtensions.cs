using System;
using System.Diagnostics;
using PSADT.Interop;

/// <summary>
/// Provides extension methods for the ProcessWindowStyle enumeration to facilitate conversion to SHOW_WINDOW_CMD values.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1110:Declare type inside namespace", Justification = "Polyfills aren't meant to be part of a namespace.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0047:Declare types in namespaces", Justification = "Polyfills aren't meant to be part of a namespace.")]
internal static class ProcessWindowStyleExtensions
{
    /// <summary>
    /// Converts a ProcessWindowStyle value to its corresponding SHOW_WINDOW_CMD value.
    /// </summary>
    /// <param name="windowStyle">The ProcessWindowStyle value to convert.</param>
    /// <returns>The corresponding SHOW_WINDOW_CMD value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the windowStyle value is not a valid ProcessWindowStyle.</exception>
    internal static SHOW_WINDOW_CMD ToShowWindowCmd(this ProcessWindowStyle windowStyle)
    {
        return windowStyle switch
        {
            ProcessWindowStyle.Normal => SHOW_WINDOW_CMD.SW_SHOWNORMAL,
            ProcessWindowStyle.Hidden => SHOW_WINDOW_CMD.SW_HIDE,
            ProcessWindowStyle.Minimized => SHOW_WINDOW_CMD.SW_SHOWMINIMIZED,
            ProcessWindowStyle.Maximized => SHOW_WINDOW_CMD.SW_SHOWMAXIMIZED,
            _ => throw new ArgumentOutOfRangeException(nameof(windowStyle), windowStyle, "Invalid ProcessWindowStyle value."),
        };
    }
}
