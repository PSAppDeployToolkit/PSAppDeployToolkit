using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Utilities;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Base class for classic dialog forms.
    /// </summary>
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
            InitializeComponent();
            if (null != options)
            {
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
                #warning "TODO: DialogPersistInterval?"
                this.flowLayoutPanelBase.ResumeLayout();
                this.ResumeLayout();
            }
        }

        /// <summary>
        /// Private backing field for the dialog result (let's not overwrite the base class's).
        /// </summary>
        private string? _result;

        /// <summary>
        /// The result of the dialog.
        /// </summary>
        public string? Result
        {
            get => _result;
            protected set => _result = value;
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonLeft_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonMiddle_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ButtonRight_Click(object sender, EventArgs e)
        {
            Close();
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
        /// Cache for icons to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary <string, Icon> iconCache = [];

        /// <summary>
        /// Cache for banners to avoid loading them multiple times.
        /// </summary>
        private static readonly Dictionary <string, Bitmap> imageCache = [];
    }
}
