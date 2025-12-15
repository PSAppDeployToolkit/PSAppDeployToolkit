using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using PSADT.LibraryInterfaces;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Base class for classic dialog forms.
    /// </summary>
    internal partial class ClassicDialog : BaseDialog, IDialogBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class.
        /// </summary>
        internal ClassicDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal ClassicDialog(BaseOptions options) : base()
        {
            // Initialise the underlying form as set up by the designer.
            InitializeComponent();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Base properties.
                SuspendLayout();
                Text = StripFormattingTags(options.AppTitle);
                Icon = GetIcon(options.AppIconImage);
                TopMost = options.DialogTopMost;
                ActiveControl = buttonDefault;
                FormClosing += Form_FormClosing;
                Load += Form_Load;
                ResumeLayout();

                // Set the expiry timer if specified.
                if (options.DialogExpiryDuration is not null && options.DialogExpiryDuration.Value != TimeSpan.Zero)
                {
                    expiryTimer = new Timer { Interval = (int)options.DialogExpiryDuration.Value.TotalMilliseconds };
                    expiryTimer.Tick += (s, e) => CloseDialog();
                }

                // PersistPrompt timer code.
                if (options.DialogPersistInterval is not null && options.DialogPersistInterval.Value != TimeSpan.Zero)
                {
                    persistTimer = new Timer { Interval = (int)options.DialogPersistInterval.Value.TotalMilliseconds };
                    persistTimer.Tick += PersistTimer_Tick;
                }

                // Set the optional dialog position.
                if (options.DialogPosition is not null)
                {
                    dialogPosition = options.DialogPosition.Value;
                }

                // Set whether the dialog can be moved.
                if (options.DialogAllowMove is not null)
                {
                    dialogAllowMove = options.DialogAllowMove.Value;
                }
            }
        }

        /// <summary>
        /// Redefined ShowDialog method to allow for custom behavior.
        /// </summary>
        public new void ShowDialog()
        {
            _ = base.ShowDialog();
        }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        public void CloseDialog()
        {
            canClose = true;
            Close();
        }

        /// <summary>
        /// Configures the specified <see cref="PictureBox"/> with an image and size based on the provided options.
        /// </summary>
        /// <remarks>The method sets the <see cref="PictureBox.Image"/> property to the banner image specified in <paramref name="options"/>. It also adjusts the size of the <see cref="PictureBox"/> to maintain the aspect ratio of the image, with a fixed width of 450 pixels.</remarks>
        /// <param name="pictureBox">The <see cref="PictureBox"/> to configure. Cannot be <see langword="null"/>.</param>
        /// <param name="options">The options containing the banner image to display. Cannot be <see langword="null"/>.</param>
        protected void SetPictureBox(PictureBox pictureBox, BaseOptions options)
        {
            double dpiScale = User32.GetDpiForWindow((HWND)Handle) / 96.0;
            pictureBox.Image = GetBanner(options.AppBannerImage);
            pictureBox.Size = new((int)Math.Ceiling(450.0 * dpiScale), (int)Math.Ceiling(450.0 * (pictureBox.Image.Height / (double)pictureBox.Image.Width) * dpiScale));
        }

        /// <summary>
        /// Format the time span to a string.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        protected static string FormatTime(TimeSpan ts)
        {
            return $"{(ts.Days * 24) + ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonLeft_Click(object sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonMiddle_Click(object sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonRight_Click(object sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the form's load event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Form_Load(object? sender, EventArgs e)
        {
            // Adjust the menu depending on our config options.
            using (DestroyMenuSafeHandle menuHandle = User32.GetSystemMenu((HWND)Handle, false))
            {
                // Disable the close button on the form. Failing that, disable the ControlBox.
                try
                {
                    _ = User32.EnableMenuItem(menuHandle, WM_SYSCOMMAND.SC_CLOSE, MENU_ITEM_FLAGS.MF_GRAYED);
                }
                catch
                {
                    ControlBox = false;
                }

                // Disable the move command on the system menu if we can't move the dialog.
                if (!dialogAllowMove)
                {
                    _ = User32.RemoveMenu(menuHandle, WM_SYSCOMMAND.SC_MOVE, MENU_ITEM_FLAGS.MF_BYCOMMAND);
                }
            }

            // Subscribe to system events for display and user preference changes.
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // Set the form's starting location.
            PositionForm();

            // Start the persist timer if it's available.
            persistTimer?.Start();
            expiryTimer?.Start();
        }

        /// <summary>
        /// Handles the event that occurs when the display settings change, ensuring the form is repositioned
        /// appropriately.
        /// </summary>
        /// <remarks>This method is intended to respond to system-level display configuration changes,
        /// such as resolution or monitor layout updates. It ensures that the form remains correctly positioned after
        /// such changes by invoking the repositioning logic on the UI thread.</remarks>
        /// <param name="sender">The source of the event, typically the system event manager.</param>
        /// <param name="e">An <see cref="EventArgs"/> instance containing event data.</param>
        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            // We're on a thread pool thread → marshal back to UI thread.
            if (!IsDisposed && IsHandleCreated)
            {
                _ = BeginInvoke(PositionForm);
            }
        }

        /// <summary>
        /// Handles the UserPreferenceChanged event to respond to changes in user preferences such as general, desktop,
        /// or window settings.
        /// </summary>
        /// <remarks>This method is typically used to reposition or update the form when relevant user
        /// preferences change, such as when the taskbar is moved or resized. It only responds to changes in the
        /// General, Desktop, or Window categories and ignores other categories.</remarks>
        /// <param name="sender">The source of the event, typically the system event dispatcher.</param>
        /// <param name="e">An object containing data about the user preference change, including the category of the change.</param>
        private void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
        {
            // Taskbar moves / size changes often show up here.
            if (!IsDisposed && IsHandleCreated && (e.Category == UserPreferenceCategory.General || e.Category == UserPreferenceCategory.Desktop || e.Category == UserPreferenceCategory.Window))
            {
                _ = BeginInvoke(PositionForm);
            }
        }

        /// <summary>
        /// Handles the form's closing event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void Form_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel the event if we can't close (i.e. user has closed from the taskbar)
            if (!CanClose())
            {
                e.Cancel = true;
                return;
            }

            // Unsubscribe from system events.
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;

            // We're actually closing. Perform certain disposals here
            // since we can't mess with the designer's Dispose override.
            if (persistTimer is not null)
            {
                persistTimer.Stop();
                persistTimer.Dispose();
                persistTimer = null;
            }
            if (expiryTimer is not null)
            {
                expiryTimer.Stop();
                expiryTimer.Dispose();
                expiryTimer = null;
            }
        }

        /// <summary>
        /// Tests whether this form is allowed to close down.
        /// </summary>
        /// <returns></returns>
        protected bool CanClose()
        {
            return canClose;
        }

        /// <summary>
        /// Restores the window to its normal state and repositions it to its starting location.
        /// </summary>
        /// <remarks>This method resets the window's state to <see cref="FormWindowState.Normal"/>, moves it to the predefined starting location, and brings it to the front of the z-order.</remarks>
        protected void RestoreWindow()
        {
            // Reset the window and restore its location.
            WindowState = FormWindowState.Normal;
            Location = startingPoint;
            BringToFront();
        }

        /// <summary>
        /// Resets the persist timer to its initial state.
        /// </summary>
        /// <remarks>This method stops the current persist timer, if it is running, and restarts it. It ensures that the timer is reset and begins counting from its initial duration.</remarks>
        protected void ResetPersistTimer()
        {
            // Reset the persist timer to its initial state.
            if (persistTimer is not null)
            {
                persistTimer.Stop();
                persistTimer.Start();
            }
        }

        /// <summary>
        /// Removes formatting tags (e.g., bold, italic, accent, and URL tags) from the specified text, replacing them
        /// with their corresponding plain text content.
        /// </summary>
        /// <remarks>This method processes text to remove specific formatting tags, such as bold, italic,
        /// accent, and URL tags, as defined by the <see cref="DialogConstants.TextFormattingRegex"/> regular expression.
        /// The content within these tags is preserved and included in the returned string. Handles nested tags properly
        /// by repeatedly processing the text until all tags are removed.</remarks>
        /// <param name="text">The input string containing formatting tags to be stripped.</param>
        /// <returns>A string with all recognized formatting tags replaced by their plain text equivalents.</returns>
        protected static string StripFormattingTags(string text)
        {
            foreach (Match match in DialogConstants.TextFormattingRegex.Matches(text))
            {
                if (match.Groups["UrlLinkSimple"] is Group urlLinkSimple && urlLinkSimple.Success)
                {
                    text = text.Replace(urlLinkSimple.Value, match.Groups["UrlLinkSimpleContent"].Value);
                }
                else if (match.Groups["UrlLinkDescriptive"] is Group urlLinkDescriptive && urlLinkDescriptive.Success)
                {
                    text = text.Replace(urlLinkDescriptive.Value, match.Groups["UrlLinkDescription"].Value);
                }
                else
                {
                    foreach (Group formattingTag in match.Groups.OfType<Group>().Where(static g => g.Success && (g.Name.StartsWith("Open", StringComparison.Ordinal) || g.Name.StartsWith("Close", StringComparison.Ordinal))))
                    {
                        text = text.Replace(formattingTag.Value, null);
                    }
                }
            }
            return text;
        }

        /// <summary>
        /// Processes Windows messages for the window, preventing movement when not allowed.
        /// </summary>
        /// <remarks>If movement of the window is not permitted, system commands to move the window are
        /// ignored. All other messages are processed normally by the base implementation.</remarks>
        /// <param name="m">A reference to the Windows message to be processed.</param>
        protected override void WndProc(ref Message m)
        {
            // Ignore any attempt to move the window.
            if (m.Msg == (uint)WINDOW_MESSAGE.WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == (uint)WM_SYSCOMMAND.SC_MOVE && !dialogAllowMove)
            {
                return;
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// Positions the form on the screen based on the specified dialog position.
        /// </summary>
        /// <remarks>The form is positioned within the working area of the screen that contains the form. The position is determined by the <see cref="dialogPosition"/> field, which specifies predefined locations such as top-left, center, or bottom-right. If the calculated position exceeds the working area bounds, it is clamped to ensure the form remains fully visible.</remarks>
        private void PositionForm()
        {
            // Get the working area (pixels not DIPs)
            Screen screen = Screen.FromControl(this);
            Rectangle workingArea = screen.WorkingArea;

            double left, top;
            switch (dialogPosition)
            {
                case DialogPosition.TopLeft:
                    left = workingArea.Left;
                    top = workingArea.Top;
                    break;

                case DialogPosition.Top:
                    left = workingArea.Left + ((workingArea.Width - Width) / 2);
                    top = workingArea.Top;
                    break;

                case DialogPosition.TopRight:
                    left = workingArea.Right - Width;
                    top = workingArea.Top;
                    break;

                case DialogPosition.TopCenter:
                    left = workingArea.Left + ((workingArea.Width - Width) / 2);
                    top = workingArea.Top + ((workingArea.Height - Height) * (1.0 / 6.0));
                    break;

                case DialogPosition.BottomLeft:
                    left = workingArea.Left;
                    top = workingArea.Bottom - Height;
                    break;

                case DialogPosition.Bottom:
                    left = workingArea.Left + ((workingArea.Width - Width) / 2);
                    top = workingArea.Bottom - Height;
                    break;

                case DialogPosition.BottomCenter:
                    left = workingArea.Left + ((workingArea.Width - Width) / 2);
                    top = workingArea.Top + ((workingArea.Height - Height) * (5.0 / 6.0));
                    break;

                case DialogPosition.BottomRight:
                    left = workingArea.Right - Width;
                    top = workingArea.Bottom - Height;
                    break;

                case DialogPosition.Center:
                case DialogPosition.Default:
                default:
                    left = workingArea.Left + ((workingArea.Width - Width) / 2);
                    top = workingArea.Top + ((workingArea.Height - Height) / 2);
                    break;
            }

            // Clamp to working-area bounds
            left = Math.Max(workingArea.Left, Math.Min(left, workingArea.Right - Width));
            top = Math.Max(workingArea.Top, Math.Min(top, workingArea.Bottom - Height));

            // Align positions to whole pixels.
            left = Math.Floor(left);
            top = Math.Floor(top);

            // Adjust for workArea offset.
            string dialogPosName = dialogPosition.ToString();
            left += dialogPosName.EndsWith("Right", StringComparison.Ordinal) ? 1 : dialogPosName.EndsWith("Left", StringComparison.Ordinal) ? -1 : 0;
            top += dialogPosName.StartsWith("Bottom", StringComparison.Ordinal) ? 1 : dialogPosName.StartsWith("Top", StringComparison.Ordinal) ? -1 : 0;

            // Set the form’s location
            Location = startingPoint = new((int)left, (int)top);
        }

        /// <summary>
        /// Handles the timer tick event for persisting the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PersistTimer_Tick(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        /// <summary>
        /// Get the icon for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Icon GetIcon(string path)
        {
            // Use a cached icon if available, otherwise load and cache it before returning it.
            if (!iconCache.TryGetValue(path, out Icon? icon))
            {
                using Icon source = !Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase) ? DrawingUtilities.ConvertBitmapToIcon(path) : new(path);
                icon = (Icon)source.Clone();
                iconCache.Add(path, icon);
            }
            return icon;
        }

        /// <summary>
        /// Get the banner image for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static Bitmap GetBanner(string path)
        {
            // Use a cached image if available, otherwise load and cache it before returning it.
            if (!imageCache.TryGetValue(path, out Bitmap? image))
            {
                using Image source = Image.FromFile(path);
                image = (Bitmap)source.Clone();
                imageCache.Add(path, image);
            }
            return image;
        }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "The redefinition of this field is by design.")]
        public new object DialogResult { get; private protected set; } = "Timeout";

        /// <summary>
        /// Starting point for the dialog.
        /// </summary>
        private Point startingPoint;

        /// <summary>
        /// Flag to indicate if the dialog can be closed.
        /// </summary>
        private bool canClose;

        /// <summary>
        /// A timer used to restore the dialog's position on the screen at a configured interval.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "We can't override the designer's Dispose() implementation.")]
        private Timer? persistTimer;

        /// <summary>
        /// A timer used to close the dialog at a configured interval after no user response.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "We can't override the designer's Dispose() implementation.")]
        private Timer? expiryTimer;

        /// <summary>
        /// Represents the position of the dialog within its container.
        /// </summary>
        /// <remarks>The default value is <see cref="DialogPosition.Center"/>, which centers the dialog.</remarks>
        private readonly DialogPosition dialogPosition = DialogPosition.Center;

        /// <summary>
        /// Indicates whether the dialog is allowed to be moved.
        /// </summary>
        private readonly bool dialogAllowMove = true;

        /// <summary>
        /// Cache for icons to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Icon> iconCache = [];

        /// <summary>
        /// Cache for banners to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Bitmap> imageCache = [];
    }
}
