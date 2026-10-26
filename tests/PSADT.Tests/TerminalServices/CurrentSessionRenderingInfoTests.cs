using System;
using System.Runtime.InteropServices;
using System.Text;
using PSADT.TerminalServices;
using Xunit;

namespace PSADT.Tests.TerminalServices
{
    /// <summary>
    /// Checks current-session probe orchestration and native result validation.
    /// </summary>
    public sealed class CurrentSessionRenderingInfoTests
    {
        /// <summary>
        /// Confirms the caller-selected session and positive Windows short circuit.
        /// </summary>
        /// <param name="windows">The fake Windows result.</param>
        /// <param name="expectedWindows">The expected Windows evidence.</param>
        /// <param name="expectedCitrix">The fake and expected Citrix evidence.</param>
        /// <param name="citrixCalls">The expected Citrix call count.</param>
        [Theory]
        [InlineData(true, true, null, 0)]
        [InlineData(false, false, true, 1)]
        [InlineData(false, false, false, 1)]
        [InlineData(null, null, null, 1)]
        public void Query_UsesTheSpecifiedClientSessionAndSkipsCitrixForPositiveWindows(bool? windows, bool? expectedWindows, bool? expectedCitrix, int citrixCalls)
        {
            const uint clientSessionId = 317;
            uint requestedSessionId = 0;
            int calls = 0;

            (bool? windowsRemote, bool? citrixRemote) = CurrentSessionRenderingInfo.Query(
                clientSessionId,
                sessionId => { requestedSessionId = sessionId; return windows; },
                () => { calls++; return expectedCitrix; });

            Assert.Equal(clientSessionId, requestedSessionId);
            Assert.Equal(expectedWindows, windowsRemote);
            Assert.Equal(expectedCitrix, citrixRemote);
            Assert.Equal(citrixCalls, calls);
        }

        /// <summary>
        /// Confirms recoverable provider failures yield unknown evidence.
        /// </summary>
        [Fact]
        public void Query_ConvertsProviderErrorsToUnknownWithoutSkippingTheOtherProbe()
        {
            (bool? windowsRemote, bool? citrixRemote) = CurrentSessionRenderingInfo.Query(
                42,
                static _ => throw new InvalidOperationException("WTS failed"),
                static () => throw new InvalidOperationException("WFAPI failed"));

            Assert.Null(windowsRemote);
            Assert.Null(citrixRemote);
        }

        /// <summary>
        /// Confirms only complete native values are read.
        /// </summary>
        /// <param name="value">The native flag value.</param>
        /// <param name="expected">The expected remote value.</param>
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, true)]
        [InlineData(317, true)]
        public void ReadRemoteFlag_UsesOnlyFourByteNativeValues(int value, bool expected)
        {
            nint buffer = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                Marshal.WriteInt32(buffer, value);
                Assert.Equal(expected, CurrentSessionRenderingInfo.ReadRemoteFlag(buffer, sizeof(int)));
                Assert.Null(CurrentSessionRenderingInfo.ReadRemoteFlag(buffer, sizeof(short)));
                Assert.Null(CurrentSessionRenderingInfo.ReadRemoteFlag(default, sizeof(int)));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Confirms an absent optional DLL does not interrupt startup evidence collection.
        /// </summary>
        [Fact]
        public void ProbeCitrix_MissingDllReturnsUnknownThroughQuery()
        {
            (bool? windowsRemote, bool? citrixRemote) = CurrentSessionRenderingInfo.Query(
                42,
                static _ => false,
                static () => CurrentSessionRenderingInfo.ProbeCitrix(@"C:\Windows\System32\PSADT_WFAPI_MISSING.dll"));

            Assert.False(windowsRemote);
            Assert.Null(citrixRemote);
        }

        /// <summary>
        /// Confirms a trusted system DLL without Citrix exports is treated as unknown.
        /// </summary>
        [Fact]
        public void ProbeCitrix_MissingExportReturnsUnknownThroughQuery()
        {
            string systemDll = System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernel32.dll");
            (bool? windowsRemote, bool? citrixRemote) = CurrentSessionRenderingInfo.Query(
                42,
                static _ => false,
                () => CurrentSessionRenderingInfo.ProbeCitrix(systemDll));

            Assert.False(windowsRemote);
            Assert.Null(citrixRemote);
        }

        /// <summary>
        /// Confirms the ICA-only client-name query validates its bounded Unicode result and frees native memory.
        /// </summary>
        /// <param name="succeeded">The simulated native BOOL return.</param>
        /// <param name="clientName">The simulated native Unicode result.</param>
        /// <param name="bytesReturned">The simulated native byte count.</param>
        /// <param name="expected">The expected nullable result.</param>
        [Theory]
        [InlineData(1, "ICA-client\0", 22U, true)]
        [InlineData(0, "ICA-client\0", 22U, null)]
        [InlineData(1, "\0", 2U, null)]
        [InlineData(1, "ICA-client", 20U, null)]
        [InlineData(1, "ICA-client\0", 21U, null)]
        [InlineData(1, "ICA-client\0", 0U, null)]
        [InlineData(1, "ICA-client\0", uint.MaxValue, null)]
        public void QueryCitrix_UsesCurrentSessionAndFreesReturnedBuffer(int succeeded, string clientName, uint bytesReturned, bool? expected)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(clientName);
            nint allocation = Marshal.AllocHGlobal(bytes.Length);
            bool freed = false;
            bool queriedCurrentSession = false;
            Marshal.Copy(bytes, 0, allocation, bytes.Length);

            int Query(nint server, uint sessionId, int infoClass, out nint buffer, out uint length)
            {
                queriedCurrentSession = server is 0 && sessionId is uint.MaxValue && infoClass is 10;
                buffer = allocation;
                length = bytesReturned;
                return succeeded;
            }

            void Free(nint buffer)
            {
                Assert.Equal(allocation, buffer);
                Marshal.FreeHGlobal(buffer);
                freed = true;
            }

            bool? actual = CurrentSessionRenderingInfo.QueryCitrix(Query, Free);
            Assert.Equal(expected, actual);
            Assert.True(queriedCurrentSession);
            Assert.True(freed);
        }

        /// <summary>
        /// Confirms a successful vendor call with no client-name buffer stays unknown and has nothing to free.
        /// </summary>
        [Fact]
        public void QueryCitrix_NullBufferIsUnknownAndDoesNotCallFree()
        {
            int freeCalls = 0;
            static int Query(nint server, uint sessionId, int infoClass, out nint buffer, out uint length)
            {
                Assert.Equal(default, server);
                Assert.Equal(uint.MaxValue, sessionId);
                Assert.Equal(10, infoClass);
                buffer = default;
                length = 8;
                return 1;
            }

            bool? result = CurrentSessionRenderingInfo.QueryCitrix(Query, _ => freeCalls++);
            Assert.Null(result);
            Assert.Equal(0, freeCalls);
        }
    }
}
