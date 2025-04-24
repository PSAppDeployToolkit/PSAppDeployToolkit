namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    public partial class RestartDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class.
        /// </summary>
        public RestartDialog() : this(default!)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public RestartDialog(RestartDialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
