using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using PSADT.UserInterface.Interfaces.Fluent;

namespace PSADT.UserInterface.Interfaces.Tests.TestHelpers
{
    /// <summary>
    /// Drives the controls a Fluent dialog built for itself.
    /// </summary>
    /// <remarks>
    /// Unlike the Classic dialogs, nothing here has to search for a control: the XAML names its elements
    /// and the compiler turns those names into fields the test assembly can see. What is needed instead
    /// are the few gestures a test cannot make directly on a window that was never shown.
    /// <para>
    /// Every member here must be called from within the dialog's own apartment. Everything a Fluent
    /// dialog exposes is a dependency property, and reading or writing one from another thread throws.
    /// </para>
    /// </remarks>
    internal static class FluentControls
    {
        /// <summary>
        /// Clicks a button, as a user would.
        /// </summary>
        /// <remarks>
        /// Raising the click event is what a real click ends up doing, and unlike a synthesised mouse
        /// press it does not need the window to be on screen - which these tests deliberately never
        /// arrange.
        /// </remarks>
        /// <param name="button">The button to click.</param>
        public static void Click(Fluence.Wpf.Controls.Button button)
        {
            ArgumentNullException.ThrowIfNull(button);
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        }

        /// <summary>
        /// Reads the caption showing on a button's face.
        /// </summary>
        /// <remarks>
        /// The caption is held in an access text element rather than as a plain string, which is what
        /// makes an underscore in it mark a keyboard shortcut rather than being shown.
        /// </remarks>
        /// <param name="button">The button to read.</param>
        /// <returns>The caption, with the accelerator marker still in it.</returns>
        public static string Caption(Fluence.Wpf.Controls.Button button)
        {
            ArgumentNullException.ThrowIfNull(button);
            return ((AccessText)button.Content).Text;
        }

        /// <summary>
        /// Asks a dialog whether it would presently allow itself to be closed.
        /// </summary>
        /// <remarks>
        /// The window answers by cancelling the closing event, so the question is put by handing it one
        /// and reading back whether it was cancelled. Reflection because the handler is protected and a
        /// Fluent dialog cannot be derived from outside its own assembly.
        /// </remarks>
        /// <param name="dialog">The dialog to ask.</param>
        /// <returns><see langword="true"/> if it would; otherwise, <see langword="false"/>.</returns>
        public static bool WouldAllowClosing(FluentDialog dialog)
        {
            CancelEventArgs closing = new();
            NonPublic.Call(dialog, "OnClosing", closing);
            return !closing.Cancel;
        }

        /// <summary>
        /// Runs one countdown tick.
        /// </summary>
        /// <remarks>
        /// The timer that would raise this is started when the window loads, which these tests never
        /// reach, so the tick is asked for directly.
        /// </remarks>
        /// <param name="dialog">The dialog to tick.</param>
        public static void Tick(FluentDialog dialog)
        {
            NonPublic.Call(dialog, "CountdownTimer_Tick", arguments: [null]);
        }
    }
}
