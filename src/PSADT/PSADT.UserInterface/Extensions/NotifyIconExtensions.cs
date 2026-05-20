using PSADT.UserInterface.DialogOptions;
using System.Windows.Forms;

namespace PSADT.UserInterface.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="NotifyIcon"/> class.
    /// </summary>
    internal static class NotifyIconExtensions
    {
        /// <summary>
        /// Displays a balloon tip notification using the system default timeout.
        /// </summary>
        /// <remarks>Uses a timeout value of 0, which allows the system to determine the display duration
        /// based on accessibility settings.</remarks>
        /// <param name="notifyIcon">The notify icon instance.</param>
        /// <param name="tipOptions">The options specifying the balloon tip configuration.</param>
        internal static void ShowBalloonTip(this NotifyIcon notifyIcon, BalloonTipOptions tipOptions)
        {
            notifyIcon.ShowBalloonTip(0, tipOptions.Title, tipOptions.Text, tipOptions.Icon);
        }
    }
}
