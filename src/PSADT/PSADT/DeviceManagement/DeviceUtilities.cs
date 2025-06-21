using System;
using System.Diagnostics;
using System.Threading;
using PSADT.LibraryInterfaces;
using Windows.Win32;
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
        /// <returns></returns>
        public static bool IsMicrophoneInUse()
        {
            // Initialize COM.
            Ole32.CoInitializeEx(Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA) ? COINIT.COINIT_APARTMENTTHREADED : COINIT.COINIT_MULTITHREADED);
            try
            {
                // Create an enumerator for audio devices.
                Ole32.CoCreateInstance(typeof(MMDeviceEnumerator).GUID, null!, CLSCTX.CLSCTX_INPROC_SERVER, out IMMDeviceEnumerator deviceEnumerator);

                // Get the default audio capture device (microphone).
                IMMDevice microphoneDevice;
                try
                {
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out microphoneDevice);
                }
                catch
                {
                    return false;
                }

                // Activate the session manager for the capture device.
                microphoneDevice.Activate(typeof(IAudioSessionManager2).GUID, CLSCTX.CLSCTX_ALL, null, out var sessionManagerObj);

                // Enumerate through audio sessions.
                var sessionEnumerator = ((IAudioSessionManager2)sessionManagerObj).GetSessionEnumerator();
                sessionEnumerator.GetCount(out int sessionCount);
                for (int i = 0; i < sessionCount; i++)
                {
                    // Check if the session state is active.
                    sessionEnumerator.GetSession(i, out var sessionControl);
                    ((IAudioSessionControl2)sessionControl).GetState(out var state);
                    if (state == AudioSessionState.AudioSessionStateActive)
                    {
                        return true;
                    }
                }
                return false;
            }
            finally
            {
                // Cleanup COM.
                PInvoke.CoUninitialize();
            }
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
    }
}
