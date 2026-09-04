using System.Runtime.CompilerServices;
using Windows.Win32.UI.Shell;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the stock icon record, which owns an icon handle when one was asked for and is responsible
    /// for destroying it.
    /// </summary>
    /// <remarks>
    /// The record is only ever produced by SHGetStockIconInfo, so it is obtained that way here rather than
    /// fabricated. That call reads from the shell and changes nothing; where an icon handle is requested,
    /// the handle belongs to this process and is destroyed before the test returns.
    /// <para>
    /// Dispose is called on the variable rather than through a method group, because this is a mutable
    /// struct: a method group conversion boxes it, and the copy in the box is what would be cleared. The
    /// production caller uses a using statement over the local, which has the same direct effect.
    /// </para>
    /// </remarks>
    public sealed class SHSTOCKICONINFOTests
    {
        /// <summary>
        /// Verifies that asking only for the image index yields one without creating an icon, and that
        /// disposing a record holding no icon does nothing rather than failing.
        /// </summary>
        [Fact]
        public void Dispose_DoesNothingWhenNoIconWasRequested()
        {
            // Act
            _ = NativeMethods.SHGetStockIconInfo(SHSTOCKICONID.SIID_APPLICATION, SHGSI_FLAGS.SHGSI_SYSICONINDEX, out SHSTOCKICONINFO info);

            // Assert
            Assert.True(info.iSysImageIndex >= 0);
            Assert.Equal(0, (nint)info.hIcon);
            info.Dispose();
        }

        /// <summary>
        /// Verifies that asking for an icon yields a handle, that disposing destroys it, and that a second
        /// dispose is tolerated. The second call is what proves the handle is cleared rather than
        /// destroyed twice, which would fail.
        /// </summary>
        [Fact]
        public void Dispose_DestroysTheIconOnceAndToleratesASecondCall()
        {
            // Arrange
            _ = NativeMethods.SHGetStockIconInfo(SHSTOCKICONID.SIID_APPLICATION, SHGSI_FLAGS.SHGSI_ICON, out SHSTOCKICONINFO info);

            // Assert
            Assert.NotEqual(0, (nint)info.hIcon);
            info.Dispose();
            Assert.Equal(0, (nint)info.hIcon);
            info.Dispose();
        }

        /// <summary>
        /// Verifies that the wrapper stamps the structure size on the caller's behalf, which the shell
        /// rejects the call without. Nothing in the signature makes a caller supply it, so the value has to
        /// come from the wrapper.
        /// </summary>
        [Fact]
        public void Size_IsStampedByTheWrapper()
        {
            // Act
            _ = NativeMethods.SHGetStockIconInfo(SHSTOCKICONID.SIID_APPLICATION, SHGSI_FLAGS.SHGSI_SYSICONINDEX, out SHSTOCKICONINFO info);

            // Assert
            Assert.Equal((uint)Unsafe.SizeOf<SHSTOCKICONINFO>(), info.cbSize);
            info.Dispose();
        }
    }
}
