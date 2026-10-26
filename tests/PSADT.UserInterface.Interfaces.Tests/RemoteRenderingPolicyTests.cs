using System.Windows.Interop;
using PSADT.UserInterface.Interfaces.Rendering;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests
{
    /// <summary>
    /// Verifies that only positive evidence or the override requests software rendering.
    /// </summary>
    public sealed class RemoteRenderingPolicyTests
    {
        /// <summary>
        /// Checks each policy combination, including uncertain results.
        /// </summary>
        /// <param name="forceSoftwareRendering">The operator override.</param>
        /// <param name="windowsRemote">The Windows probe result.</param>
        /// <param name="citrixRemote">The Citrix probe result.</param>
        /// <param name="expected">The expected WPF preference.</param>
        [Theory]
        [InlineData(false, null, null, RenderMode.Default)]
        [InlineData(false, false, false, RenderMode.Default)]
        [InlineData(false, false, null, RenderMode.Default)]
        [InlineData(false, null, false, RenderMode.Default)]
        [InlineData(false, true, null, RenderMode.SoftwareOnly)]
        [InlineData(false, null, true, RenderMode.SoftwareOnly)]
        [InlineData(false, true, false, RenderMode.SoftwareOnly)]
        [InlineData(false, false, true, RenderMode.SoftwareOnly)]
        [InlineData(true, null, null, RenderMode.SoftwareOnly)]
        [InlineData(true, false, false, RenderMode.SoftwareOnly)]
        public void Choose_UsesPositiveEvidenceOrOverride(bool forceSoftwareRendering, bool? windowsRemote, bool? citrixRemote, RenderMode expected)
        {
            Assert.Equal(expected, RemoteRenderingPolicy.Choose(forceSoftwareRendering, windowsRemote, citrixRemote));
        }
    }
}
