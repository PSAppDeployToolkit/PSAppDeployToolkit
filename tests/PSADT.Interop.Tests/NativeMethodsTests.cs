using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using PSADT.Interop.SafeHandles;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authentication.Identity;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Power;
using Windows.Win32.System.Registry;
using Windows.Win32.System.Services;
using Windows.Win32.System.SystemInformation;
using Windows.Win32.System.Threading;
using Xunit;

namespace PSADT.Interop.Tests
{
    /// <summary>
    /// Tests the native wrappers against the state the operating system reports through other means. The
    /// wrappers themselves do four things worth checking: they marshal arguments, they size buffers with
    /// the two-call pattern, they turn a native failure into the matching managed exception, and they hand
    /// back safe handles. Each test here targets one of those.
    /// </summary>
    /// <remarks>
    /// Every call made here queries state and changes none. Where a wrapper can only be exercised with
    /// elevation the test is written but skipped, so an unelevated run reports what it could not cover
    /// rather than silently omitting it.
    /// </remarks>
    public sealed class NativeMethodsTests
    {
        /// <summary>
        /// Whether the caller is running elevated, which gates the tests that cannot succeed otherwise.
        /// </summary>
        public static bool IsElevated { get; } = GetIsElevated();

        /// <summary>
        /// Verifies that the current-process handle is the documented pseudo-handle, and that it reports
        /// itself invalid because negative one is the invalid sentinel for the handle type it is wrapped
        /// in. This is why the wrappers that accept it check for a closed handle rather than an invalid
        /// one, and tightening one of those guards would break every caller passing this handle.
        /// </summary>
        [Fact]
        public void GetCurrentProcess_IsThePseudoHandleAndReportsItselfInvalid()
        {
            // Act
            using SafeProcessHandle process = NativeMethods.GetCurrentProcess();

            // Assert
            Assert.Equal(-1L, process.DangerousGetHandle().ToInt64());
            Assert.True(process.IsInvalid);
            Assert.False(process.IsClosed);
            Assert.True(NativeMethods.GetProcessId(process) is not 0);
        }

        /// <summary>
        /// Verifies that the identity of the running process reads back through the wrappers as the same
        /// process the framework reports, which covers the argument marshalling in both directions.
        /// </summary>
        [Fact]
        public void ProcessIdentity_MatchesWhatTheFrameworkReports()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            using SafeProcessHandle process = NativeMethods.GetCurrentProcess();

            // Act
            uint processId = NativeMethods.GetProcessId(process);
            _ = NativeMethods.ProcessIdToSessionId(processId, out uint sessionId);

            // Assert
            Assert.Equal((uint)current.Id, processId);
            Assert.Equal((uint)current.SessionId, sessionId);
        }

        /// <summary>
        /// Verifies that the image path of the running process reads back as the executable the framework
        /// reports. The wrapper seeds the size from the span it is given and returns the length written, so
        /// this covers the whole span-and-length convention rather than just the call succeeding.
        /// </summary>
        [Fact]
        public void QueryFullProcessImageName_ReturnsTheRunningExecutable()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            Assert.NotNull(current.MainModule);
            using SafeProcessHandle process = NativeMethods.GetCurrentProcess();
            char[] buffer = new char[1024];

            // Act
            _ = NativeMethods.QueryFullProcessImageName(process, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, out uint length);

            // Assert
            Assert.True(length is > 0 and < 1024);
            Assert.Equal(current.MainModule.FileName, new string(buffer, 0, (int)length), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that the token of the running process names the running user. The buffer is sized by
        /// asking with nothing, which the wrapper deliberately does not treat as a failure, and the user
        /// identifier is read straight out of the returned block.
        /// </summary>
        [Fact]
        public void GetTokenInformation_NamesTheRunningUser()
        {
            // Arrange
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            using SafeProcessHandle process = NativeMethods.GetCurrentProcess();
            _ = NativeMethods.OpenProcessToken(process, TOKEN_ACCESS_MASK.TOKEN_QUERY, out SafeFileHandle token);

            using (token)
            {
                // Act: the sizing call reports the length without the buffer, then the buffer is filled
                _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenUser, default, out uint length);
                byte[] buffer = new byte[length];
                _ = NativeMethods.GetTokenInformation(token, TOKEN_INFORMATION_CLASS.TokenUser, buffer, out uint written);

                // Assert: a TOKEN_USER opens with a pointer to the identifier, which is all this needs
                SecurityIdentifier user;
                unsafe
                {
                    fixed (byte* block = buffer)
                    {
                        user = new((nint)(*(void**)block));
                    }
                }
                Assert.Equal(length, written);
                Assert.Equal(identity.User, user);
            }
        }

        /// <summary>
        /// Verifies that the version the kernel reports matches the build the system records for itself.
        /// The wrapper fills in the structure size on the caller's behalf, which the call fails without.
        /// </summary>
        [Fact]
        public void RtlGetVersion_MatchesTheRecordedBuildNumber()
        {
            // Arrange
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", writable: false);
            Assert.NotNull(key);
            string? recorded = key.GetValue("CurrentBuildNumber") as string;
            Assert.NotNull(recorded);

            // Act
            _ = NativeMethods.RtlGetVersion(out OSVERSIONINFOEXW version);

            // Assert
            Assert.Equal(uint.Parse(recorded, System.Globalization.CultureInfo.InvariantCulture), version.dwBuildNumber);
            Assert.True(version.dwMajorVersion >= 10);
        }

        /// <summary>
        /// Verifies that a read of our own memory returns what is there. This is the only wrapper taking a
        /// raw address alongside a span, so the two have to agree about how much was read.
        /// </summary>
        [Fact]
        public void ReadProcessMemory_ReadsBackOurOwnBuffer()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            byte[] source = [1, 2, 3, 5, 8, 13, 21, 34];
            byte[] destination = new byte[source.Length];
            using SafeFileHandle process = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_VM_READ, bInheritHandle: false, (uint)current.Id);

            // Act
            unsafe
            {
                fixed (byte* address = source)
                {
                    _ = NativeMethods.ReadProcessMemory(process, (nint)address, destination, out nuint read);
                    Assert.Equal(source.Length, (int)read);
                }
            }

            // Assert
            Assert.Equal(source, destination);
        }

        /// <summary>
        /// Verifies that waiting on a live process reports a timeout rather than a failure. Every value the
        /// wait can return other than the failure sentinel is a legitimate result, so treating a non-zero
        /// return as an error would break every caller polling a process.
        /// </summary>
        [Fact]
        public void WaitForSingleObject_ReportsATimeoutRatherThanAFailure()
        {
            // Arrange
            using Process current = Process.GetCurrentProcess();
            using SafeFileHandle process = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_SYNCHRONIZE, bInheritHandle: false, (uint)current.Id);

            // Act & Assert
            Assert.Equal(WAIT_EVENT.WAIT_TIMEOUT, NativeMethods.WaitForSingleObject(process, 0));
        }

        /// <summary>
        /// Verifies that the running process reports itself as still active, which is the sentinel a caller
        /// has to distinguish from a real exit code.
        /// </summary>
        [Fact]
        public void GetExitCodeProcess_ReportsTheRunningProcessAsActive()
        {
            // Arrange
            using SafeProcessHandle process = NativeMethods.GetCurrentProcess();

            // Act
            _ = NativeMethods.GetExitCodeProcess(process, out uint exitCode);

            // Assert
            Assert.Equal(259u, exitCode);
        }

        /// <summary>
        /// Verifies that opening a process that cannot exist is raised as a failure rather than handed back
        /// as an invalid handle. Zero is the idle process, which no caller may open.
        /// </summary>
        [Fact]
        public void OpenProcess_RaisesAFailureForAnUnopenableProcess()
        {
            // Act & Assert
            _ = Assert.ThrowsAny<Exception>(static () => { using SafeFileHandle idle = NativeMethods.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, bInheritHandle: false, 0); });
        }

        /// <summary>
        /// Verifies that a known export resolves and a missing one is raised as the matching Windows error.
        /// A missing export comes back as a null address rather than a failed call, so the wrapper has to
        /// notice it.
        /// </summary>
        [Fact]
        public void GetProcAddress_ResolvesAKnownExportAndRaisesAMissingOne()
        {
            // Arrange
            using FreeLibrarySafeHandle library = NativeMethods.LoadLibraryEx("kernel32.dll");

            // Act
            FARPROC resolved = NativeMethods.GetProcAddress(library, "GetSystemPowerStatus");
            Win32Exception exception = Assert.Throws<Win32Exception>(() => NativeMethods.GetProcAddress(library, "AnExportThatDoesNotExist"));

            // Assert
            Assert.False(resolved.IsNull);
            Assert.Equal((int)WIN32_ERROR.ERROR_PROC_NOT_FOUND, exception.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a module which cannot be found is raised rather than returned as an invalid
        /// handle, and that it arrives as the closest managed exception with the Windows error kept
        /// underneath. A caller catching for a missing file gets what it expects without losing the code.
        /// </summary>
        [Fact]
        public void LoadLibraryEx_RaisesAFailureForAMissingModule()
        {
            // Act
            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(static () => { using FreeLibrarySafeHandle missing = NativeMethods.LoadLibraryEx("a-module-that-does-not-exist.dll"); });

            // Assert
            Win32Exception inner = Assert.IsType<Win32Exception>(exception.InnerException);
            Assert.Equal((int)WIN32_ERROR.ERROR_MOD_NOT_FOUND, inner.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that a handle to a file on disk is reported as such. The wrapper has to separate a
        /// genuine unknown type from a failed call, since both come back as the same value.
        /// </summary>
        [Fact]
        public void GetFileType_ReportsAFileOnDiskAndCoversCreateFile()
        {
            // Arrange
            string path = typeof(NativeMethodsTests).Assembly.Location;

            // Act
            using SafeFileHandle file = NativeMethods.CreateFile(path, FileSystemRights.Read, FILE_SHARE_MODE.FILE_SHARE_READ, lpSecurityAttributes: null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FileAttributes.Normal);

            // Assert
            Assert.Equal(FILE_TYPE.FILE_TYPE_DISK, NativeMethods.GetFileType(file));
        }

        /// <summary>
        /// Verifies that a missing file is refused before the call is made. The wrapper only opens existing
        /// files, so the guard is what turns a creation disposition mistake into a clear failure.
        /// </summary>
        [Fact]
        public void CreateFile_RefusesAMissingFileBeforeCalling()
        {
            // Arrange
            string missing = Path.Join(AppContext.BaseDirectory, "a-file-that-does-not-exist.bin");

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => { using SafeFileHandle file = NativeMethods.CreateFile(missing, FileSystemRights.Read, FILE_SHARE_MODE.FILE_SHARE_READ, lpSecurityAttributes: null, FILE_CREATION_DISPOSITION.OPEN_EXISTING, FileAttributes.Normal); });
        }

        /// <summary>
        /// Verifies that the drive the system lives on resolves to a device path, which is the whole point
        /// of the call and the only part of the result that is not machine specific.
        /// </summary>
        [Fact]
        public void QueryDosDevice_ResolvesTheSystemDriveToADevicePath()
        {
            // Arrange
            string drive = Environment.SystemDirectory[..2];
            char[] buffer = new char[1024];

            // Act
            uint length = NativeMethods.QueryDosDevice(drive, buffer);

            // Assert
            Assert.True(length is > 0 and < 1024);
            Assert.StartsWith(@"\Device\", new string(buffer, 0, (int)length), StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that a registry key opens for reading and reports a shape consistent with a key that
        /// exists, which covers both wrappers and the handle handed between them.
        /// </summary>
        [Fact]
        public void RegOpenKeyEx_OpensAKeyForReadingAndReportsItsShape()
        {
            // Arrange
            // For a predefined key the framework hands out a non-owning wrapper, so closing it closes nothing.
            using SafeRegistryHandle root = Registry.LocalMachine.Handle;

            // Act
            _ = NativeMethods.RegOpenKeyEx(root, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", REG_SAM_FLAGS.KEY_READ, out SafeRegistryHandle key);

            using (key)
            {
                _ = NativeMethods.RegQueryInfoKey(key, default, out _, out uint subKeys, out _, out _, out uint values, out _, out _, out _, out _);

                // Assert
                Assert.False(key.IsInvalid);
                Assert.True(subKeys > 0);
                Assert.True(values > 0);
            }
        }

        /// <summary>
        /// Verifies that opening a key that does not exist is raised as a missing-file failure, which is
        /// how Windows reports it and what a caller has to catch.
        /// </summary>
        [Fact]
        public void RegOpenKeyEx_RaisesAFailureForAMissingKey()
        {
            // Arrange
            // For a predefined key the framework hands out a non-owning wrapper, so closing it closes nothing.
            using SafeRegistryHandle root = Registry.LocalMachine.Handle;

            // Act & Assert
            _ = Assert.Throws<FileNotFoundException>(() => NativeMethods.RegOpenKeyEx(root, @"SOFTWARE\A-Key-That-Does-Not-Exist", REG_SAM_FLAGS.KEY_READ, out _));
        }

        /// <summary>
        /// Verifies that a service status reads back with a documented state. The buffer is sized by asking
        /// with nothing, which the wrapper tolerates only for the insufficient-buffer error, so this covers
        /// that exemption as well as the query itself.
        /// </summary>
        [Fact]
        public void QueryServiceStatusEx_ReportsADocumentedState()
        {
            // Arrange
            using CloseServiceHandleSafeHandle manager = NativeMethods.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_CONNECT);
            using CloseServiceHandleSafeHandle service = NativeMethods.OpenService(manager, "EventLog", SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS);

            // Act: the sizing call reports the length without the buffer, then the buffer is filled
            _ = NativeMethods.QueryServiceStatusEx(service, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, default, out uint needed);
            byte[] buffer = new byte[needed];
            _ = NativeMethods.QueryServiceStatusEx(service, SC_STATUS_TYPE.SC_STATUS_PROCESS_INFO, buffer, out _);

            // Assert: a SERVICE_STATUS_PROCESS opens with the type then the current state
            Assert.Equal(36u, needed);
            Assert.True(BitConverter.ToUInt32(buffer, 4) is >= 1 and <= 7);
        }

        /// <summary>
        /// Verifies that opening a service that does not exist is raised as the matching Windows error,
        /// since the call reports it through an invalid handle rather than a failed return.
        /// </summary>
        [Fact]
        public void OpenService_RaisesAFailureForAMissingService()
        {
            // Arrange
            using CloseServiceHandleSafeHandle manager = NativeMethods.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_CONNECT);

            // Act
            Win32Exception exception = Assert.Throws<Win32Exception>(() => { using CloseServiceHandleSafeHandle missing = NativeMethods.OpenService(manager, "AServiceThatDoesNotExist", SERVICE_ACCESS_RIGHTS.SERVICE_QUERY_STATUS); });

            // Assert
            Assert.Equal((int)WIN32_ERROR.ERROR_SERVICE_DOES_NOT_EXIST, exception.NativeErrorCode);
        }

        /// <summary>
        /// Verifies that the firmware table identifier for SMBIOS is the one the RSMB provider accepts. The
        /// FourCC test on the enumeration has to exempt this member because it is a provider-relative zero
        /// rather than a packed signature, and that exemption rests on documentation alone. This checks it
        /// against the provider: asking for table zero returns a table, and the two-call sizing convention
        /// agrees with itself about how large it is.
        /// </summary>
        [Fact]
        public void GetSystemFirmwareTable_AcceptsTheProviderRelativeSmbiosIdentifier()
        {
            // Act: the sizing call reports the length without the buffer, then the buffer is filled
            uint needed = NativeMethods.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, default);
            byte[] buffer = new byte[needed];
            uint written = NativeMethods.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, buffer);

            // Assert: a RawSMBIOSData opens with a four-byte version stamp then the length of what follows
            Assert.True(needed > 8);
            Assert.Equal(needed, written);
            Assert.Equal(needed - 8, BitConverter.ToUInt32(buffer, 4));
        }

        /// <summary>
        /// Verifies that the power status reads back within its documented ranges. The structure is a
        /// packed set of single bytes where every field has a reserved value for "unknown", so a
        /// marshalling mistake shows up as a field outside its range rather than as a failed call.
        /// </summary>
        [Fact]
        public void GetSystemPowerStatus_ReportsValuesWithinTheirDocumentedRanges()
        {
            // Act
            _ = NativeMethods.GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

            // Assert
            Assert.True(status.ACLineStatus is 0 or 1 or 255);
            Assert.True(status.BatteryLifePercent is <= 100 or 255);
            Assert.True(status.BatteryFlag is 0 or 1 or 2 or 4 or 8 or 128 or 255);
        }

        /// <summary>
        /// Verifies that the product of the running version resolves, and that a version no Windows ever
        /// carried is raised as a failure. The call does not record a reason for its failure, so the
        /// wrapper has to supply one.
        /// </summary>
        [Fact]
        public void GetProductInfo_ResolvesTheRunningVersionAndRejectsAnImpossibleOne()
        {
            // Arrange
            _ = NativeMethods.RtlGetVersion(out OSVERSIONINFOEXW version);

            // Act
            _ = NativeMethods.GetProductInfo(version.dwMajorVersion, version.dwMinorVersion, version.wServicePackMajor, version.wServicePackMinor, out OS_PRODUCT_TYPE product);

            // Assert
            Assert.NotEqual(OS_PRODUCT_TYPE.PRODUCT_UNDEFINED, product);
            _ = Assert.Throws<Win32Exception>(static () => NativeMethods.GetProductInfo(0, 0, 0, 0, out _));
        }

        /// <summary>
        /// Verifies that a product code belonging to nothing is reported as unknown rather than raised.
        /// This wrapper passes the result straight through because "not installed" is an answer, not a
        /// failure, and a caller asking whether something is installed needs to be told no.
        /// </summary>
        [Fact]
        public void MsiQueryProductState_ReportsAnUnknownProductWithoutRaising()
        {
            // Arrange
            Guid nothing = Guid.Empty;

            // Act & Assert
            Assert.Equal(Windows.Win32.System.ApplicationInstallationAndServicing.INSTALLSTATE.INSTALLSTATE_UNKNOWN, NativeMethods.MsiQueryProductState(nothing));
        }

        /// <summary>
        /// Verifies that a policy class the wrapper has no buffer size for is refused before the call is
        /// made. The size cannot be recovered from the call itself, so an unlisted class would otherwise
        /// produce a handle describing the wrong length of memory.
        /// </summary>
        /// <param name="informationClass">The class expected to be refused, as its underlying value since the
        /// enumeration is not visible outside the assembly it belongs to.</param>
        [Theory]
        [InlineData((int)POLICY_INFORMATION_CLASS.PolicyLocalAccountDomainInformation)]
        [InlineData((int)POLICY_INFORMATION_CLASS.PolicyMachineAccountInformation)]
        public void LsaQueryInformationPolicy_RefusesAClassWithNoKnownSize(int informationClass)
        {
            // Arrange
            using SafeNoReleaseHandle policy = new(1);

            // Act & Assert
            _ = Assert.Throws<ArgumentOutOfRangeException>(() => NativeMethods.LsaQueryInformationPolicy(policy, (POLICY_INFORMATION_CLASS)informationClass, out _));
        }

        /// <summary>
        /// Verifies that full control of the service database is refused without elevation, which is the
        /// access denial the wrapper has to translate rather than return.
        /// </summary>
        [Fact(Skip = "Requires an unelevated caller.", SkipWhen = nameof(IsElevated))]
        public void OpenSCManager_IsRefusedWithoutElevation()
        {
            // Act & Assert
            _ = Assert.Throws<UnauthorizedAccessException>(static () => { using CloseServiceHandleSafeHandle refused = NativeMethods.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_ALL_ACCESS); });
        }

        /// <summary>
        /// Verifies that full control of the service database is granted with elevation, so the refusal
        /// above is attributable to the caller's rights rather than to the wrapper.
        /// </summary>
        [Fact(Skip = "Requires an elevated caller.", SkipUnless = nameof(IsElevated))]
        public void OpenSCManager_IsGrantedWithElevation()
        {
            // Act
            using CloseServiceHandleSafeHandle manager = NativeMethods.OpenSCManager(SC_MANAGER_ACCESS.SC_MANAGER_ALL_ACCESS);

            // Assert
            Assert.False(manager.IsInvalid);
        }

        /// <summary>
        /// Verifies that the local security authority opens for reading and answers a query about the
        /// machine's own role. This is the one path that produces a real memory handle from the authority,
        /// so it covers the length the wrapper attaches to that handle as well as the query. Reading the
        /// local policy needs no elevation, so this runs for every caller.
        /// </summary>
        [Fact]
        public void LsaQueryInformationPolicy_ReportsTheMachineRole()
        {
            // Act
            _ = NativeMethods.LsaOpenPolicy(default, LSA_POLICY_ACCESS.POLICY_VIEW_LOCAL_INFORMATION, out LsaCloseSafeHandle policy);

            using (policy)
            {
                Assert.False(policy.IsInvalid);
                _ = NativeMethods.LsaQueryInformationPolicy(policy, POLICY_INFORMATION_CLASS.PolicyLsaServerRoleInformation, out SafeLsaFreeMemoryHandle buffer);

                using (buffer)
                {
                    // Assert: the block holds a single role value, which is a backup or a primary
                    Assert.False(buffer.IsInvalid);
                    Assert.Equal(sizeof(uint), buffer.Length);
                    Assert.True(buffer.AsReadOnlySpan<uint>()[0] is 2 or 3);
                }
            }
        }

        /// <summary>
        /// Determines whether the caller is running with administrative rights.
        /// </summary>
        /// <returns><see langword="true"/> if the caller is elevated; otherwise, <see langword="false"/>.</returns>
        private static bool GetIsElevated()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
