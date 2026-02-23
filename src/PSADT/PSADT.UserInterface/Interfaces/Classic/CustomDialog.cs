using System;
using System.ComponentModel;
using System.Drawing;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Custom classic dialog form.
    /// </summary>
    internal partial class CustomDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialog"/> class.
        /// </summary>
        internal CustomDialog() : this(null!, null!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the CustomDialog class using the specified options and sets the default dialog
        /// result to indicate a timeout.
        /// </summary>
        /// <remarks>This constructor sets the dialog's initial result to "Timeout". Use this overload
        /// when you want to create a dialog with default timeout behavior.</remarks>
        /// <param name="options">The options that configure the behavior and appearance of the dialog. Cannot be null.</param>
        internal CustomDialog(CustomDialogOptions options) : this(options, new CustomDialogResult("Timeout"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the CustomDialog class using the specified dialog options and result object.
        /// </summary>
        /// <remarks>This constructor applies the provided options to customize the dialog's controls,
        /// such as button visibility, message alignment, and icon display. Ensure that the options parameter is
        /// properly configured before passing it to this constructor.</remarks>
        /// <param name="options">The options that configure the appearance and behavior of the dialog. Cannot be null.</param>
        /// <param name="dialogResult">An object representing the result of the dialog interaction, indicating the user's choice or action.</param>
        private protected CustomDialog(CustomDialogOptions options, CustomDialogResult dialogResult) : base(options, dialogResult)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            SuspendLayout();
            flowLayoutPanelBase.SuspendLayout();
            flowLayoutPanelDialog.SuspendLayout();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (options is not null)
            {
                // Set up the picturebox.
                SetPictureBox(pictureBanner, options);

                // Set up the buttons.
                if (options.ButtonLeftText is not null)
                {
                    buttonLeft.Text = StripFormattingTags(options.ButtonLeftText);
                    buttonLeft.Visible = true;
                }
                else
                {
                    tableLayoutPanelButton.Controls.Remove(buttonLeft);
                }
                if (options.ButtonMiddleText is not null)
                {
                    buttonMiddle.Text = StripFormattingTags(options.ButtonMiddleText);
                    buttonMiddle.Visible = true;
                }
                else
                {
                    tableLayoutPanelButton.Controls.Remove(buttonMiddle);
                }
                if (options.ButtonRightText is not null)
                {
                    buttonRight.Text = StripFormattingTags(options.ButtonRightText);
                    buttonRight.Visible = true;
                }
                else
                {
                    tableLayoutPanelButton.Controls.Remove(buttonRight);
                }

                // Set up the icon.
                if (options.Icon is null)
                {
                    tableLayoutPanelIconMessage.SuspendLayout();
                    tableLayoutPanelIconMessage.Controls.Remove(pictureIcon);
                    tableLayoutPanelIconMessage.SetColumn(labelMessage, 0);
                    tableLayoutPanelIconMessage.SetColumnSpan(labelMessage, 2);
                    int extraWidth = (int)tableLayoutPanelIconMessage.ColumnStyles[0].Width;
                    labelMessage.MinimumSize = new(labelMessage.MinimumSize.Width + extraWidth, labelMessage.MinimumSize.Height);
                    labelMessage.MaximumSize = new(labelMessage.MaximumSize.Width + extraWidth, labelMessage.MaximumSize.Height);
                    tableLayoutPanelIconMessage.ResumeLayout();
                }
                else
                {
                    pictureIcon.Image = SystemIcons.Get(options.Icon.Value);
                }

                // Set up the message.
                if ((options.MessageAlignment is not null) && Enum.TryParse($"Top{options.MessageAlignment}", out ContentAlignment alignment))
                {
                    labelMessage.TextAlign = alignment;
                }
                labelMessage.Text = StripFormattingTags(options.MessageText);
            }

            // Resume the dialog now that we've applied any options.
            flowLayoutPanelDialog.ResumeLayout(false);
            flowLayoutPanelDialog.PerformLayout();
            flowLayoutPanelBase.ResumeLayout(false);
            flowLayoutPanelBase.PerformLayout();
            ResumeLayout();
            PerformLayout();
        }

        /// <summary>
        /// Handles the Click event for the left button in the custom dialog, setting the dialog result and initiating
        /// dialog closure.
        /// </summary>
        /// <remarks>This method sets the dialog result to a value based on the left button's text before
        /// invoking the base implementation to close the dialog. Override this method to customize the behavior when
        /// the left button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the left button that was clicked.</param>
        /// <param name="e">An EventArgs object that contains the event data.</param>
        private protected override void ButtonLeft_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new CustomDialogResult(buttonLeft.Text);
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the middle button, setting the dialog result based on the button's text and
        /// invoking the base implementation to process the dialog closure.
        /// </summary>
        /// <remarks>This method assigns a custom dialog result derived from the middle button's text
        /// before delegating to the base class handler. Override this method to customize the behavior when the middle
        /// button is clicked.</remarks>
        /// <param name="sender">The source of the event, typically the middle button that was clicked.</param>
        /// <param name="e">An object that contains the event data associated with the click event.</param>
        private protected override void ButtonMiddle_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new CustomDialogResult(buttonMiddle.Text);
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event for the right button, setting the dialog result and invoking the base class method
        /// to manage window closure.
        /// </summary>
        /// <remarks>This method sets the dialog result to a custom value based on the button's text
        /// before calling the base class implementation to ensure proper handling of the dialog's closure.</remarks>
        /// <param name="sender">The source of the event, typically the button that was clicked.</param>
        /// <param name="e">The event data associated with the click event.</param>
        private protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = new CustomDialogResult(buttonRight.Text);
            base.ButtonRight_Click(sender, e);
        }
    }
}
