namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Close applications dialog form.
    /// </summary>
    public partial class CloseAppsDialog : AbortableDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class.
        /// </summary>
        public CloseAppsDialog() : this(default!)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public CloseAppsDialog(CloseAppsDialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
