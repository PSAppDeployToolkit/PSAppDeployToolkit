using System;
using System.Diagnostics;

namespace PSADT.UserInterface.Dialogs
{
    internal static class DialogTools
    {
        /// <summary>
        /// Reboots the computer and terminates this process.
        /// </summary>
        internal static void RestartComputer()
        {
            // Reboot the system and hard-exit this process.
            using (var process = new Process())
            {
                process.StartInfo.FileName = "shutdown.exe";
                process.StartInfo.Arguments = "/r /f /t 0";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit();
            }
            Environment.Exit(0);
        }
    }
}
