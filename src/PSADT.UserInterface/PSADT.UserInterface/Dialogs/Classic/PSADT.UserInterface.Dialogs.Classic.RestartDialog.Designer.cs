using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
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
            this.countdownFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.labelRestartMessage = new System.Windows.Forms.Label();
            this.labelTimeRemaining = new System.Windows.Forms.Label();
            this.labelCountdown = new System.Windows.Forms.Label();
            this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonMinimize = new System.Windows.Forms.Button();
            this.buttonRestartNow = new System.Windows.Forms.Button();
            this.dialogFlowLayoutPanel.SuspendLayout();
            this.countdownFlowLayoutPanel.SuspendLayout();
            this.buttonTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // dialogFlowLayoutPanel
            // 
            this.dialogFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.dialogFlowLayoutPanel.AutoSize = true;
            this.dialogFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.dialogFlowLayoutPanel.Controls.Add(this.labelMessage);
            this.dialogFlowLayoutPanel.Controls.Add(this.countdownFlowLayoutPanel);
            this.dialogFlowLayoutPanel.Controls.Add(this.buttonTableLayoutPanel);
            this.dialogFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.dialogFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.dialogFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.dialogFlowLayoutPanel.MaximumSize = new System.Drawing.Size(450, 0);
            this.dialogFlowLayoutPanel.MinimumSize = new System.Drawing.Size(450, 0);
            this.dialogFlowLayoutPanel.Name = "dialogFlowLayoutPanel";
            this.dialogFlowLayoutPanel.Padding = new System.Windows.Forms.Padding(23);
            this.dialogFlowLayoutPanel.Size = new System.Drawing.Size(450, 223);
            this.dialogFlowLayoutPanel.TabIndex = 2;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelMessage.AutoSize = true;
            this.labelMessage.Location = new System.Drawing.Point(23, 23);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelMessage.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelMessage.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.Size = new System.Drawing.Size(404, 30);
            this.labelMessage.TabIndex = 0;
            this.labelMessage.Text = "In order for the installation to complete, you must restart your computer. Please save your work and restart within the alloted time.";
            this.labelMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // countdownFlowLayoutPanel
            // 
            this.countdownFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.countdownFlowLayoutPanel.AutoSize = true;
            this.countdownFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.countdownFlowLayoutPanel.Controls.Add(this.labelRestartMessage);
            this.countdownFlowLayoutPanel.Controls.Add(this.labelTimeRemaining);
            this.countdownFlowLayoutPanel.Controls.Add(this.labelCountdown);
            this.countdownFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.countdownFlowLayoutPanel.Location = new System.Drawing.Point(23, 65);
            this.countdownFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.countdownFlowLayoutPanel.MinimumSize = new System.Drawing.Size(404, 0);
            this.countdownFlowLayoutPanel.Name = "countdownFlowLayoutPanel";
            this.countdownFlowLayoutPanel.Size = new System.Drawing.Size(404, 80);
            this.countdownFlowLayoutPanel.TabIndex = 4;
            // 
            // labelRestartMessage
            // 
            this.labelRestartMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelRestartMessage.AutoSize = true;
            this.labelRestartMessage.Location = new System.Drawing.Point(0, 0);
            this.labelRestartMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelRestartMessage.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelRestartMessage.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelRestartMessage.Name = "labelRestartMessage";
            this.labelRestartMessage.Size = new System.Drawing.Size(404, 15);
            this.labelRestartMessage.TabIndex = 3;
            this.labelRestartMessage.Text = "Your computer will be automatically restarted at the end of the countdown.";
            this.labelRestartMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelTimeRemaining
            // 
            this.labelTimeRemaining.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelTimeRemaining.AutoSize = true;
            this.labelTimeRemaining.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTimeRemaining.Location = new System.Drawing.Point(0, 27);
            this.labelTimeRemaining.Margin = new System.Windows.Forms.Padding(0);
            this.labelTimeRemaining.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelTimeRemaining.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelTimeRemaining.Name = "labelTimeRemaining";
            this.labelTimeRemaining.Size = new System.Drawing.Size(404, 21);
            this.labelTimeRemaining.TabIndex = 1;
            this.labelTimeRemaining.Text = "Time remaining:";
            this.labelTimeRemaining.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCountdown
            // 
            this.labelCountdown.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelCountdown.AutoSize = true;
            this.labelCountdown.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdown.Location = new System.Drawing.Point(0, 48);
            this.labelCountdown.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdown.MaximumSize = new System.Drawing.Size(404, 0);
            this.labelCountdown.MinimumSize = new System.Drawing.Size(404, 0);
            this.labelCountdown.Name = "labelCountdown";
            this.labelCountdown.Size = new System.Drawing.Size(404, 32);
            this.labelCountdown.TabIndex = 2;
            this.labelCountdown.Text = "1:23:45";
            this.labelCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonTableLayoutPanel
            // 
            this.buttonTableLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.buttonTableLayoutPanel.AutoSize = true;
            this.buttonTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonTableLayoutPanel.ColumnCount = 2;
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonTableLayoutPanel.Controls.Add(this.buttonMinimize, 1, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.buttonRestartNow, 0, 0);
            this.buttonTableLayoutPanel.Location = new System.Drawing.Point(23, 175);
            this.buttonTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 24, 0, 0);
            this.buttonTableLayoutPanel.MaximumSize = new System.Drawing.Size(404, 0);
            this.buttonTableLayoutPanel.MinimumSize = new System.Drawing.Size(404, 0);
            this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
            this.buttonTableLayoutPanel.RowCount = 1;
            this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonTableLayoutPanel.Size = new System.Drawing.Size(404, 25);
            this.buttonTableLayoutPanel.TabIndex = 3;
            // 
            // buttonMinimize
            // 
            this.buttonMinimize.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.buttonMinimize.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonMinimize.Location = new System.Drawing.Point(209, 0);
            this.buttonMinimize.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonMinimize.MaximumSize = new System.Drawing.Size(195, 25);
            this.buttonMinimize.MinimumSize = new System.Drawing.Size(195, 25);
            this.buttonMinimize.Name = "buttonMinimize";
            this.buttonMinimize.Size = new System.Drawing.Size(195, 25);
            this.buttonMinimize.TabIndex = 1;
            this.buttonMinimize.Text = "Minimize";
            this.buttonMinimize.UseVisualStyleBackColor = true;
            // 
            // buttonRestartNow
            // 
            this.buttonRestartNow.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.buttonRestartNow.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonRestartNow.Location = new System.Drawing.Point(0, 0);
            this.buttonRestartNow.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRestartNow.MaximumSize = new System.Drawing.Size(195, 25);
            this.buttonRestartNow.MinimumSize = new System.Drawing.Size(195, 25);
            this.buttonRestartNow.Name = "buttonRestartNow";
            this.buttonRestartNow.Size = new System.Drawing.Size(195, 25);
            this.buttonRestartNow.TabIndex = 0;
            this.buttonRestartNow.Text = "Restart Now";
            this.buttonRestartNow.UseVisualStyleBackColor = true;
            // 
            // RestartDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 0);
            this.Controls.Add(this.dialogFlowLayoutPanel);
            this.MaximumSize = new System.Drawing.Size(466, 0);
            this.MinimumSize = new System.Drawing.Size(466, 0);
            this.Name = "RestartDialog";
            this.Text = "RestartDialog";
            this.Controls.SetChildIndex(this.dialogFlowLayoutPanel, 0);
            this.dialogFlowLayoutPanel.ResumeLayout(false);
            this.dialogFlowLayoutPanel.PerformLayout();
            this.countdownFlowLayoutPanel.ResumeLayout(false);
            this.countdownFlowLayoutPanel.PerformLayout();
            this.buttonTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel dialogFlowLayoutPanel;
        private Label labelMessage;
        private TableLayoutPanel buttonTableLayoutPanel;
        private Button buttonMinimize;
        private Button buttonRestartNow;
        private FlowLayoutPanel countdownFlowLayoutPanel;
        private Label labelRestartMessage;
        private Label labelTimeRemaining;
        private Label labelCountdown;
    }
}
