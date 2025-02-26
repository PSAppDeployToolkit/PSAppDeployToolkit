using System.Threading;
using Windows.Win32;
using Windows.Win32.Media.Audio;
using Windows.Win32.System.Com;

namespace PSADT.Devices
{
    public static class Audio
    {
        public static bool IsMicrophoneInUse()
        {
            // Initialize COM.
            PInvoke.CoInitializeEx(Thread.CurrentThread.GetApartmentState().Equals(ApartmentState.STA) ? COINIT.COINIT_APARTMENTTHREADED : COINIT.COINIT_MULTITHREADED).ThrowOnFailure();
            bool micInUse = false;
            try
            {
                // Create an enumerator for audio devices.
                PInvoke.CoCreateInstance(typeof(MMDeviceEnumerator).GUID, null, CLSCTX.CLSCTX_INPROC_SERVER, out IMMDeviceEnumerator deviceEnumerator).ThrowOnFailure();

                // Get the default audio capture device (microphone).
                IMMDevice microphoneDevice;
                try
                {
                    deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out microphoneDevice);
                }
                catch
                {
                    return micInUse;
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
                        micInUse = true;
                        break;
                    }
                }
            }
            finally
            {
                // Cleanup COM.
                PInvoke.CoUninitialize();
            }

            return micInUse;
        }
    }
}
