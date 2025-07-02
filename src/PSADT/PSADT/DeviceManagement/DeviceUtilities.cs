﻿using System;
using System.Diagnostics;
using PSADT.Extensions;
using PSADT.LibraryInterfaces;
using Windows.Win32;
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
        /// <returns></returns>
        public static bool IsMicrophoneInUse()
        {
            // Get the default audio capture device (microphone).
            IMMDevice microphoneDevice;
            try
            {
                ((IMMDeviceEnumerator)new MMDeviceEnumerator()).GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out microphoneDevice);
            }
            catch
            {
                return false;
            }

            // Activate the session manager for the capture device and enumerate through each session.
            microphoneDevice.Activate<IAudioSessionManager2>(CLSCTX.CLSCTX_INPROC_SERVER, null, out var sessionManagerObj);
            var sessionEnumerator = sessionManagerObj.GetSessionEnumerator();
            sessionEnumerator.GetCount(out var sessionCount);
            for (int i = 0; i < sessionCount; i++)
            {
                // Check if the session state is active.
                sessionEnumerator.GetSession(i, out var sessionControl);
                sessionControl.GetState(out var state);
                if (state == AudioSessionState.AudioSessionStateActive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests whether the current device has completed its Out-of-Box Experience (OOBE).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Win32Exception"></exception>
        public static bool IsOOBEComplete()
        {
            Kernel32.OOBEComplete(out var isOobeComplete);
            return isOobeComplete;
        }

        /// <summary>
        /// Reboots the computer and terminates this process.
        /// </summary>
        internal static void RestartComputer()
        {
            // Reboot the system and hard-exit this process.
            using (Process process = new())
            {
                process.StartInfo.FileName = "shutdown.exe";
                process.StartInfo.Arguments = "/r /f /t 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start(); process.WaitForExit();
            }
            Environment.Exit(0);
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
            Span<byte> buffer = stackalloc byte[(int)Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, 0, null)];
            Kernel32.GetSystemFirmwareTable(FIRMWARE_TABLE_PROVIDER.RSMB, 0, buffer);

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
                        return chassisType >= 1 && chassisType <= 32 ? (SystemChassisType)chassisType : SystemChassisType.Other;
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
    }
}
