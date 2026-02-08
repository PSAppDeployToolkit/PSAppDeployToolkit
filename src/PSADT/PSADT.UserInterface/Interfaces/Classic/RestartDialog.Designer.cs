using System.Windows.Forms;

namespace PSADT.UserInterface.Interfaces.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    partial class RestartDialog
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
            if (disposing && (components is not null))
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
            this.flowLayoutPanelDialog = new System.Windows.Forms.FlowLayoutPanel();
            this.labelMessage = new System.Windows.Forms.Label();
            this.labelCustomMessage = new System.Windows.Forms.Label();
            this.flowLayoutPanelCountdown = new System.Windows.Forms.FlowLayoutPanel();
            this.labelRestartMessage = new System.Windows.Forms.Label();
            this.labelTimeRemaining = new System.Windows.Forms.Label();
            this.labelCountdown = new System.Windows.Forms.Label();
            this.tableLayoutPanelButton = new System.Windows.Forms.TableLayoutPanel();
            this.buttonMinimize = new System.Windows.Forms.Button();
            this.buttonRestartNow = new System.Windows.Forms.Button();
            this.pictureBanner = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanelBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.flowLayoutPanelCountdown.SuspendLayout();
            this.tableLayoutPanelButton.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).BeginInit();
            this.flowLayoutPanelBase.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.labelMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.labelCustomMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelCountdown);
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelButton);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(14, 11, 14, 13);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 230);
            this.flowLayoutPanelDialog.TabIndex = 2;
            this.flowLayoutPanelDialog.WrapContents = false;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMessage.AutoSize = true;
            this.labelMessage.Location = new System.Drawing.Point(14, 11);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.labelMessage.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(422, 15);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "In order for the installation to complete, you must restart your computer.";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCustomMessage
            // 
            this.labelCustomMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCustomMessage.AutoSize = true;
            this.labelCustomMessage.Location = new System.Drawing.Point(14, 38);
            this.labelCustomMessage.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.labelCustomMessage.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelCustomMessage.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelCustomMessage.Name = "labelCustomMessage";
            this.labelCustomMessage.Size = new System.Drawing.Size(422, 30);
            this.labelCustomMessage.TabIndex = 5;
            this.labelCustomMessage.Text = "This is an optional custom text message. It can be used to display specific infor" +
    "mation to the user before they reboot their device.";
            this.labelCustomMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelCountdown
            // 
            this.flowLayoutPanelCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelCountdown.AutoSize = true;
            this.flowLayoutPanelCountdown.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelCountdown.Controls.Add(this.labelRestartMessage);
            this.flowLayoutPanelCountdown.Controls.Add(this.labelTimeRemaining);
            this.flowLayoutPanelCountdown.Controls.Add(this.labelCountdown);
            this.flowLayoutPanelCountdown.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCountdown.Location = new System.Drawing.Point(14, 80);
            this.flowLayoutPanelCountdown.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelCountdown.MaximumSize = new System.Drawing.Size(422, 0);
            this.flowLayoutPanelCountdown.MinimumSize = new System.Drawing.Size(422, 0);
            this.flowLayoutPanelCountdown.Name = "flowLayoutPanelCountdown";
            this.flowLayoutPanelCountdown.Size = new System.Drawing.Size(422, 94);
            this.flowLayoutPanelCountdown.TabIndex = 4;
            this.flowLayoutPanelCountdown.WrapContents = false;
            // 
            // labelRestartMessage
            // 
            this.labelRestartMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRestartMessage.AutoSize = true;
            this.labelRestartMessage.Location = new System.Drawing.Point(0, 0);
            this.labelRestartMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelRestartMessage.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelRestartMessage.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelRestartMessage.Name = "labelRestartMessage";
            this.labelRestartMessage.Size = new System.Drawing.Size(422, 30);
            this.labelRestartMessage.TabIndex = 3;
            this.labelRestartMessage.Text = "Please save your work and restart within the alloted time. Your computer will be " +
    "automatically restarted at the end of the countdown.";
            this.labelRestartMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelTimeRemaining
            // 
            this.labelTimeRemaining.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelTimeRemaining.AutoSize = true;
            this.labelTimeRemaining.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTimeRemaining.Location = new System.Drawing.Point(0, 42);
            this.labelTimeRemaining.Margin = new System.Windows.Forms.Padding(0);
            this.labelTimeRemaining.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelTimeRemaining.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelTimeRemaining.Name = "labelTimeRemaining";
            this.labelTimeRemaining.Size = new System.Drawing.Size(422, 20);
            this.labelTimeRemaining.TabIndex = 1;
            this.labelTimeRemaining.Text = "Time remaining:";
            this.labelTimeRemaining.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCountdown
            // 
            this.labelCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCountdown.AutoSize = true;
            this.labelCountdown.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdown.Location = new System.Drawing.Point(0, 62);
            this.labelCountdown.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdown.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelCountdown.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelCountdown.Name = "labelCountdown";
            this.labelCountdown.Size = new System.Drawing.Size(422, 32);
            this.labelCountdown.TabIndex = 2;
            this.labelCountdown.Text = "1:23:45";
            this.labelCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanelButton
            // 
            this.tableLayoutPanelButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanelButton.AutoSize = true;
            this.tableLayoutPanelButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelButton.ColumnCount = 2;
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelButton.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelButton.Controls.Add(this.buttonMinimize, 1, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonRestartNow, 0, 0);
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(14, 192);
            this.tableLayoutPanelButton.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 1;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(422, 25);
            this.tableLayoutPanelButton.TabIndex = 3;
            // 
            // buttonMinimize
            // 
            this.buttonMinimize.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonMinimize.Location = new System.Drawing.Point(217, 0);
            this.buttonMinimize.Margin = new System.Windows.Forms.Padding(0);
            this.buttonMinimize.MaximumSize = new System.Drawing.Size(205, 25);
            this.buttonMinimize.MinimumSize = new System.Drawing.Size(204, 25);
            this.buttonMinimize.Name = "buttonMinimize";
            this.buttonMinimize.Size = new System.Drawing.Size(205, 25);
            this.buttonMinimize.TabIndex = 1;
            this.buttonMinimize.Text = "Minimize";
            this.buttonMinimize.UseVisualStyleBackColor = true;
            this.buttonMinimize.Click += new System.EventHandler(this.ButtonRight_Click);
            // 
            // buttonRestartNow
            // 
            this.buttonRestartNow.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonRestartNow.Location = new System.Drawing.Point(0, 0);
            this.buttonRestartNow.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRestartNow.MaximumSize = new System.Drawing.Size(205, 25);
            this.buttonRestartNow.MinimumSize = new System.Drawing.Size(206, 25);
            this.buttonRestartNow.Name = "buttonRestartNow";
            this.buttonRestartNow.Size = new System.Drawing.Size(206, 25);
            this.buttonRestartNow.TabIndex = 0;
            this.buttonRestartNow.Text = "Restart Now";
            this.buttonRestartNow.UseVisualStyleBackColor = true;
            this.buttonRestartNow.Click += new System.EventHandler(this.ButtonLeft_Click);
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
            this.flowLayoutPanelBase.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelBase.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelBase.Name = "flowLayoutPanelBase";
            this.flowLayoutPanelBase.Size = new System.Drawing.Size(450, 230);
            this.flowLayoutPanelBase.TabIndex = 3;
            // 
            // RestartDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 230);
            this.Controls.Add(this.flowLayoutPanelBase);
            this.Name = "RestartDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelBase, 0);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.flowLayoutPanelCountdown.ResumeLayout(false);
            this.flowLayoutPanelCountdown.PerformLayout();
            this.tableLayoutPanelButton.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).EndInit();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelDialog;
        private Label labelMessage;
        private TableLayoutPanel tableLayoutPanelButton;
        private Button buttonMinimize;
        private Button buttonRestartNow;
        private FlowLayoutPanel flowLayoutPanelCountdown;
        private Label labelRestartMessage;
        private Label labelTimeRemaining;
        private Label labelCountdown;
        private Label labelCustomMessage;
        private PictureBox pictureBanner;
        private FlowLayoutPanel flowLayoutPanelBase;
    }
}
