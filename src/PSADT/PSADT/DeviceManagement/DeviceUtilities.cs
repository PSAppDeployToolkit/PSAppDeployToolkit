using System;
using System.IO;
using System.Runtime.InteropServices;
using PSADT.LibraryInterfaces;
using PSADT.LibraryInterfaces.Extensions;
using PSADT.ProcessManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;
using Windows.Win32.System.SystemInformation;

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
        /// Retrieves the chassis type of the system based on SMBIOS Type 3 data.
        /// </summary>
        /// <remarks>This method parses the system firmware table to extract the chassis type information
        /// as defined by the SMBIOS specification. The chassis type is represented as an enumeration of <see
        /// cref="SystemChassisType"/> values. <para> If the chassis type cannot be determined or is outside the valid
        /// range specified by the SMBIOS, the method returns <see cref="SystemChassisType.Other"/> or <see
        /// cref="SystemChassisType.Unknown"/>. </para></remarks>
        /// <returns>A <see cref="SystemChassisType"/> value representing the chassis type of the system. Returns <see
        /// cref="SystemChassisType.Unknown"/> if the chassis type information is unavailable.</returns>
        internal static SystemChassisType GetSystemChassisType()
        {
            // Set up required constants for SMBIOS Type 3 parsing.
            const byte SMBIOSTypeChassisInformation = 3;
            const byte ChassisTypeOffset = 5;

            // Allocate buffer for the SMBIOS data and retrieve it.
            Span<byte> buffer = stackalloc byte[(int)Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, null)];
            _ = Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, FIRMWARE_TABLE_ID.SMBIOS, buffer);

            // Parse the SMBIOS data to find Type 3, skipping the header.
            int offset = 0; offset += 8;
            while (offset < buffer.Length)
            {
                // Check if we've reached the end of usable data
                if (offset + 4 >= buffer.Length)
                {
                    break;
                }

                // If we found the chassis information structure
                if (buffer[offset] == SMBIOSTypeChassisInformation)
                {
                    // Make sure we have enough data to read the chassis type.
                    if (offset + ChassisTypeOffset < buffer.Length)
                    {
                        // The chassis type is at offset 5 within the Type 3 structure.
                        // Extract the lower 7 bits as per spec (bit 7 is reserved).
                        byte chassisType = buffer[offset + ChassisTypeOffset] &= 0x7F;
                        return chassisType is >= 1 and <= 32 ? (SystemChassisType)chassisType : SystemChassisType.Other;
                    }
                    break;
                }

                // Move to the next structure. First, skip the formatted part.
                offset += buffer[offset + 1];

                // Then skip the unformatted part (string fields). This ends with a double null terminator.
                while (offset < buffer.Length - 1 && !(buffer[offset] == 0 && buffer[offset + 1] == 0))
                {
                    offset++;
                }

                // Skip the double null terminator.
                offset += 2;
            }

            // No chassis information found.
            return SystemChassisType.Unknown;
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
    }
}
