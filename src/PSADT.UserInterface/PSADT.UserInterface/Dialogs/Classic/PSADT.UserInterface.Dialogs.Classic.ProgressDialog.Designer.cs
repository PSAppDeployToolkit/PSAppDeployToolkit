using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Progress dialog form.
    /// </summary>
    partial class ProgressDialog
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
            this.flowLayoutPanelDialog = new System.Windows.Forms.FlowLayoutPanel();
            this.labelMessage = new System.Windows.Forms.Label();
            this.labelDetail = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.labelMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.labelDetail);
            this.flowLayoutPanelDialog.Controls.Add(this.progressBar);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(17);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 127);
            this.flowLayoutPanelDialog.TabIndex = 2;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelMessage.AutoSize = true;
            this.labelMessage.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMessage.Location = new System.Drawing.Point(17, 17);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(416, 17);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "Installation in progress. Please wait...";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDetail
            // 
            this.labelDetail.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelDetail.AutoSize = true;
            this.labelDetail.Location = new System.Drawing.Point(17, 46);
            this.labelDetail.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.labelDetail.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelDetail.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelDetail.Name = "labelDetail";
            this.labelDetail.Size = new System.Drawing.Size(416, 15);
            this.labelDetail.TabIndex = 1;
            this.labelDetail.Text = "This window will close automatically when the installation is complete.";
            this.labelDetail.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.progressBar.Location = new System.Drawing.Point(18, 85);
            this.progressBar.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.progressBar.MarqueeAnimationSpeed = 50;
            this.progressBar.MaximumSize = new System.Drawing.Size(414, 25);
            this.progressBar.MinimumSize = new System.Drawing.Size(414, 25);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(414, 25);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 2;
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 127);
            this.Controls.Add(this.flowLayoutPanelDialog);
            this.Name = "ProgressDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelDialog, 0);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelDialog;
        private Label labelMessage;
        private Label labelDetail;
        private ProgressBar progressBar;
    }
}
