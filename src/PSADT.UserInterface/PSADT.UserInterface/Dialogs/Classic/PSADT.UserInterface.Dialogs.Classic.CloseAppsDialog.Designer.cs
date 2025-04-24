using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
    /// <summary>
    /// Restart dialog form.
    /// </summary>
    partial class CloseAppsDialog
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
            this.labelWelcomeMessage = new System.Windows.Forms.Label();
            this.labelAppName = new System.Windows.Forms.Label();
            this.labelCustomMessage = new System.Windows.Forms.Label();
            this.flowLayoutPanelCloseApps = new System.Windows.Forms.FlowLayoutPanel();
            this.labelCloseProcessesMessage = new System.Windows.Forms.Label();
            this.listBoxCloseProcesses = new System.Windows.Forms.ListBox();
            this.flowLayoutPanelDeferral = new System.Windows.Forms.FlowLayoutPanel();
            this.labelDeferralExpiryMessage = new System.Windows.Forms.Label();
            this.labelDeferDeadline = new System.Windows.Forms.Label();
            this.labelDeferWarningMessage = new System.Windows.Forms.Label();
            this.flowLayoutPanelCountdown = new System.Windows.Forms.FlowLayoutPanel();
            this.labelCountdownMessage = new System.Windows.Forms.Label();
            this.labelCountdown = new System.Windows.Forms.Label();
            this.tableLayoutPanelButton = new System.Windows.Forms.TableLayoutPanel();
            this.buttonLeft = new System.Windows.Forms.Button();
            this.buttonMiddle = new System.Windows.Forms.Button();
            this.buttonRight = new System.Windows.Forms.Button();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.flowLayoutPanelCloseApps.SuspendLayout();
            this.flowLayoutPanelDeferral.SuspendLayout();
            this.flowLayoutPanelCountdown.SuspendLayout();
            this.tableLayoutPanelButton.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.labelWelcomeMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.labelAppName);
            this.flowLayoutPanelDialog.Controls.Add(this.labelCustomMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelCloseApps);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelDeferral);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelCountdown);
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelButton);
            this.flowLayoutPanelDialog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(17);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 503);
            this.flowLayoutPanelDialog.TabIndex = 3;
            // 
            // labelWelcomeMessage
            // 
            this.labelWelcomeMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelWelcomeMessage.AutoSize = true;
            this.labelWelcomeMessage.Location = new System.Drawing.Point(17, 17);
            this.labelWelcomeMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelWelcomeMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelWelcomeMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelWelcomeMessage.Name = "labelWelcomeMessage";
            this.labelWelcomeMessage.Size = new System.Drawing.Size(416, 15);
            this.labelWelcomeMessage.TabIndex = 0;
            this.labelWelcomeMessage.Text = "The following application is about to be installed:";
            this.labelWelcomeMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelAppName
            // 
            this.labelAppName.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelAppName.AutoSize = true;
            this.labelAppName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppName.Location = new System.Drawing.Point(17, 44);
            this.labelAppName.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.labelAppName.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelAppName.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(416, 21);
            this.labelAppName.TabIndex = 1;
            this.labelAppName.Text = "Adobe Acrobat Unified 25.001.20428";
            this.labelAppName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCustomMessage
            // 
            this.labelCustomMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelCustomMessage.AutoSize = true;
            this.labelCustomMessage.Location = new System.Drawing.Point(17, 77);
            this.labelCustomMessage.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.labelCustomMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelCustomMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelCustomMessage.Name = "labelCustomMessage";
            this.labelCustomMessage.Size = new System.Drawing.Size(416, 30);
            this.labelCustomMessage.TabIndex = 5;
            this.labelCustomMessage.Text = "This is a custom message that you can optionally display here  This could include" +
    " info specific to the app, or general info for your end users.";
            this.labelCustomMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelCloseApps
            // 
            this.flowLayoutPanelCloseApps.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanelCloseApps.AutoSize = true;
            this.flowLayoutPanelCloseApps.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelCloseApps.Controls.Add(this.labelCloseProcessesMessage);
            this.flowLayoutPanelCloseApps.Controls.Add(this.listBoxCloseProcesses);
            this.flowLayoutPanelCloseApps.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCloseApps.Location = new System.Drawing.Point(17, 119);
            this.flowLayoutPanelCloseApps.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelCloseApps.MaximumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelCloseApps.MinimumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelCloseApps.Name = "flowLayoutPanelCloseApps";
            this.flowLayoutPanelCloseApps.Size = new System.Drawing.Size(416, 178);
            this.flowLayoutPanelCloseApps.TabIndex = 2;
            // 
            // labelCloseProcessesMessage
            // 
            this.labelCloseProcessesMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelCloseProcessesMessage.AutoSize = true;
            this.labelCloseProcessesMessage.Location = new System.Drawing.Point(0, 0);
            this.labelCloseProcessesMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelCloseProcessesMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelCloseProcessesMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelCloseProcessesMessage.Name = "labelCloseProcessesMessage";
            this.labelCloseProcessesMessage.Size = new System.Drawing.Size(416, 60);
            this.labelCloseProcessesMessage.TabIndex = 0;
            this.labelCloseProcessesMessage.Text = "The following programs must be closed before the installation can proceed.\n\nPleas" +
    "e save your work, close the programs, and then continue. Alternatively, save you" +
    "r work and click \"Close Programs\".";
            this.labelCloseProcessesMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // listBoxCloseProcesses
            // 
            this.listBoxCloseProcesses.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.listBoxCloseProcesses.FormattingEnabled = true;
            this.listBoxCloseProcesses.ItemHeight = 15;
            this.listBoxCloseProcesses.Items.AddRange(new object[] {
            "Adobe Acrobat",
            "Microsoft Word",
            "Microsoft Excel"});
            this.listBoxCloseProcesses.Location = new System.Drawing.Point(0, 69);
            this.listBoxCloseProcesses.Margin = new System.Windows.Forms.Padding(0);
            this.listBoxCloseProcesses.MaximumSize = new System.Drawing.Size(416, 109);
            this.listBoxCloseProcesses.MinimumSize = new System.Drawing.Size(416, 109);
            this.listBoxCloseProcesses.Name = "listBoxCloseProcesses";
            this.listBoxCloseProcesses.Size = new System.Drawing.Size(416, 109);
            this.listBoxCloseProcesses.TabIndex = 1;
            // 
            // flowLayoutPanelDeferral
            // 
            this.flowLayoutPanelDeferral.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanelDeferral.AutoSize = true;
            this.flowLayoutPanelDeferral.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferralExpiryMessage);
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferDeadline);
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferWarningMessage);
            this.flowLayoutPanelDeferral.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDeferral.Location = new System.Drawing.Point(17, 309);
            this.flowLayoutPanelDeferral.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelDeferral.MaximumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelDeferral.MinimumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelDeferral.Name = "flowLayoutPanelDeferral";
            this.flowLayoutPanelDeferral.Size = new System.Drawing.Size(416, 63);
            this.flowLayoutPanelDeferral.TabIndex = 3;
            // 
            // labelDeferralExpiryMessage
            // 
            this.labelDeferralExpiryMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelDeferralExpiryMessage.AutoSize = true;
            this.labelDeferralExpiryMessage.Location = new System.Drawing.Point(0, 0);
            this.labelDeferralExpiryMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelDeferralExpiryMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelDeferralExpiryMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelDeferralExpiryMessage.Name = "labelDeferralExpiryMessage";
            this.labelDeferralExpiryMessage.Size = new System.Drawing.Size(416, 15);
            this.labelDeferralExpiryMessage.TabIndex = 0;
            this.labelDeferralExpiryMessage.Text = "You can choose to defer the installation until the deferral expires:";
            this.labelDeferralExpiryMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDeferDeadline
            // 
            this.labelDeferDeadline.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelDeferDeadline.AutoSize = true;
            this.labelDeferDeadline.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeferDeadline.Location = new System.Drawing.Point(0, 24);
            this.labelDeferDeadline.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelDeferDeadline.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelDeferDeadline.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelDeferDeadline.Name = "labelDeferDeadline";
            this.labelDeferDeadline.Size = new System.Drawing.Size(416, 15);
            this.labelDeferDeadline.TabIndex = 1;
            this.labelDeferDeadline.Text = "Remaining Deferrals: 3";
            this.labelDeferDeadline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDeferWarningMessage
            // 
            this.labelDeferWarningMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelDeferWarningMessage.AutoSize = true;
            this.labelDeferWarningMessage.Location = new System.Drawing.Point(0, 48);
            this.labelDeferWarningMessage.Margin = new System.Windows.Forms.Padding(0);
            this.labelDeferWarningMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelDeferWarningMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelDeferWarningMessage.Name = "labelDeferWarningMessage";
            this.labelDeferWarningMessage.Size = new System.Drawing.Size(416, 15);
            this.labelDeferWarningMessage.TabIndex = 2;
            this.labelDeferWarningMessage.Text = "Once the deferral has expired, you will no longer have the option to defer.";
            this.labelDeferWarningMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelCountdown
            // 
            this.flowLayoutPanelCountdown.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.flowLayoutPanelCountdown.AutoSize = true;
            this.flowLayoutPanelCountdown.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelCountdown.Controls.Add(this.labelCountdownMessage);
            this.flowLayoutPanelCountdown.Controls.Add(this.labelCountdown);
            this.flowLayoutPanelCountdown.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCountdown.Location = new System.Drawing.Point(17, 384);
            this.flowLayoutPanelCountdown.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelCountdown.MaximumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelCountdown.MinimumSize = new System.Drawing.Size(416, 0);
            this.flowLayoutPanelCountdown.Name = "flowLayoutPanelCountdown";
            this.flowLayoutPanelCountdown.Size = new System.Drawing.Size(416, 53);
            this.flowLayoutPanelCountdown.TabIndex = 4;
            // 
            // labelCountdownMessage
            // 
            this.labelCountdownMessage.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelCountdownMessage.AutoSize = true;
            this.labelCountdownMessage.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdownMessage.Location = new System.Drawing.Point(0, 0);
            this.labelCountdownMessage.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdownMessage.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelCountdownMessage.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelCountdownMessage.Name = "labelCountdownMessage";
            this.labelCountdownMessage.Size = new System.Drawing.Size(416, 21);
            this.labelCountdownMessage.TabIndex = 0;
            this.labelCountdownMessage.Text = "The installation will automatically continue in:";
            this.labelCountdownMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCountdown
            // 
            this.labelCountdown.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.labelCountdown.AutoSize = true;
            this.labelCountdown.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdown.Location = new System.Drawing.Point(0, 21);
            this.labelCountdown.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdown.MaximumSize = new System.Drawing.Size(416, 0);
            this.labelCountdown.MinimumSize = new System.Drawing.Size(416, 0);
            this.labelCountdown.Name = "labelCountdown";
            this.labelCountdown.Size = new System.Drawing.Size(416, 32);
            this.labelCountdown.TabIndex = 1;
            this.labelCountdown.Text = "1:23:45";
            this.labelCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(17, 461);
            this.tableLayoutPanelButton.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.tableLayoutPanelButton.MaximumSize = new System.Drawing.Size(416, 0);
            this.tableLayoutPanelButton.MinimumSize = new System.Drawing.Size(416, 0);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 1;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(416, 25);
            this.tableLayoutPanelButton.TabIndex = 4;
            // 
            // buttonLeft
            // 
            this.buttonLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonLeft.Location = new System.Drawing.Point(0, 0);
            this.buttonLeft.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLeft.MaximumSize = new System.Drawing.Size(135, 25);
            this.buttonLeft.MinimumSize = new System.Drawing.Size(135, 25);
            this.buttonLeft.Name = "buttonLeft";
            this.buttonLeft.Size = new System.Drawing.Size(135, 25);
            this.buttonLeft.TabIndex = 0;
            this.buttonLeft.Text = "Close Programs";
            this.buttonLeft.UseVisualStyleBackColor = true;
            // 
            // buttonMiddle
            // 
            this.buttonMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonMiddle.Location = new System.Drawing.Point(140, 0);
            this.buttonMiddle.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.buttonMiddle.MaximumSize = new System.Drawing.Size(135, 25);
            this.buttonMiddle.MinimumSize = new System.Drawing.Size(135, 25);
            this.buttonMiddle.Name = "buttonMiddle";
            this.buttonMiddle.Size = new System.Drawing.Size(135, 25);
            this.buttonMiddle.TabIndex = 1;
            this.buttonMiddle.Text = "Defer";
            this.buttonMiddle.UseVisualStyleBackColor = true;
            // 
            // buttonRight
            // 
            this.buttonRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonRight.Location = new System.Drawing.Point(281, 0);
            this.buttonRight.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRight.MaximumSize = new System.Drawing.Size(135, 25);
            this.buttonRight.MinimumSize = new System.Drawing.Size(135, 25);
            this.buttonRight.Name = "buttonRight";
            this.buttonRight.Size = new System.Drawing.Size(135, 25);
            this.buttonRight.TabIndex = 2;
            this.buttonRight.Text = "Continue";
            this.buttonRight.UseVisualStyleBackColor = true;
            // 
            // CloseAppsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 503);
            this.Controls.Add(this.flowLayoutPanelDialog);
            this.MaximumSize = new System.Drawing.Size(466, 0);
            this.MinimumSize = new System.Drawing.Size(466, 0);
            this.Name = "CloseAppsDialog";
            this.Text = "CloseAppsDialog";
            this.Controls.SetChildIndex(this.flowLayoutPanelDialog, 0);
            this.flowLayoutPanelDialog.ResumeLayout(false);
            this.flowLayoutPanelDialog.PerformLayout();
            this.flowLayoutPanelCloseApps.ResumeLayout(false);
            this.flowLayoutPanelCloseApps.PerformLayout();
            this.flowLayoutPanelDeferral.ResumeLayout(false);
            this.flowLayoutPanelDeferral.PerformLayout();
            this.flowLayoutPanelCountdown.ResumeLayout(false);
            this.flowLayoutPanelCountdown.PerformLayout();
            this.tableLayoutPanelButton.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelDialog;
        private Label labelWelcomeMessage;
        private Label labelAppName;
        private FlowLayoutPanel flowLayoutPanelCloseApps;
        private Label labelCloseProcessesMessage;
        private ListBox listBoxCloseProcesses;
        private FlowLayoutPanel flowLayoutPanelDeferral;
        private Label labelDeferralExpiryMessage;
        private Label labelDeferDeadline;
        private Label labelDeferWarningMessage;
        private FlowLayoutPanel flowLayoutPanelCountdown;
        private Label labelCountdownMessage;
        private Label labelCountdown;
        private TableLayoutPanel tableLayoutPanelButton;
        private Button buttonLeft;
        private Button buttonRight;
        private Button buttonMiddle;
        private Label labelCustomMessage;
    }
}
