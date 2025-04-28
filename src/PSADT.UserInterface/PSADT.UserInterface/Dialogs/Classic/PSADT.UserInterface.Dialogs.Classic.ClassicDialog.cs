using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.LibraryInterfaces;
using PSADT.UserInterface.Utilities;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Base class for classic dialog forms.
    /// </summary>
    #warning "TODO: Add public contract for dialogs."
    public partial class ClassicDialog : Form
    {
        /// <summary>
        /// Static constructor to initialize the application settings.
        /// </summary>
        static ClassicDialog()
        {
            // Only run in the actual app, not in Visual Studio's designer.
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassicDialog"/> class.
        /// </summary>
        public ClassicDialog() : this(default!)
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
        public ClassicDialog(BaseOptions options) : base()
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
                #warning "TODO: DialogAccentColor?"
                this.flowLayoutPanelBase.ResumeLayout();
                this.FormClosing += Form_FormClosing;
                this.Load += Form_Load;
                this.ResumeLayout();

                // PersistPrompt timer code.
                if (options.DialogExpiryDuration != TimeSpan.Zero)
                {
                    this.persistTimer = new Timer() { Interval = (int)options.DialogExpiryDuration.TotalMilliseconds };
                    this.persistTimer.Tick += PersistTimer_Tick;
                }
            }
        }

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        public string? Result { get; protected set; }

        /// <summary>
        /// Closes the dialog.
        /// </summary>
        public void CloseDialog()
        {
            canClose = true;
            Close();
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
        private void Form_Load(object? sender, EventArgs e)
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
        private void Form_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Cancel the event if we can't close (i.e. user has closed from the taskbar)
            if (!canClose)
            {
                e.Cancel = true;
                return;
            }

            // We're actually closing. Perform certain disposals here
            // since we can't mess with the designer's Dispose override.
            persistTimer?.Dispose();
        }

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
            // Check if the icon is already cached.
            if (iconCache.TryGetValue(path, out Icon? icon))
            {
                return icon;
            }

            // If we're not dealing with an icon, convert it and return it.
            if (!Path.GetExtension(path).Equals(".ico", StringComparison.OrdinalIgnoreCase))
            {
                icon = DrawingUtilities.ConvertBitmapToIcon(path);
                iconCache.Add(path, icon);
                return icon;
            }

            // We've got an actual icon! Let's load it and cache it.
            using (var source = new Icon(path))
            {
                icon = (Icon)source.Clone();
                iconCache.Add(path, icon);
                return icon;
            }
        }

        /// <summary>
        /// Get the banner image for the dialog.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static Bitmap GetBanner(string path)
        {
            // Check if the image is already cached.
            if (imageCache.TryGetValue(path, out Bitmap? image))
            {
                return image;
            }

            // Load the image and cache it before returning.
            using (var source = Bitmap.FromFile(path))
            {
                image = (Bitmap)source.Clone();
                imageCache.Add(path, image);
                return image;
            }
        }

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
        private static readonly Dictionary <string, Icon> iconCache = [];

        /// <summary>
        /// Cache for banners to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary <string, Bitmap> imageCache = [];
    }
}
