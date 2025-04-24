namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Progress dialog form.
    /// </summary>
    public partial class ProgressDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class.
        /// </summary>
        public ProgressDialog() : this(default!)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialog(ProgressDialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
