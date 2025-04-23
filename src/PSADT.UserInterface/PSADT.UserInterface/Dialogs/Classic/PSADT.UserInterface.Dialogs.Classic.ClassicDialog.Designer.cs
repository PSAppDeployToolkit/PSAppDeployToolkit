using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    partial class ClassicDialog
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
            this.baseFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBanner = new System.Windows.Forms.PictureBox();
            this.buttonDefault = new System.Windows.Forms.Button();
            this.baseFlowLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).BeginInit();
            this.SuspendLayout();
            // 
            // baseFlowLayoutPanel
            // 
            this.baseFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.baseFlowLayoutPanel.AutoSize = true;
            this.baseFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.baseFlowLayoutPanel.Controls.Add(this.pictureBanner);
            this.baseFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.baseFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.baseFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.baseFlowLayoutPanel.Name = "baseFlowLayoutPanel";
            this.baseFlowLayoutPanel.TabIndex = 0;
            // 
            // pictureBanner
            // 
            this.pictureBanner.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.pictureBanner.Location = new System.Drawing.Point(0, 0);
            this.pictureBanner.Margin = new System.Windows.Forms.Padding(0);
            this.pictureBanner.Name = "pictureBanner";
            this.pictureBanner.Size = new System.Drawing.Size(450, 0);
            this.pictureBanner.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBanner.TabIndex = 0;
            this.pictureBanner.TabStop = false;
            this.pictureBanner.WaitOnLoad = true;
            // 
            // buttonDefault
            // 
            this.buttonDefault.BackColor = System.Drawing.Color.Transparent;
            this.buttonDefault.Enabled = false;
            this.buttonDefault.FlatAppearance.BorderSize = 0;
            this.buttonDefault.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonDefault.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonDefault.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonDefault.ForeColor = System.Drawing.Color.Transparent;
            this.buttonDefault.Location = new System.Drawing.Point(0, 0);
            this.buttonDefault.Margin = new System.Windows.Forms.Padding(0);
            this.buttonDefault.Name = "buttonDefault";
            this.buttonDefault.Size = new System.Drawing.Size(0, 0);
            this.buttonDefault.TabIndex = 1;
            this.buttonDefault.TabStop = false;
            this.buttonDefault.UseVisualStyleBackColor = true;
            // 
            // ClassicDialog
            // 
            this.AcceptButton = this.buttonDefault;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(450, 0);
            this.Controls.Add(this.buttonDefault);
            this.Controls.Add(this.baseFlowLayoutPanel);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(466, 0);
            this.MinimizeBox = false;
            this.Name = "ClassicDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ClassicDialog";
            this.TopMost = true;
            this.baseFlowLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBanner)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private PictureBox pictureBanner;
        private Button buttonDefault;
        protected FlowLayoutPanel baseFlowLayoutPanel;
    }
}
