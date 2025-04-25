using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Custom dialog form.
    /// </summary>
    partial class CustomDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanelIconMessage = new System.Windows.Forms.TableLayoutPanel();
            this.labelMessage = new System.Windows.Forms.Label();
            this.pictureIcon = new System.Windows.Forms.PictureBox();
            this.tableLayoutPanelButton = new System.Windows.Forms.TableLayoutPanel();
            this.buttonLeft = new System.Windows.Forms.Button();
            this.buttonMiddle = new System.Windows.Forms.Button();
            this.buttonRight = new System.Windows.Forms.Button();
            this.flowLayoutPanelDialog = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanelIconMessage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).BeginInit();
            this.tableLayoutPanelButton.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelIconMessage
            // 
            this.tableLayoutPanelIconMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.tableLayoutPanelIconMessage.AutoSize = true;
            this.tableLayoutPanelIconMessage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelIconMessage.ColumnCount = 2;
            this.tableLayoutPanelIconMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 67F));
            this.tableLayoutPanelIconMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelIconMessage.Controls.Add(this.labelMessage, 1, 0);
            this.tableLayoutPanelIconMessage.Controls.Add(this.pictureIcon, 0, 0);
            this.tableLayoutPanelIconMessage.Location = new System.Drawing.Point(17, 17);
            this.tableLayoutPanelIconMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.tableLayoutPanelIconMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.tableLayoutPanelIconMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.tableLayoutPanelIconMessage.Name = "tableLayoutPanelIconMessage";
            this.tableLayoutPanelIconMessage.RowCount = 1;
            this.tableLayoutPanelIconMessage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelIconMessage.Size = new System.Drawing.Size(416, 60);
            this.tableLayoutPanelIconMessage.TabIndex = 3;
            // 
            // labelMessage
            // 
            this.labelMessage.AutoSize = true;
            this.labelMessage.Location = new System.Drawing.Point(67, 0);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0);
            this.labelMessage.MaximumSize = new System.Drawing.Size(349, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(349, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(349, 60);
            this.labelMessage.TabIndex = 1;
            this.labelMessage.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud" +
    " exercitation ullamco laboris...";
            // 
            // pictureIcon
            // 
            this.pictureIcon.Location = new System.Drawing.Point(3, 1);
            this.pictureIcon.Margin = new System.Windows.Forms.Padding(3, 1, 0, 0);
            this.pictureIcon.MaximumSize = new System.Drawing.Size(48, 48);
            this.pictureIcon.MinimumSize = new System.Drawing.Size(48, 48);
            this.pictureIcon.Name = "pictureIcon";
            this.pictureIcon.Size = new System.Drawing.Size(48, 48);
            this.pictureIcon.TabIndex = 0;
            this.pictureIcon.TabStop = false;
            this.pictureIcon.WaitOnLoad = true;
            // 
            // tableLayoutPanelButton
            // 
            this.tableLayoutPanelButton.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.tableLayoutPanelButton.AutoSize = true;
            this.tableLayoutPanelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelButton.ColumnCount = 3;
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanelButton.Controls.Add(this.buttonLeft, 0, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonMiddle, 1, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonRight, 2, 0);
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(18, 101);
            this.tableLayoutPanelButton.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.tableLayoutPanelButton.MaximumSize = new System.Drawing.Size(414, 0);
            this.tableLayoutPanelButton.MinimumSize = new System.Drawing.Size(414, 0);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 1;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(414, 25);
            this.tableLayoutPanelButton.TabIndex = 4;
            // 
            // buttonLeft
            // 
            this.buttonLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonLeft.Location = new System.Drawing.Point(0, 0);
            this.buttonLeft.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLeft.MaximumSize = new System.Drawing.Size(133, 25);
            this.buttonLeft.MinimumSize = new System.Drawing.Size(133, 25);
            this.buttonLeft.Name = "buttonLeft";
            this.buttonLeft.Size = new System.Drawing.Size(133, 25);
            this.buttonLeft.TabIndex = 0;
            this.buttonLeft.Text = "ButtonLeft";
            this.buttonLeft.UseVisualStyleBackColor = true;
            this.buttonLeft.Click += new System.EventHandler(this.ButtonLeft_Click);
            // 
            // buttonMiddle
            // 
            this.buttonMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonMiddle.Location = new System.Drawing.Point(140, 0);
            this.buttonMiddle.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.buttonMiddle.MaximumSize = new System.Drawing.Size(133, 25);
            this.buttonMiddle.MinimumSize = new System.Drawing.Size(133, 25);
            this.buttonMiddle.Name = "buttonMiddle";
            this.buttonMiddle.Size = new System.Drawing.Size(133, 25);
            this.buttonMiddle.TabIndex = 1;
            this.buttonMiddle.Text = "ButtonMiddle";
            this.buttonMiddle.UseVisualStyleBackColor = true;
            this.buttonMiddle.Click += new System.EventHandler(this.ButtonMiddle_Click);
            // 
            // buttonRight
            // 
            this.buttonRight.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.buttonRight.Location = new System.Drawing.Point(281, 0);
            this.buttonRight.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRight.MaximumSize = new System.Drawing.Size(133, 25);
            this.buttonRight.MinimumSize = new System.Drawing.Size(133, 25);
            this.buttonRight.Name = "buttonRight";
            this.buttonRight.Size = new System.Drawing.Size(133, 25);
            this.buttonRight.TabIndex = 2;
            this.buttonRight.Text = "ButtonRight";
            this.buttonRight.UseVisualStyleBackColor = true;
            this.buttonRight.Click += new System.EventHandler(this.ButtonRight_Click);
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelIconMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelButton);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(17);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 143);
            this.flowLayoutPanelDialog.TabIndex = 5;
            // 
            // CustomDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 143);
            this.Controls.Add(this.flowLayoutPanelDialog);
            this.Name = "CustomDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelDialog, 0);
            this.tableLayoutPanelIconMessage.ResumeLayout(false);
            this.tableLayoutPanelIconMessage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).EndInit();
            this.tableLayoutPanelButton.ResumeLayout(false);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TableLayoutPanel tableLayoutPanelIconMessage;
        private PictureBox pictureIcon;
        private TableLayoutPanel tableLayoutPanelButton;
        private Label labelMessage;
        private Button buttonLeft;
        private Button buttonRight;
        private Button buttonMiddle;
        private FlowLayoutPanel flowLayoutPanelDialog;
    }
}
