using System;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.LibraryInterfaces.SafeHandles;
using PSADT.ProcessManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;

namespace PSADT.DeviceManagement
{
    /// <summary>
    /// Utility class containing methods to do with audio tests.
    /// </summary>
    public static class DeviceUtilities
    {
        /// <summary>
        /// Tests whether the microphone is in use on the current device.
        /// </summary>
        /// <returns><see langword="true"/> if the microphone is currently in use; otherwise, <see langword="false"/>.</returns>
        public static bool IsMicrophoneInUse()
        {
            // Get the default audio capture device (microphone).
            IMMDeviceEnumerator deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
            try
            {
                // Try to get the default audio endpoint for capture devices. If none exists, return false.
                IMMDevice microphoneDevice;
                try
                {
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out microphoneDevice);
                }
                catch (Exception ex) when (ex.Message is not null)
                {
                    return false;
                }

                // Activate the session manager for the capture device and enumerate through each session.
                try
                {
                    microphoneDevice.Activate(CLSCTX.CLSCTX_INPROC_SERVER, null, out IAudioSessionManager2 sessionManager);
                    try
                    {
                        IAudioSessionEnumerator sessionEnumerator = sessionManager.GetSessionEnumerator();
                        try
                        {
                            sessionEnumerator.GetCount(out int sessionCount);
                            for (int i = 0; i < sessionCount; i++)
                            {
                                // Check if the session state is active.
                                sessionEnumerator.GetSession(i, out IAudioSessionControl sessionControl);
                                try
                                {
                                    sessionControl.GetState(out AudioSessionState state);
                                    if (state == AudioSessionState.AudioSessionStateActive)
                                    {
                                        return true;
                                    }
                                }
                                finally
                                {
                                    _ = Marshal.ReleaseComObject(sessionControl);
                                }
                            }
                            return false;
                        }
                        finally
                        {
                            _ = Marshal.ReleaseComObject(sessionEnumerator);
                        }
                    }
                    finally
                    {
                        _ = Marshal.ReleaseComObject(sessionManager);
                    }
                }
                finally
                {
                    _ = Marshal.ReleaseComObject(microphoneDevice);
                }
            }
            finally
            {
                _ = Marshal.ReleaseComObject(deviceEnumerator);
            }
        }

        /// <summary>
        /// Tests whether the current device has completed its Out-of-Box Experience (OOBE).
        /// </summary>
        /// <returns></returns>
        public static bool IsOOBEComplete()
        {
            _ = Kernel32.OOBEComplete(out BOOL isOobeComplete);
            return isOobeComplete;
        }

        /// <summary>
        /// Reboots the computer and terminates this process.
        /// </summary>
        internal static void RestartComputer()
        {
            Environment.Exit(ProcessManager.LaunchAsync(new(Path.Combine(Environment.SystemDirectory, "shutdown.exe"), ["/r /f /t 0"], Environment.SystemDirectory, denyUserTermination: true, createNoWindow: true))!.Task.GetAwaiter().GetResult().ExitCode);
        }

        /// <summary>
        /// Retrieves the system uptime.
        /// </summary>
        /// <remarks>The system uptime is calculated based on the number of milliseconds elapsed since the
        /// system was started.</remarks>
        /// <returns>A <see cref="TimeSpan"/> representing the duration for which the system has been running since the last
        /// restart.</returns>
        public static TimeSpan GetSystemUptime()
        {
            return TimeSpan.FromMilliseconds(PInvoke.GetTickCount64());
        }

        /// <summary>
        /// Retrieves the system boot time by calculating the difference between the current time and the system uptime.
        /// </summary>
        /// <returns>A <see cref="DateTime"/> representing the date and time when the system was last booted.</returns>
        public static DateTime GetSystemBootTime()
        {
            return DateTime.Now - GetSystemUptime();
        }

        /// <summary>
        /// Retrieves the current domain join status and associated domain or workgroup name of the local computer.
        /// </summary>
        /// <returns>A <see cref="DomainStatus"/> object containing the join status and the name of the domain or workgroup the
        /// computer is joined to.</returns>
        public static DomainStatus GetDomainStatus()
        {
            _ = NetApi32.NetGetJoinInformation(out SafeNetApiBufferFreeHandle? nameBuffer, out Windows.Win32.NetworkManagement.NetManagement.NETSETUP_JOIN_STATUS bufferType);
            using (nameBuffer)
            {
                return new((NETSETUP_JOIN_STATUS)bufferType, nameBuffer.ToStringUni());
            }
        }
    }
}
