using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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
    internal partial class ClassicDialog : Form, IDialogBase
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
            if (null != options)
            {
                // Base properties.
                this.SuspendLayout();   
                this.flowLayoutPanelBase.SuspendLayout();
                this.Text = options.AppTitle;
                this.Icon = GetIcon(options.AppIconImage);
                this.pictureBanner.Image = GetBanner(options.AppBannerImage);
                this.pictureBanner.Size = new Size(450, (int)Math.Ceiling(450.0 * ((double)this.pictureBanner.Image.Height / (double)this.pictureBanner.Image.Width)));
                #warning "TODO: DialogPosition?"
                #warning "TODO: DialogAllowMove?"
                this.TopMost = options.DialogTopMost;
                this.flowLayoutPanelBase.ResumeLayout();
                this.FormClosing += Form_FormClosing;
                this.Load += Form_Load;
                this.ResumeLayout();

                // PersistPrompt timer code.
                if (null != options.DialogPersistInterval && options.DialogPersistInterval.Value != TimeSpan.Zero)
                {
                    this.persistTimer = new Timer() { Interval = (int)options.DialogPersistInterval.Value.TotalMilliseconds };
                    this.persistTimer.Tick += PersistTimer_Tick;
                }
            }
        }

        /// <summary>
        /// Redefined ShowDialog method to allow for custom behavior.
        /// </summary>
        public new void ShowDialog()
        {
            base.ShowDialog();
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
        /// Setter for Result to get around some C# malarkey.
        /// </summary>
        /// <param name="result"></param>
        protected void SetResult(string result)
        {
            DialogResult = result;
        }

        /// <summary>
        /// Format the time span to a string.
        /// </summary>
        /// <param name="ts"></param>
        /// <returns></returns>
        protected static string FormatTime(TimeSpan ts) => $"{ts.Days * 24 + ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}";

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
            // Disable the close button on the form. Failing that, disable the ControlBox.
            try
            {
                using (var menuHandle = User32.GetSystemMenu((HWND)this.Handle, false))
                {
                    User32.EnableMenuItem(menuHandle, PInvoke.SC_CLOSE, MENU_ITEM_FLAGS.MF_GRAYED);
                }
            }
            catch
            {
                this.ControlBox = false;
            }

            // Start the persist timer if it's available.
            startingPoint = this.Location;
            persistTimer?.Start();
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

            // We're actually closing. Perform certain disposals here
            // since we can't mess with the designer's Dispose override.
            persistTimer?.Dispose();
        }

        /// <summary>
        /// Tests whether this form is allowed to close down.
        /// </summary>
        /// <returns></returns>
        protected bool CanClose() => canClose;

        /// <summary>
        /// Handles the timer tick event for persisting the dialog.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PersistTimer_Tick(object? sender, EventArgs e)
        {
            // Reset the window and restore its location.
            this.WindowState = FormWindowState.Normal;
            this.Location = startingPoint;
            this.BringToFront();
        }

        /// <summary>
        /// Get the icon for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Icon GetIcon(string path)
        {
            // Use a cached icon if available, otherwise load and cache it before returning it.
            if (!iconCache.TryGetValue(path, out Icon? icon))
            {
                using (var source = !Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase) ? DrawingUtilities.ConvertBitmapToIcon(path) : new Icon(path))
                {
                    icon = (Icon)source.Clone();
                    iconCache.Add(path, icon);
                }
            }
            return icon;
        }

        /// <summary>
        /// Get the banner image for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Bitmap GetBanner(string path)
        {
            // Use a cached image if available, otherwise load and cache it before returning it.
            if (!imageCache.TryGetValue(path, out Bitmap? image))
            {
                using (var source = Bitmap.FromFile(path))
                {
                    image = (Bitmap)source.Clone();
                    imageCache.Add(path, image);
                }
            }
            return image;
        }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        public new object DialogResult { get; private set; } = "Timeout";

        /// <summary>
        /// Timer for persisting the dialog.
        /// </summary>
        protected Timer? PersistTimer
        {
            get => persistTimer;
            private set
            {
                persistTimer?.Dispose();
                persistTimer = value;
            }
        }

        /// <summary>
        /// Private backing field for the persist timer.
        /// </summary>
        private Timer? persistTimer = null;

        /// <summary>
        /// Flag to indicate if the dialog can be closed.
        /// </summary>
        private bool canClose = false;

        /// <summary>
        /// Starting point for the dialog.
        /// </summary>
        private Point startingPoint;

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
