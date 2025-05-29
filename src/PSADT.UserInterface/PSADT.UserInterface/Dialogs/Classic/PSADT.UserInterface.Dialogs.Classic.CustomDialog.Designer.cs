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
            this.pictureBanner = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanelBase = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanelIconMessage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).BeginInit();
            this.tableLayoutPanelButton.SuspendLayout();
            this.flowLayoutPanelDialog.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).BeginInit();
            this.flowLayoutPanelBase.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanelIconMessage
            // 
            this.tableLayoutPanelIconMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelIconMessage.AutoSize = true;
            this.tableLayoutPanelIconMessage.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelIconMessage.ColumnCount = 2;
            this.tableLayoutPanelIconMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 59F));
            this.tableLayoutPanelIconMessage.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelIconMessage.Controls.Add(this.labelMessage, 1, 0);
            this.tableLayoutPanelIconMessage.Controls.Add(this.pictureIcon, 0, 0);
            this.tableLayoutPanelIconMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanelIconMessage.Location = new System.Drawing.Point(11, 9);
            this.tableLayoutPanelIconMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.tableLayoutPanelIconMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.tableLayoutPanelIconMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.tableLayoutPanelIconMessage.Name = "tableLayoutPanelIconMessage";
            this.tableLayoutPanelIconMessage.RowCount = 1;
            this.tableLayoutPanelIconMessage.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelIconMessage.Size = new System.Drawing.Size(428, 50);
            this.tableLayoutPanelIconMessage.TabIndex = 3;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMessage.AutoSize = true;
            this.labelMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMessage.Location = new System.Drawing.Point(59, 1);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.labelMessage.MaximumSize = new System.Drawing.Size(366, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(366, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(366, 45);
            this.labelMessage.TabIndex = 1;
            this.labelMessage.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud" +
    " exercitation ullamco laboris...";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // pictureIcon
            // 
            this.pictureIcon.Location = new System.Drawing.Point(2, 2);
            this.pictureIcon.Margin = new System.Windows.Forms.Padding(2, 2, 0, 0);
            this.pictureIcon.MaximumSize = new System.Drawing.Size(48, 48);
            this.pictureIcon.MinimumSize = new System.Drawing.Size(48, 48);
            this.pictureIcon.Name = "pictureIcon";
            this.pictureIcon.Size = new System.Drawing.Size(48, 48);
            this.pictureIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureIcon.TabIndex = 0;
            this.pictureIcon.TabStop = false;
            this.pictureIcon.WaitOnLoad = true;
            // 
            // tableLayoutPanelButton
            // 
            this.tableLayoutPanelButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelButton.AutoSize = true;
            this.tableLayoutPanelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelButton.ColumnCount = 3;
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelButton.Controls.Add(this.buttonLeft, 0, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonMiddle, 1, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonRight, 2, 0);
            this.tableLayoutPanelButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(11, 77);
            this.tableLayoutPanelButton.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 1;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(428, 25);
            this.tableLayoutPanelButton.TabIndex = 4;
            // 
            // buttonLeft
            // 
            this.buttonLeft.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonLeft.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonLeft.Location = new System.Drawing.Point(0, 0);
            this.buttonLeft.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLeft.MaximumSize = new System.Drawing.Size(137, 25);
            this.buttonLeft.MinimumSize = new System.Drawing.Size(137, 25);
            this.buttonLeft.Name = "buttonLeft";
            this.buttonLeft.Size = new System.Drawing.Size(137, 25);
            this.buttonLeft.TabIndex = 0;
            this.buttonLeft.Text = "ButtonLeft";
            this.buttonLeft.UseVisualStyleBackColor = true;
            this.buttonLeft.Click += new System.EventHandler(this.ButtonLeft_Click);
            // 
            // buttonMiddle
            // 
            this.buttonMiddle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.buttonMiddle.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonMiddle.Location = new System.Drawing.Point(145, 0);
            this.buttonMiddle.Margin = new System.Windows.Forms.Padding(0);
            this.buttonMiddle.MaximumSize = new System.Drawing.Size(138, 25);
            this.buttonMiddle.MinimumSize = new System.Drawing.Size(138, 25);
            this.buttonMiddle.Name = "buttonMiddle";
            this.buttonMiddle.Size = new System.Drawing.Size(138, 25);
            this.buttonMiddle.TabIndex = 1;
            this.buttonMiddle.Text = "ButtonMiddle";
            this.buttonMiddle.UseVisualStyleBackColor = true;
            this.buttonMiddle.Click += new System.EventHandler(this.ButtonMiddle_Click);
            // 
            // buttonRight
            // 
            this.buttonRight.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.buttonRight.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRight.Location = new System.Drawing.Point(291, 0);
            this.buttonRight.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRight.MaximumSize = new System.Drawing.Size(137, 25);
            this.buttonRight.MinimumSize = new System.Drawing.Size(137, 25);
            this.buttonRight.Name = "buttonRight";
            this.buttonRight.Size = new System.Drawing.Size(137, 25);
            this.buttonRight.TabIndex = 2;
            this.buttonRight.Text = "ButtonRight";
            this.buttonRight.UseVisualStyleBackColor = true;
            this.buttonRight.Click += new System.EventHandler(this.ButtonRight_Click);
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelIconMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelButton);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(11, 9, 11, 10);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 112);
            this.flowLayoutPanelDialog.TabIndex = 5;
            this.flowLayoutPanelDialog.WrapContents = false;
            // 
            // pictureBanner
            // 
            this.pictureBanner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBanner.Location = new System.Drawing.Point(0, 0);
            this.pictureBanner.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBanner.Name = "pictureBanner";
            this.pictureBanner.Size = new System.Drawing.Size(450, 0);
            this.pictureBanner.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBanner.TabIndex = 0;
            this.pictureBanner.TabStop = false;
            this.pictureBanner.WaitOnLoad = true;
            // 
            // flowLayoutPanelBase
            // 
            this.flowLayoutPanelBase.AutoSize = true;
            this.flowLayoutPanelBase.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelBase.Controls.Add(this.pictureBanner);
            this.flowLayoutPanelBase.Controls.Add(this.flowLayoutPanelDialog);
            this.flowLayoutPanelBase.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelBase.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelBase.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelBase.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelBase.Name = "flowLayoutPanelBase";
            this.flowLayoutPanelBase.Size = new System.Drawing.Size(450, 112);
            this.flowLayoutPanelBase.TabIndex = 6;
            // 
            // CustomDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 112);
            this.Controls.Add(this.flowLayoutPanelBase);
            this.Name = "CustomDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelBase, 0);
            this.tableLayoutPanelIconMessage.ResumeLayout(false);
            this.tableLayoutPanelIconMessage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).EndInit();
            this.tableLayoutPanelButton.ResumeLayout(false);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).EndInit();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
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
        private PictureBox pictureBanner;
        private FlowLayoutPanel flowLayoutPanelBase;
    }
}
