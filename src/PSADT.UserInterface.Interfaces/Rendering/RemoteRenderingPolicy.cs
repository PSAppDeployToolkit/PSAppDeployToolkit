using System.Windows.Interop;

namespace PSADT.UserInterface.Interfaces.Rendering
{
    /// <summary>
    /// Selects the WPF rendering preference from an operator override and current-session evidence.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0182:Avoid unused internal types", Justification = "This type is consumed by DialogManager in Task 3.")]
    internal static class RemoteRenderingPolicy
    {
        /// <summary>
        /// Chooses software rendering only for an override or positive remote-session evidence.
        /// </summary>
        /// <param name="forceSoftwareRendering">The explicit operator override.</param>
        /// <param name="windowsRemote">The nullable Windows current-session result.</param>
        /// <param name="citrixRemote">The nullable Citrix current-session result.</param>
        /// <returns>The process rendering preference.</returns>
        internal static RenderMode Choose(bool forceSoftwareRendering, bool? windowsRemote, bool? citrixRemote)
        {
            return forceSoftwareRendering || windowsRemote is true || citrixRemote is true
                ? RenderMode.SoftwareOnly
                : RenderMode.Default;
        }
    }
}
