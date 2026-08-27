using PSADT.Interop.Utilities;
using Windows.Win32.Foundation;
using Windows.Win32.System.Diagnostics.Debug;
using Xunit;

namespace PSADT.Interop.Tests.Utilities
{
    /// <summary>
    /// Tests the HRESULT, NTSTATUS and Win32 error code arithmetic exposed by ExceptionUtilities.
    /// </summary>
    public sealed class ExceptionUtilitiesTests
    {
        /// <summary>
        /// Verifies that a failing Win32 error is folded into an HRESULT under the Win32 facility.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_WIN32_WrapsFailureUnderWin32Facility()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_FILE_NOT_FOUND);

            // Assert
            Assert.Equal(unchecked((int)0x80070002), result.Value);
        }

        /// <summary>
        /// Verifies that success is passed through unchanged rather than being marked as a failure.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_WIN32_LeavesSuccessUnchanged()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_WIN32(WIN32_ERROR.ERROR_SUCCESS);

            // Assert
            Assert.Equal(0, result.Value);
        }

        /// <summary>
        /// Verifies that the facility is extracted from bits 16 through 28 of an HRESULT.
        /// </summary>
        [Fact]
        public void HRESULT_FACILITY_ExtractsWin32Facility()
        {
            // Arrange
            HRESULT hResult = new(unchecked((int)0x80070002));

            // Act
            FACILITY_CODE facility = ExceptionUtilities.HRESULT_FACILITY(hResult);

            // Assert
            Assert.Equal(FACILITY_CODE.FACILITY_WIN32, facility);
        }

        /// <summary>
        /// Verifies that the code is extracted from the low word of an HRESULT.
        /// </summary>
        [Fact]
        public void HRESULT_CODE_ExtractsLowWord()
        {
            // Arrange
            HRESULT hResult = new(unchecked((int)0x80070002));

            // Act
            uint code = ExceptionUtilities.HRESULT_CODE(hResult);

            // Assert
            Assert.Equal((uint)WIN32_ERROR.ERROR_FILE_NOT_FOUND, code);
        }

        /// <summary>
        /// Verifies that an NTSTATUS is folded into an HRESULT by setting the NT facility bit and
        /// leaving the remaining bits alone.
        /// </summary>
        [Fact]
        public void HRESULT_FROM_NT_SetsNtFacilityBit()
        {
            // Act
            HRESULT result = ExceptionUtilities.HRESULT_FROM_NT(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(unchecked((int)0xD0000022), result.Value);
        }

        /// <summary>
        /// Verifies that an NTSTATUS with a documented Win32 equivalent maps onto it. This queries
        /// ntdll through RtlNtStatusToDosError and does not modify any system state.
        /// </summary>
        [Fact]
        public void WIN32_FROM_NT_MapsKnownStatusToWin32Error()
        {
            // Act
            WIN32_ERROR? result = ExceptionUtilities.WIN32_FROM_NT(NTSTATUS.STATUS_ACCESS_DENIED);

            // Assert
            Assert.Equal(WIN32_ERROR.ERROR_ACCESS_DENIED, result);
        }
    }
}
