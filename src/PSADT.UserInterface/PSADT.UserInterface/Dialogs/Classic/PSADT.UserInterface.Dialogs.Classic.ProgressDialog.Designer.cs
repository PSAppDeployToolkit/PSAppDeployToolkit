using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
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
            this.dialogFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.labelMessage = new System.Windows.Forms.Label();
            this.labelDetail = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.dialogFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // dialogFlowLayoutPanel
            // 
            this.dialogFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.dialogFlowLayoutPanel.AutoSize = true;
            this.dialogFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.dialogFlowLayoutPanel.Controls.Add(this.labelMessage);
            this.dialogFlowLayoutPanel.Controls.Add(this.labelDetail);
            this.dialogFlowLayoutPanel.Controls.Add(this.progressBar);
            this.dialogFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.dialogFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.dialogFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.dialogFlowLayoutPanel.MaximumSize = new System.Drawing.Size(450, 0);
            this.dialogFlowLayoutPanel.MinimumSize = new System.Drawing.Size(450, 0);
            this.dialogFlowLayoutPanel.Name = "dialogFlowLayoutPanel";
            this.dialogFlowLayoutPanel.Padding = new System.Windows.Forms.Padding(23);
            this.dialogFlowLayoutPanel.Size = new System.Drawing.Size(450, 149);
            this.dialogFlowLayoutPanel.TabIndex = 2;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelMessage.AutoSize = true;
            this.labelMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMessage.Location = new System.Drawing.Point(23, 23);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelMessage.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(404, 15);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "Installation in progress. Please wait...";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDetail
            // 
            this.labelDetail.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelDetail.AutoSize = true;
            this.labelDetail.Location = new System.Drawing.Point(23, 50);
            this.labelDetail.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelDetail.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelDetail.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelDetail.Name = "labelDetail";
            this.labelDetail.Size = new System.Drawing.Size(404, 15);
            this.labelDetail.TabIndex = 1;
            this.labelDetail.Text = "This window will close automatically when the installation is complete.";
            this.labelDetail.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.progressBar.Location = new System.Drawing.Point(24, 101);
            this.progressBar.Margin = new System.Windows.Forms.Padding(0, 24, 0, 0);
            this.progressBar.MaximumSize = new System.Drawing.Size(402, 25);
            this.progressBar.MinimumSize = new System.Drawing.Size(402, 25);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(402, 25);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 2;
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 0);
            this.Controls.Add(this.dialogFlowLayoutPanel);
            this.MaximumSize = new System.Drawing.Size(466, 0);
            this.MinimumSize = new System.Drawing.Size(466, 0);
            this.Name = "ProgressDialog";
            this.Text = "ProgressDialog";
            this.Controls.SetChildIndex(this.dialogFlowLayoutPanel, 0);
            this.dialogFlowLayoutPanel.ResumeLayout(false);
            this.dialogFlowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel dialogFlowLayoutPanel;
        private Label labelMessage;
        private Label labelDetail;
        private ProgressBar progressBar;
    }
}
