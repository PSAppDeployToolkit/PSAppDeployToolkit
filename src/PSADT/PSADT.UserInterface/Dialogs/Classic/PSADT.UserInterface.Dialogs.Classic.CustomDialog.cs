using System;
using System.ComponentModel;
using System.Drawing;
using PSADT.UserInterface.DialogOptions;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Custom classic dialog form.
    /// </summary>
    internal partial class CustomDialog : ClassicDialog, IModalDialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialog"/> class.
        /// </summary>
        internal CustomDialog() : this(default!)
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Runtime)
            {
                throw new InvalidOperationException("This constructor cannot be used in runtime mode.");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialog"/> class with the specified options.
        /// </summary>
        /// <param name="options"></param>
        internal CustomDialog(CustomDialogOptions options) : base(options)
        {
            // Initialise the form and reset the control order.
            // The designer tries to add its controls ahead of the base's.
            InitializeComponent();
            this.SuspendLayout();
            this.flowLayoutPanelBase.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();

            // Apply options to the form if we have any (i.e. not in the designer).
            if (null != options)
            {
                // Set up the picturebox.
                SetPictureBox(this.pictureBanner, options);

                // Set up the buttons.
                if (options.ButtonLeftText != null)
                {
                    this.buttonLeft.Text = StripFormattingTags(options.ButtonLeftText);
                    this.buttonLeft.Visible = true;
                }
                else
                {
                    this.tableLayoutPanelButton.Controls.Remove(buttonLeft);
                }
                if (options.ButtonMiddleText != null)
                {
                    this.buttonMiddle.Text = StripFormattingTags(options.ButtonMiddleText);
                    this.buttonMiddle.Visible = true;
                }
                else
                {
                    this.tableLayoutPanelButton.Controls.Remove(buttonMiddle);
                }
                if (options.ButtonRightText != null)
                {
                    this.buttonRight.Text = StripFormattingTags(options.ButtonRightText);
                    this.buttonRight.Visible = true;
                }
                else
                {
                    this.tableLayoutPanelButton.Controls.Remove(buttonRight);
                }

                // Set up the icon.
                if (null == options.Icon)
                {
                    this.tableLayoutPanelIconMessage.SuspendLayout();
                    this.tableLayoutPanelIconMessage.Controls.Remove(this.pictureIcon);
                    this.tableLayoutPanelIconMessage.SetColumn(this.labelMessage, 0);
                    this.tableLayoutPanelIconMessage.SetColumnSpan(this.labelMessage, 2);
                    var extraWidth = (int)this.tableLayoutPanelIconMessage.ColumnStyles[0].Width;
                    this.labelMessage.MinimumSize = new Size(this.labelMessage.MinimumSize.Width + extraWidth, this.labelMessage.MinimumSize.Height);
                    this.labelMessage.MaximumSize = new Size(this.labelMessage.MaximumSize.Width + extraWidth, this.labelMessage.MaximumSize.Height);
                    this.tableLayoutPanelIconMessage.ResumeLayout();
                }
                else
                {
                    this.pictureIcon.Image = SystemIcons.SystemIconLookupTable[options.Icon.Value];
                }

                // Set up the message.
                if ((null != options.MessageAlignment) && Enum.TryParse<ContentAlignment>($"Top{options.MessageAlignment}", out var alignment))
                {
                    this.labelMessage.TextAlign = alignment;
                }
                this.labelMessage.Text = StripFormattingTags(options.MessageText);
            }

            // Resume the dialog now that we've applied any options.
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
            this.ResumeLayout();
            this.PerformLayout();
        }

        /// <summary>
        /// Handles the click event of the left button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonLeft_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = this.buttonLeft.Text;
            base.ButtonLeft_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the middle button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonMiddle_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = this.buttonMiddle.Text;
            base.ButtonMiddle_Click(sender, e);
        }

        /// <summary>
        /// Handles the click event of the right button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ButtonRight_Click(object sender, EventArgs e)
        {
            // Set the result and call base method to handle window closure.
            DialogResult = this.buttonRight.Text;
            base.ButtonRight_Click(sender, e);
        }
    }
}
