using System.Windows.Forms;

namespace PSADT.UserInterface.Interfaces.Classic
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
            this.labelDetail = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.pictureBanner = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanelBase = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanelDialog.SuspendLayout();
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
            this.flowLayoutPanelDialog.Controls.Add(this.labelDetail);
            this.flowLayoutPanelDialog.Controls.Add(this.progressBar);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(14, 11, 14, 13);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 111);
            this.flowLayoutPanelDialog.TabIndex = 2;
            this.flowLayoutPanelDialog.WrapContents = false;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMessage.AutoSize = true;
            this.labelMessage.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMessage.Location = new System.Drawing.Point(14, 11);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelMessage.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(422, 17);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "Installation in progress. Please wait...";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // labelDetail
            // 
            this.labelDetail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDetail.AutoSize = true;
            this.labelDetail.Location = new System.Drawing.Point(14, 40);
            this.labelDetail.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.labelDetail.MaximumSize = new System.Drawing.Size(422, 0);
            this.labelDetail.MinimumSize = new System.Drawing.Size(422, 0);
            this.labelDetail.Name = "labelDetail";
            this.labelDetail.Size = new System.Drawing.Size(422, 15);
            this.labelDetail.TabIndex = 1;
            this.labelDetail.Text = "This window will close automatically when the installation is complete.";
            this.labelDetail.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(14, 73);
            this.progressBar.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.progressBar.MarqueeAnimationSpeed = 50;
            this.progressBar.MaximumSize = new System.Drawing.Size(422, 25);
            this.progressBar.MinimumSize = new System.Drawing.Size(422, 25);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(422, 25);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 2;
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
            this.flowLayoutPanelBase.Size = new System.Drawing.Size(450, 111);
            this.flowLayoutPanelBase.TabIndex = 3;
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 111);
            this.Controls.Add(this.flowLayoutPanelBase);
            this.Name = "ProgressDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelBase, 0);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).EndInit();
            this.flowLayoutPanelBase.ResumeLayout(false);
            this.flowLayoutPanelBase.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelDialog;
        private Label labelMessage;
        private Label labelDetail;
        private ProgressBar progressBar;
        private PictureBox pictureBanner;
        private FlowLayoutPanel flowLayoutPanelBase;
    }
}
