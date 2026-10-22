using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using PSADT.DeviceManagement;
using PSADT.Interop;
using PSADT.UserInterface.DialogOptions;
using PSADT.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Base class for classic dialog forms.
    /// </summary>
    internal partial class ClassicDialog : ClassicBase, IBaseDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0191:Do not use the null-forgiving operator", Justification = "This is necessary here.")]
        internal ClassicDialog() : this(null!, null!)
        {
            if (LicenseManager.UsageMode is LicenseUsageMode.Runtime)
            {
                throw new NotSupportedException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the ClassicDialog class using the specified dialog options and result object.
        /// </summary>
        /// <remarks>This constructor applies the provided options to set up the dialog's properties,
        /// including title, icon, timers for expiry and persistence, position, and movement capability. It also
        /// attaches event handlers for dialog loading and closing.</remarks>
        /// <param name="options">The options that configure the dialog's appearance and behavior. Must not be null.</param>
        /// <param name="dialogResult">An object representing the result of the dialog interaction, used to determine the outcome when the dialog
        /// is closed.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0056:Do not call overridable members in constructor", Justification = "This is OK here.")]
        private protected ClassicDialog(BaseDialogOptions options, IDialogResult dialogResult)
        {
            // Initialise the underlying form as set up by the designer.
            InitializeComponent();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Base properties.
                SuspendLayout();
                Text = StripFormattingTags(options.AppTitle);
                Icon = GetIcon(options.AppTaskbarIconImage ?? options.AppIconImage);
                TopMost = options.DialogTopMost;
                ActiveControl = buttonDefault;
                FormClosing += Form_FormClosing;
                FormClosed += Form_FormClosed;
                Load += Form_Load;
                SizeChanged += Form_SizeChanged;
                Disposed += (_, _) =>
                {
                    foreach (Font font in scaledFonts)
                    {
                        font.Dispose();
                    }
                };
                ResumeLayout();

                // Set the expiry timer if specified.
                if (options.DialogExpiryDuration > TimeSpan.Zero)
                {
                    expiryTimer.Interval = (int)options.DialogExpiryDuration.Value.TotalMilliseconds;
                }

                // PersistPrompt timer code.
                if (options.DialogPersistInterval > TimeSpan.Zero)
                {
                    persistTimer.Interval = (int)options.DialogPersistInterval.Value.TotalMilliseconds;
                }

                // Set the optional dialog position.
                if (options.DialogPosition is not null)
                {
                    dialogPosition = options.DialogPosition.Value;
                }
                else if (!DeviceUtilities.IsOOBEComplete())
                {
                    dialogPosition = DialogPosition.Oobe;
                }

                // Set whether the dialog can be moved.
                if (options.DialogAllowMove is not null)
                {
                    dialogAllowMove = options.DialogAllowMove.Value;
                }

                // Minimize caption-button support is off by default; callers (and PSAppDeployToolkit's
                // -AllowMinimize) must explicitly opt in by setting DialogAllowMinimize=true in
                // BaseDialogOptions. Assigned either way rather than only on opting in, mirroring how
                // the Fluent dialog applies the same option.
                //
                // The minimize box is all that has to move; the designer already supplies the rest, so
                // the four further assignments this block used to carry only rewrote existing values.
                MinimizeBox = options.DialogAllowMinimize is true;

                // Set the requested scale, applied once the derived dialog has laid itself out.
                if (options.ClassicDialogScale is not null)
                {
                    dialogScale = options.ClassicDialogScale.Value;
                }
            }

            // Set the dialog result.
            DialogResult = dialogResult;
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
        /// <remarks>The method sets the <see cref="PictureBox.Image"/> property to the banner image specified in <paramref name="options"/>. It also adjusts the size of the <see cref="PictureBox"/> to maintain the aspect ratio of the image, with a fixed width of 512 pixels.</remarks>
        /// <param name="pictureBox">The <see cref="PictureBox"/> to configure. Cannot be <see langword="null"/>.</param>
        /// <param name="options">The options containing the banner image to display. Cannot be <see langword="null"/>.</param>
        private protected void SetPictureBox(PictureBox pictureBox, BaseDialogOptions options)
        {
            double dpiScale = NativeMethods.GetDpiForWindow((HWND)Handle) / 96.0;
            pictureBox.Image = GetBanner(options.AppBannerImage);
            pictureBox.Size = new((int)Math.Ceiling(512.0 * dpiScale), (int)Math.Ceiling(512.0 * (pictureBox.Image.Height / (double)pictureBox.Image.Width) * dpiScale));
        }

        /// <summary>
        /// Lets the specified list box give up height, down to four rows, when the dialog is too tall for the screen.
        /// </summary>
        /// <remarks>The list box must fill a panel of its own, whose height is what gets constrained.</remarks>
        /// <param name="listBox">The list box to allow to shrink. Its rows already scroll.</param>
        private protected void AllowScrolling(ListBox listBox)
        {
            scrollableList = listBox;
        }

        /// <summary>
        /// Lets the label in the specified panel scroll, down to four lines, when the dialog is too tall for the screen.
        /// </summary>
        /// <remarks>The panel must hold only the label and size itself to it, so constraining the panel's height is what makes the label scroll.</remarks>
        /// <param name="host">The panel hosting the label.</param>
        private protected void AllowScrolling(Panel host)
        {
            scrollableLabelHosts.Add(host);
        }

        /// <summary>
        /// Formats a specified time interval as a string in hours, minutes, and seconds.
        /// </summary>
        /// <remarks>The formatted string includes the total number of hours, which may exceed 24 if the
        /// time interval spans multiple days. Minutes and seconds are always displayed as two digits.</remarks>
        /// <param name="ts">The time interval to format.</param>
        /// <returns>A string representation of the total hours, minutes, and seconds in the format "HH:MM:SS".</returns>
        private protected static string FormatTime(TimeSpan ts)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{(ts.Days * 24) + ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}");
        }

        /// <summary>
        /// Handles the click event for the left button and closes the dialog.
        /// </summary>
        /// <remarks>Override this method in a derived class to implement custom behavior when the left
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the left button that was clicked.</param>
        /// <param name="e">An object that contains the event data.</param>
        private protected virtual void ButtonLeft_Click(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event for the middle button and closes the dialog.
        /// </summary>
        /// <remarks>Override this method in a derived class to implement custom behavior when the middle
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">An object that contains the event data.</param>
        private protected virtual void ButtonMiddle_Click(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the click event for the right button and closes the dialog.
        /// </summary>
        /// <remarks>Override this method in a derived class to provide custom behavior when the right
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the right button that was clicked.</param>
        /// <param name="e">An object that contains the event data.</param>
        private protected virtual void ButtonRight_Click(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Handles the form's load event to initialize settings, configure the system menu, subscribe to system events,
        /// and start relevant timers.
        /// </summary>
        /// <remarks>This method disables or adjusts certain system menu items based on configuration,
        /// subscribes to display and user preference change events, sets the initial position of the form, and starts
        /// timers related to form persistence and expiry. It also signals operation success for client-server
        /// scenarios.</remarks>
        /// <param name="sender">The source of the event, typically the form being loaded.</param>
        /// <param name="e">An EventArgs instance containing the event data.</param>
        private protected virtual void Form_Load(object? sender, EventArgs e)
        {
            // Adjust the menu depending on our config options.
            using (DestroyMenuSafeHandle menuHandle = NativeMethods.GetSystemMenu((HWND)Handle, bRevert: false))
            {
                // Disable the close button on the form. Failing that, disable the ControlBox.
                _ = NativeMethods.EnableMenuItem(menuHandle, WM_SYSCOMMAND.SC_CLOSE, MENU_ITEM_FLAGS.MF_GRAYED);

                // Disable the move command on the system menu if we can't move the dialog.
                if (!dialogAllowMove)
                {
                    _ = NativeMethods.RemoveMenu(menuHandle, WM_SYSCOMMAND.SC_MOVE, MENU_ITEM_FLAGS.MF_BYCOMMAND);
                }
            }

            // Subscribe to system events for display and user preference changes.
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

            // Set the form's size and starting location.
            FitAndPositionForm();

            // Start the persist timer if it's available.
            if (persistTimer.Interval != int.MaxValue)
            {
                persistTimer.Start();
            }
            if (expiryTimer.Interval != int.MaxValue)
            {
                expiryTimer.Start();
            }
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
                _ = BeginInvoke(FitAndPositionForm);
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
            if (!IsDisposed && IsHandleCreated && (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Desktop or UserPreferenceCategory.Window))
            {
                _ = BeginInvoke(FitAndPositionForm);
            }
        }

        /// <summary>
        /// Handles the form's size changing, such as when its content changes, by refitting and repositioning it.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Form_SizeChanged(object? sender, EventArgs e)
        {
            // Bringing the dialog to the front minimizes and restores it, and a minimized form has no size to fit.
            if (!fitting && Visible && WindowState is FormWindowState.Normal && Size != fittedSize)
            {
                FitAndPositionForm();
            }
        }

        /// <summary>
        /// Handles the form's closing event, allowing for cleanup operations and the option to cancel the closing
        /// process.
        /// </summary>
        /// <remarks>If the form cannot be closed, the closing operation can be canceled by setting <paramref
        /// name="e"/>.Cancel to <see langword="true"/>.</remarks>
        /// <param name="sender">The source of the event, typically the form that is being closed.</param>
        /// <param name="e">A FormClosingEventArgs that contains data related to the closing event, including the ability to cancel the
        /// operation.</param>
        private void Form_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel the event if we can't close (i.e. user has closed from the taskbar)
            if (!CanClose())
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Handles the FormClosed event by unsubscribing from system events and disposing of timers.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private protected virtual void Form_FormClosed(object? sender, FormClosedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            persistTimer.Stop();
            expiryTimer.Stop();
        }

        /// <summary>
        /// Determines whether the current instance can be closed.
        /// </summary>
        /// <returns>true if the instance can be closed; otherwise, false.</returns>
        private protected bool CanClose()
        {
            return canClose;
        }

        /// <summary>
        /// Restores the window to its normal state and repositions it to its starting location.
        /// </summary>
        /// <remarks>This method resets the window's state to <see cref="FormWindowState.Normal"/>, moves it to the predefined starting location, and brings it to the front of the z-order.</remarks>
        private protected void RestoreWindow()
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
        private protected void ResetPersistTimer()
        {
            // Reset the persist timer to its initial state.
            if (persistTimer.Enabled)
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
        /// accent, and URL tags, as defined by the <see cref="DialogText.FormattingRegex"/> regular expression.
        /// The content within these tags is preserved and included in the returned string.
        /// <para>
        /// One pass over the matches found in the original text suffices. The pattern matches each opening and
        /// closing style tag as a match in its own right rather than as a pair, so a nested tag is simply another
        /// match, and each match is removed by replacing its literal text rather than by its position. That means
        /// every occurrence of a given tag goes at once and the later matches for it become no-ops, which is also
        /// why the positions recorded in the matches are never read - they refer to the text as it was before any
        /// replacement.
        /// </para></remarks>
        /// <param name="text">The input string containing formatting tags to be stripped.</param>
        /// <returns>A string with all recognized formatting tags replaced by their plain text equivalents.</returns>
        private protected static string StripFormattingTags(string text)
        {
            foreach (Match match in DialogText.FormattingRegex.Matches(text))
            {
                if (match.Groups["UrlLinkSimple"] is Group urlLinkSimple && urlLinkSimple.Success)
                {
                    text = text.Replace(urlLinkSimple.Value, match.Groups["UrlLinkSimpleContent"].Value, StringComparison.Ordinal);
                }
                else if (match.Groups["UrlLinkDescriptive"] is Group urlLinkDescriptive && urlLinkDescriptive.Success)
                {
                    text = text.Replace(urlLinkDescriptive.Value, match.Groups["UrlLinkDescription"].Value, StringComparison.Ordinal);
                }
                else
                {
                    foreach (Group formattingTag in match.Groups.OfType<Group>().Where(static g => g.Success && (g.Name.StartsWith("Open", StringComparison.Ordinal) || g.Name.StartsWith("Close", StringComparison.Ordinal))))
                    {
                        text = text.Replace(formattingTag.Value, newValue: null, StringComparison.Ordinal);
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
                    {
                        left = workingArea.Left;
                        top = workingArea.Top;
                        break;
                    }

                case DialogPosition.Top:
                    {
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5);
                        top = workingArea.Top;
                        break;
                    }

                case DialogPosition.TopRight:
                    {
                        left = workingArea.Right - Width;
                        top = workingArea.Top;
                        break;
                    }

                case DialogPosition.TopCenter:
                    {
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5);
                        top = workingArea.Top + ((workingArea.Height - Height) * (1.0 / 6.0));
                        break;
                    }

                case DialogPosition.BottomLeft:
                    {
                        left = workingArea.Left;
                        top = workingArea.Bottom - Height;
                        break;
                    }

                case DialogPosition.Bottom:
                    {
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5);
                        top = workingArea.Bottom - Height;
                        break;
                    }

                case DialogPosition.BottomCenter:
                    {
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5);
                        top = workingArea.Top + ((workingArea.Height - Height) * (5.0 / 6.0));
                        break;
                    }

                case DialogPosition.BottomRight:
                    {
                        left = workingArea.Right - Width;
                        top = workingArea.Bottom - Height;
                        break;
                    }

                case DialogPosition.Oobe:
                    {
                        // Center vertically on full screen (compensating for non-existent taskbar in OOBE)
                        // Calculate taskbar offset in pixels: difference between full screen and working area
                        Rectangle screenBounds = screen.Bounds;
                        int taskbarOffsetPixels = screenBounds.Height - workingArea.Height - (workingArea.Top - screenBounds.Top);
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5) - (Width * 0.6);
                        top = workingArea.Top + ((workingArea.Height - Height) * 0.5) + (taskbarOffsetPixels * 0.5);
                        break;
                    }

                case DialogPosition.Center:
                case DialogPosition.Default:
                default:
                    {
                        left = workingArea.Left + ((workingArea.Width - Width) * 0.5);
                        top = workingArea.Top + ((workingArea.Height - Height) * 0.5);
                        break;
                    }
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
        /// Fits the form within the working area of its screen, then positions it.
        /// </summary>
        private void FitAndPositionForm()
        {
            FitToWorkingArea();
            PositionForm();
        }

        /// <summary>
        /// Fits the form within the working area of its screen, first letting its scrollable regions give up height and
        /// only then scaling it down.
        /// </summary>
        private void FitToWorkingArea()
        {
            if (fitting || WindowState is not FormWindowState.Normal)
            {
                return;
            }
            fitting = true;
            try
            {
                // Lay the dialog out at its designed size with nothing constrained to find its natural size. Fonts and
                // wrapping don't scale linearly, so measuring at whatever scale is applied would give a different answer.
                foreach (Panel host in scrollableLabelHosts)
                {
                    SetLabelScrollHeight(host, height: null);
                }
                ScaleDialog(1.0);
                Panel? listPanel = scrollableList?.Parent is Panel panel && Contains(panel) ? panel : null;
                if (listPanel is not null)
                {
                    designListHeight ??= listPanel.Height;
                    listPanel.Height = designListHeight.Value;
                }

                // The list gives up height first, down to four rows so its scroll bar keeps room for a thumb between its arrows, then each label down to four lines.
                List<DialogFitRegion> regions = [];
                if (listPanel is not null && scrollableList is not null)
                {
                    int listBorder = scrollableList.Height - scrollableList.ClientSize.Height;
                    regions.Add(new(listPanel.Height, Math.Min(listPanel.Height, (scrollableList.ItemHeight * 4) + listBorder)));
                }
                // Lines are measured with GDI, as the label draws with it, rather than trusting Font.Height from GDI+.
                foreach (Panel host in scrollableLabelHosts)
                {
                    regions.Add(new(host.Height, Math.Min(host.Height, TextRenderer.MeasureText("X", host.Controls[0].Font).Height * 4)));
                }

                // Fit within the working area, less the margin the dialog keeps from its edges and the window frame.
                Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                int margin = (int)Math.Round(ScreenEdgeMargin * NativeMethods.GetDpiForWindow((HWND)Handle) / 96.0, MidpointRounding.AwayFromZero);
                Size frame = Size - ClientSize;
                int availableHeight = workingArea.Height - (2 * margin) - frame.Height;
                DialogFit fit = DialogFitCalculator.Calculate(ClientSize.Width, ClientSize.Height, workingArea.Width - (2 * margin) - frame.Width, availableHeight, dialogScale, regions);
                ApplyFit(fit.Scale);

                // WinForms rounds every scaled size, so take off whatever overflow that leaves behind.
                if (ClientSize.Height > availableHeight)
                {
                    ApplyFit(Math.Max(BaseDialogOptions.MinimumDialogScale, appliedScale * availableHeight / ClientSize.Height));
                }
                fittedSize = Size;

                // Rescaling restores the designed layout, so the regions are unconstrained first and constrained again after.
                void ApplyFit(double scale)
                {
                    foreach (Panel host in scrollableLabelHosts)
                    {
                        SetLabelScrollHeight(host, height: null);
                    }
                    ScaleDialog(scale);
                    SuspendLayout();
                    int region = 0;
                    if (listPanel is not null)
                    {
                        double maxHeight = fit.RegionMaxHeights[region++];
                        if (!double.IsPositiveInfinity(maxHeight))
                        {
                            listPanel.Height = (int)Math.Floor(maxHeight * appliedScale);
                        }
                    }
                    foreach (Panel host in scrollableLabelHosts)
                    {
                        double maxHeight = fit.RegionMaxHeights[region++];
                        if (!double.IsPositiveInfinity(maxHeight))
                        {
                            SetLabelScrollHeight(host, (int)Math.Floor(maxHeight * appliedScale));
                        }
                    }
                    ResumeLayout();
                }
            }
            finally
            {
                fitting = false;
            }
        }

        /// <summary>
        /// Constrains a scrollable label's host panel to the specified height, or lets it size to the label again.
        /// </summary>
        /// <param name="host">The panel hosting the label.</param>
        /// <param name="height">The height to constrain the panel to, or <see langword="null"/> to stop constraining it.</param>
        private static void SetLabelScrollHeight(Panel host, int? height)
        {
            // The label narrows by the scroll bar's width while it scrolls so its text never runs beneath the bar.
            // Whether the panel already scrolls records whether the label is narrowed, so it is only ever narrowed once.
            Control label = host.Controls[0];
            int scrollBarWidth = SystemInformation.VerticalScrollBarWidth;
            if (height is null)
            {
                if (host.AutoScroll)
                {
                    host.AutoScroll = false;
                    label.MaximumSize = new(label.MaximumSize.Width + scrollBarWidth, 0);
                    label.MinimumSize = new(label.MinimumSize.Width > 0 ? label.MinimumSize.Width + scrollBarWidth : 0, label.MinimumSize.Height);
                    host.AutoSize = true;
                }
                return;
            }
            Size size = new(host.Width, height.Value);
            if (!host.AutoScroll)
            {
                host.AutoSize = false;
                label.MaximumSize = new(label.MaximumSize.Width - scrollBarWidth, 0);
                label.MinimumSize = new(label.MinimumSize.Width > 0 ? label.MinimumSize.Width - scrollBarWidth : 0, label.MinimumSize.Height);
            }
            host.Size = size;
            host.AutoScroll = true;
        }

        /// <summary>
        /// Scales the dialog's layout and fonts relative to its designed size.
        /// </summary>
        /// <remarks>WinForms scales bounds, margins, padding and size limits but not fonts, so every explicitly set font
        /// is rescaled from its designed size. The layout is restored to its design before every scale rather than scaled
        /// by the ratio to the last one, as WinForms rounds each scaled size and repeated round trips would drift.</remarks>
        /// <param name="scale">The scale to apply, where 1.0 is the designed size.</param>
        private void ScaleDialog(double scale)
        {
            if (Math.Abs(scale - appliedScale) < 0.001)
            {
                return;
            }

            // Capture the design before the first scale. A font is explicitly set wherever it isn't the very instance inherited from the parent.
            if (designFonts is null || designLayout is null || designStyleSizes is null)
            {
                designFonts = new() { { this, Font } };
                designLayout = [];
                designStyleSizes = [];
                Stack<Control> pending = new([this]);
                while (pending.Count > 0)
                {
                    Control parent = pending.Pop();
                    designLayout.Add(parent, (parent.Bounds, parent.Margin, parent.Padding, parent.MinimumSize, parent.MaximumSize));
                    if (parent is TableLayoutPanel table)
                    {
                        foreach (ColumnStyle style in table.ColumnStyles)
                        {
                            designStyleSizes.Add(style, style.Width);
                        }
                        foreach (RowStyle style in table.RowStyles)
                        {
                            designStyleSizes.Add(style, style.Height);
                        }
                    }
                    foreach (Control child in parent.Controls)
                    {
                        if (!ReferenceEquals(child.Font, parent.Font))
                        {
                            designFonts.Add(child, child.Font);
                        }
                        pending.Push(child);
                    }
                }
            }

            // Restore the design, scale it, then swap in the fonts and release the ones they replace.
            SuspendLayout();
            foreach (KeyValuePair<Control, (Rectangle Bounds, Padding Margin, Padding Padding, Size MinimumSize, Size MaximumSize)> design in designLayout)
            {
                // The form's own bounds are left alone, as it sizes itself to its content and is positioned after fitting.
                Control control = design.Key;
                control.MinimumSize = Size.Empty;
                control.MaximumSize = design.Value.MaximumSize;
                control.MinimumSize = design.Value.MinimumSize;
                control.Margin = design.Value.Margin;
                control.Padding = design.Value.Padding;
                if (!ReferenceEquals(control, this))
                {
                    control.Bounds = design.Value.Bounds;
                }
            }
            foreach (KeyValuePair<TableLayoutStyle, float> design in designStyleSizes)
            {
                if (design.Key is ColumnStyle column)
                {
                    column.Width = design.Value;
                }
                else if (design.Key is RowStyle row)
                {
                    row.Height = design.Value;
                }
            }
            if (Math.Abs(scale - 1.0) >= 0.001)
            {
                Scale(new SizeF((float)scale, (float)scale));
            }
            List<Font> replacedFonts = scaledFonts;
            scaledFonts = [];
            foreach (KeyValuePair<Control, Font> designFont in designFonts)
            {
                if (Math.Abs(scale - 1.0) < 0.001)
                {
                    designFont.Key.Font = designFont.Value;
                    continue;
                }
                Font font = new(designFont.Value.FontFamily, (float)(designFont.Value.Size * scale), designFont.Value.Style, designFont.Value.Unit, designFont.Value.GdiCharSet);
                designFont.Key.Font = font;
                scaledFonts.Add(font);
            }
            ResumeLayout();
            foreach (Font font in replacedFonts)
            {
                font.Dispose();
            }
            appliedScale = scale;
        }

        /// <summary>
        /// Handles the timer tick event for persisting the dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private void PersistTimer_Tick(object? sender, EventArgs e)
        {
            RestoreWindow();
        }

        /// <summary>
        /// Closes the dialog when the expiry timer elapses.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExpiryTimer_Tick(object? sender, EventArgs e)
        {
            CloseDialog();
        }

        /// <summary>
        /// Retrieves an icon from the specified file path, using a cached version if available.
        /// </summary>
        /// <remarks>If the specified path does not refer to an icon file, the image is converted to icon
        /// format before being returned. Subsequent calls with the same path will return the cached icon instance. This
        /// method is intended for internal use within the assembly.</remarks>
        /// <param name="path">The file path to the icon image. This can be a path to a standard image file or a base64-encoded image
        /// string. The path must not be null or empty.</param>
        /// <returns>An <see cref="Icon"/> object representing the icon at the specified path. Returns a cached icon if it has
        /// been previously loaded.</returns>
        internal static Icon GetIcon(string path)
        {
            // Use a cached icon if available, otherwise load and cache it before returning it.
            if (!iconCache.TryGetValue(path, out Icon? icon))
            {
                using Stream stream = MiscUtilities.GetBase64StringBytes(path) is not byte[] bytes ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : new MemoryStream(bytes, writable: false);
                if (!DrawingUtilities.IsStreamAnIcon(stream))
                {
                    using Bitmap image = new(stream, useIcm: true);
                    icon = DrawingUtilities.ConvertBitmapToIcon(image);
                }
                else
                {
                    icon = new(stream, 256, 256);
                }
                iconCache.Add(path, icon);
            }
            return icon;
        }

        /// <summary>
        /// Retrieves a bitmap image from the specified file path, using a cached version if available.
        /// </summary>
        /// <remarks>If the image is not already cached, it is loaded from the file system or decoded from
        /// a base64 string and then cached for future requests. This method improves performance by avoiding repeated
        /// disk or decoding operations for the same image.</remarks>
        /// <param name="path">The file path of the image to retrieve. This can be a path to a standard image file or a base64-encoded
        /// image string.</param>
        /// <returns>A Bitmap object representing the image at the specified path.</returns>
        internal static Bitmap GetBanner(string path)
        {
            // Use a cached image if available, otherwise load and cache it before returning it.
            if (!imageCache.TryGetValue(path, out Bitmap? image))
            {
                using Stream stream = MiscUtilities.GetBase64StringBytes(path) is not byte[] bytes ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : new MemoryStream(bytes, writable: false);
                imageCache.Add(path, image = new(stream, useIcm: true));
            }
            return image;
        }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1061:Do not hide base class methods", Justification = "The redefinition of this field is by design.")]
        [DefaultValue("Timeout")]
        public new IDialogResult DialogResult { get; private protected set; }

        /// <summary>
        /// Starting point for the dialog.
        /// </summary>
        private Point startingPoint;

        /// <summary>
        /// Flag to indicate if the dialog can be closed.
        /// </summary>
        private bool canClose;

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
        /// The scale requested for the dialog, relative to its designed size.
        /// </summary>
        private readonly double dialogScale = 1.0;

        /// <summary>
        /// The scale currently applied to the dialog.
        /// </summary>
        private double appliedScale = 1.0;

        /// <summary>
        /// The explicitly set fonts of the form and its controls at their designed size, captured on first scaling.
        /// </summary>
        private Dictionary<Control, Font>? designFonts;

        /// <summary>
        /// The bounds, spacing and size limits of the form and its controls as designed, captured on first scaling.
        /// </summary>
        private Dictionary<Control, (Rectangle Bounds, Padding Margin, Padding Padding, Size MinimumSize, Size MaximumSize)>? designLayout;

        /// <summary>
        /// The sizes of the table layout row and column styles as designed, captured on first scaling.
        /// </summary>
        private Dictionary<TableLayoutStyle, float>? designStyleSizes;

        /// <summary>
        /// The fonts created for the current scale, released when replaced.
        /// </summary>
        private List<Font> scaledFonts = [];

        /// <summary>
        /// The list box allowed to give up height when the dialog is too tall, if any.
        /// </summary>
        private ListBox? scrollableList;

        /// <summary>
        /// The designed height of the scrollable list box's panel, captured on first fitting.
        /// </summary>
        private int? designListHeight;

        /// <summary>
        /// The panels hosting labels allowed to scroll when the dialog is too tall, in the order they give up height.
        /// </summary>
        private readonly List<Panel> scrollableLabelHosts = [];

        /// <summary>
        /// Whether the dialog is currently being fitted, so the size changes it causes don't refit it again.
        /// </summary>
        private bool fitting;

        /// <summary>
        /// The size the dialog was last fitted to, so returning to it, as on restoring from minimized, doesn't refit it.
        /// </summary>
        private Size fittedSize;

        /// <summary>
        /// The margin, in device-independent pixels, kept between the dialog and the edges of the working area.
        /// </summary>
        private const int ScreenEdgeMargin = 16;

        /// <summary>
        /// Cache for icons to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Icon> iconCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Cache for banners to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary<string, Bitmap> imageCache = new(StringComparer.OrdinalIgnoreCase);
    }
}
