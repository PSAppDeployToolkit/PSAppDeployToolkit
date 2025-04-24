namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Abortable classic dialog form.
    /// </summary>
    public partial class AbortableDialog : ClassicDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbortableDialog"/> class.
        /// </summary>
        public AbortableDialog() : this(default!)
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbortableDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        public AbortableDialog(DialogOptions options) : base(options)
        {
            InitializeComponent();
        }
    }
}
