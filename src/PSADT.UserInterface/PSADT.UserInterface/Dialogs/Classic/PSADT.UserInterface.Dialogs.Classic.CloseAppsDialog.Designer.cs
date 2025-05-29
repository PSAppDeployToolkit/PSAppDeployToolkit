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
            this.components = new System.ComponentModel.Container();
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
            this.buttonCloseProcesses = new System.Windows.Forms.Button();
            this.buttonDefer = new System.Windows.Forms.Button();
            this.toolTipButtonContinue = new System.Windows.Forms.ToolTip(this.components);
            this.buttonContinue = new System.Windows.Forms.Button();
            this.flowLayoutPanelDialog.SuspendLayout();
            this.flowLayoutPanelCloseApps.SuspendLayout();
            this.flowLayoutPanelDeferral.SuspendLayout();
            this.flowLayoutPanelCountdown.SuspendLayout();
            this.tableLayoutPanelButton.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanelDialog
            // 
            this.flowLayoutPanelDialog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelDialog.AutoSize = true;
            this.flowLayoutPanelDialog.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDialog.Controls.Add(this.labelWelcomeMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.labelAppName);
            this.flowLayoutPanelDialog.Controls.Add(this.labelCustomMessage);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelCloseApps);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelDeferral);
            this.flowLayoutPanelDialog.Controls.Add(this.flowLayoutPanelCountdown);
            this.flowLayoutPanelDialog.Controls.Add(this.tableLayoutPanelButton);
            this.flowLayoutPanelDialog.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDialog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelDialog.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanelDialog.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelDialog.MaximumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.MinimumSize = new System.Drawing.Size(450, 0);
            this.flowLayoutPanelDialog.Name = "flowLayoutPanelDialog";
            this.flowLayoutPanelDialog.Padding = new System.Windows.Forms.Padding(11, 9, 11, 10);
            this.flowLayoutPanelDialog.Size = new System.Drawing.Size(450, 466);
            this.flowLayoutPanelDialog.TabIndex = 3;
            this.flowLayoutPanelDialog.WrapContents = false;
            // 
            // labelWelcomeMessage
            // 
            this.labelWelcomeMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWelcomeMessage.AutoSize = true;
            this.labelWelcomeMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWelcomeMessage.Location = new System.Drawing.Point(11, 9);
            this.labelWelcomeMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 12);
            this.labelWelcomeMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelWelcomeMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelWelcomeMessage.Name = "labelWelcomeMessage";
            this.labelWelcomeMessage.Size = new System.Drawing.Size(428, 15);
            this.labelWelcomeMessage.TabIndex = 0;
            this.labelWelcomeMessage.Text = "The following application is about to be installed:";
            this.labelWelcomeMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelAppName
            // 
            this.labelAppName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelAppName.AutoSize = true;
            this.labelAppName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppName.Location = new System.Drawing.Point(11, 36);
            this.labelAppName.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.labelAppName.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelAppName.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelAppName.Name = "labelAppName";
            this.labelAppName.Size = new System.Drawing.Size(428, 21);
            this.labelAppName.TabIndex = 1;
            this.labelAppName.Text = "Adobe Acrobat Unified 25.001.20428";
            this.labelAppName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCustomMessage
            // 
            this.labelCustomMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCustomMessage.AutoSize = true;
            this.labelCustomMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCustomMessage.Location = new System.Drawing.Point(11, 69);
            this.labelCustomMessage.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.labelCustomMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelCustomMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelCustomMessage.Name = "labelCustomMessage";
            this.labelCustomMessage.Size = new System.Drawing.Size(428, 30);
            this.labelCustomMessage.TabIndex = 5;
            this.labelCustomMessage.Text = "This is a custom message that you can optionally display here. This could include" +
    " info specific to the app, or general info for your end users.";
            this.labelCustomMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelCloseApps
            // 
            this.flowLayoutPanelCloseApps.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelCloseApps.AutoSize = true;
            this.flowLayoutPanelCloseApps.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelCloseApps.Controls.Add(this.labelCloseProcessesMessage);
            this.flowLayoutPanelCloseApps.Controls.Add(this.listBoxCloseProcesses);
            this.flowLayoutPanelCloseApps.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCloseApps.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelCloseApps.Location = new System.Drawing.Point(11, 111);
            this.flowLayoutPanelCloseApps.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelCloseApps.MaximumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelCloseApps.MinimumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelCloseApps.Name = "flowLayoutPanelCloseApps";
            this.flowLayoutPanelCloseApps.Size = new System.Drawing.Size(428, 163);
            this.flowLayoutPanelCloseApps.TabIndex = 2;
            this.flowLayoutPanelCloseApps.WrapContents = false;
            // 
            // labelCloseProcessesMessage
            // 
            this.labelCloseProcessesMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCloseProcessesMessage.AutoSize = true;
            this.labelCloseProcessesMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCloseProcessesMessage.Location = new System.Drawing.Point(0, 0);
            this.labelCloseProcessesMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelCloseProcessesMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelCloseProcessesMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelCloseProcessesMessage.Name = "labelCloseProcessesMessage";
            this.labelCloseProcessesMessage.Size = new System.Drawing.Size(428, 60);
            this.labelCloseProcessesMessage.TabIndex = 0;
            this.labelCloseProcessesMessage.Text = "The following programs must be closed before the installation can proceed.\n\nPleas" +
    "e save your work, close the programs, and then continue. Alternatively, save you" +
    "r work and click \"Close Programs\".";
            this.labelCloseProcessesMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // listBoxCloseProcesses
            // 
            this.listBoxCloseProcesses.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.listBoxCloseProcesses.Enabled = false;
            this.listBoxCloseProcesses.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxCloseProcesses.FormattingEnabled = true;
            this.listBoxCloseProcesses.ItemHeight = 15;
            this.listBoxCloseProcesses.Items.AddRange(new object[] {
            "Adobe Acrobat",
            "Microsoft Word",
            "Microsoft Excel"});
            this.listBoxCloseProcesses.Location = new System.Drawing.Point(3, 69);
            this.listBoxCloseProcesses.Margin = new System.Windows.Forms.Padding(0);
            this.listBoxCloseProcesses.Name = "listBoxCloseProcesses";
            this.listBoxCloseProcesses.Size = new System.Drawing.Size(422, 94);
            this.listBoxCloseProcesses.TabIndex = 1;
            // 
            // flowLayoutPanelDeferral
            // 
            this.flowLayoutPanelDeferral.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelDeferral.AutoSize = true;
            this.flowLayoutPanelDeferral.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferralExpiryMessage);
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferDeadline);
            this.flowLayoutPanelDeferral.Controls.Add(this.labelDeferWarningMessage);
            this.flowLayoutPanelDeferral.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelDeferral.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelDeferral.Location = new System.Drawing.Point(11, 286);
            this.flowLayoutPanelDeferral.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelDeferral.MaximumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelDeferral.MinimumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelDeferral.Name = "flowLayoutPanelDeferral";
            this.flowLayoutPanelDeferral.Size = new System.Drawing.Size(428, 63);
            this.flowLayoutPanelDeferral.TabIndex = 3;
            this.flowLayoutPanelDeferral.WrapContents = false;
            // 
            // labelDeferralExpiryMessage
            // 
            this.labelDeferralExpiryMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDeferralExpiryMessage.AutoSize = true;
            this.labelDeferralExpiryMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeferralExpiryMessage.Location = new System.Drawing.Point(0, 0);
            this.labelDeferralExpiryMessage.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelDeferralExpiryMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelDeferralExpiryMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelDeferralExpiryMessage.Name = "labelDeferralExpiryMessage";
            this.labelDeferralExpiryMessage.Size = new System.Drawing.Size(428, 15);
            this.labelDeferralExpiryMessage.TabIndex = 0;
            this.labelDeferralExpiryMessage.Text = "You can choose to defer the installation until the deferral expires:";
            this.labelDeferralExpiryMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDeferDeadline
            // 
            this.labelDeferDeadline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDeferDeadline.AutoSize = true;
            this.labelDeferDeadline.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeferDeadline.Location = new System.Drawing.Point(0, 24);
            this.labelDeferDeadline.Margin = new System.Windows.Forms.Padding(0, 0, 0, 9);
            this.labelDeferDeadline.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelDeferDeadline.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelDeferDeadline.Name = "labelDeferDeadline";
            this.labelDeferDeadline.Size = new System.Drawing.Size(428, 15);
            this.labelDeferDeadline.TabIndex = 1;
            this.labelDeferDeadline.Text = "Remaining Deferrals: 3";
            this.labelDeferDeadline.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelDeferWarningMessage
            // 
            this.labelDeferWarningMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelDeferWarningMessage.AutoSize = true;
            this.labelDeferWarningMessage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelDeferWarningMessage.Location = new System.Drawing.Point(0, 48);
            this.labelDeferWarningMessage.Margin = new System.Windows.Forms.Padding(0);
            this.labelDeferWarningMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelDeferWarningMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelDeferWarningMessage.Name = "labelDeferWarningMessage";
            this.labelDeferWarningMessage.Size = new System.Drawing.Size(428, 15);
            this.labelDeferWarningMessage.TabIndex = 2;
            this.labelDeferWarningMessage.Text = "Once the deferral has expired, you will no longer have the option to defer.";
            this.labelDeferWarningMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // flowLayoutPanelCountdown
            // 
            this.flowLayoutPanelCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelCountdown.AutoSize = true;
            this.flowLayoutPanelCountdown.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanelCountdown.Controls.Add(this.labelCountdownMessage);
            this.flowLayoutPanelCountdown.Controls.Add(this.labelCountdown);
            this.flowLayoutPanelCountdown.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanelCountdown.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.flowLayoutPanelCountdown.Location = new System.Drawing.Point(11, 361);
            this.flowLayoutPanelCountdown.Margin = new System.Windows.Forms.Padding(0, 6, 0, 6);
            this.flowLayoutPanelCountdown.MaximumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelCountdown.MinimumSize = new System.Drawing.Size(428, 0);
            this.flowLayoutPanelCountdown.Name = "flowLayoutPanelCountdown";
            this.flowLayoutPanelCountdown.Size = new System.Drawing.Size(428, 52);
            this.flowLayoutPanelCountdown.TabIndex = 4;
            this.flowLayoutPanelCountdown.WrapContents = false;
            // 
            // labelCountdownMessage
            // 
            this.labelCountdownMessage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCountdownMessage.AutoSize = true;
            this.labelCountdownMessage.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdownMessage.Location = new System.Drawing.Point(0, 0);
            this.labelCountdownMessage.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdownMessage.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelCountdownMessage.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelCountdownMessage.Name = "labelCountdownMessage";
            this.labelCountdownMessage.Size = new System.Drawing.Size(428, 20);
            this.labelCountdownMessage.TabIndex = 0;
            this.labelCountdownMessage.Text = "The installation will automatically continue in:";
            this.labelCountdownMessage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCountdown
            // 
            this.labelCountdown.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelCountdown.AutoSize = true;
            this.labelCountdown.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCountdown.Location = new System.Drawing.Point(0, 20);
            this.labelCountdown.Margin = new System.Windows.Forms.Padding(0);
            this.labelCountdown.MaximumSize = new System.Drawing.Size(428, 0);
            this.labelCountdown.MinimumSize = new System.Drawing.Size(428, 0);
            this.labelCountdown.Name = "labelCountdown";
            this.labelCountdown.Size = new System.Drawing.Size(428, 32);
            this.labelCountdown.TabIndex = 1;
            this.labelCountdown.Text = "1:23:45";
            this.labelCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.tableLayoutPanelButton.Controls.Add(this.buttonCloseProcesses, 0, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonDefer, 1, 0);
            this.tableLayoutPanelButton.Controls.Add(this.buttonContinue, 2, 0);
            this.tableLayoutPanelButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tableLayoutPanelButton.Location = new System.Drawing.Point(11, 431);
            this.tableLayoutPanelButton.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.tableLayoutPanelButton.Name = "tableLayoutPanelButton";
            this.tableLayoutPanelButton.RowCount = 1;
            this.tableLayoutPanelButton.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelButton.Size = new System.Drawing.Size(428, 25);
            this.tableLayoutPanelButton.TabIndex = 4;
            // 
            // buttonCloseProcesses
            // 
            this.buttonCloseProcesses.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonCloseProcesses.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCloseProcesses.Location = new System.Drawing.Point(0, 0);
            this.buttonCloseProcesses.Margin = new System.Windows.Forms.Padding(0);
            this.buttonCloseProcesses.MaximumSize = new System.Drawing.Size(137, 25);
            this.buttonCloseProcesses.MinimumSize = new System.Drawing.Size(137, 25);
            this.buttonCloseProcesses.Name = "buttonCloseProcesses";
            this.buttonCloseProcesses.Size = new System.Drawing.Size(137, 25);
            this.buttonCloseProcesses.TabIndex = 0;
            this.buttonCloseProcesses.Text = "Close Programs";
            this.buttonCloseProcesses.UseVisualStyleBackColor = true;
            this.buttonCloseProcesses.Click += new System.EventHandler(this.ButtonLeft_Click);
            // 
            // buttonDefer
            // 
            this.buttonDefer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)));
            this.buttonDefer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDefer.Location = new System.Drawing.Point(145, 0);
            this.buttonDefer.Margin = new System.Windows.Forms.Padding(0);
            this.buttonDefer.MaximumSize = new System.Drawing.Size(138, 25);
            this.buttonDefer.MinimumSize = new System.Drawing.Size(138, 25);
            this.buttonDefer.Name = "buttonDefer";
            this.buttonDefer.Size = new System.Drawing.Size(138, 25);
            this.buttonDefer.TabIndex = 1;
            this.buttonDefer.Text = "Defer";
            this.buttonDefer.UseVisualStyleBackColor = true;
            this.buttonDefer.Click += new System.EventHandler(this.ButtonMiddle_Click);
            // 
            // toolTipButtonContinue
            // 
            this.toolTipButtonContinue.AutoPopDelay = 5000;
            this.toolTipButtonContinue.BackColor = System.Drawing.Color.LightGoldenrodYellow;
            this.toolTipButtonContinue.InitialDelay = 100;
            this.toolTipButtonContinue.ReshowDelay = 100;
            // 
            // buttonContinue
            // 
            this.buttonContinue.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonContinue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonContinue.Location = new System.Drawing.Point(291, 0);
            this.buttonContinue.Margin = new System.Windows.Forms.Padding(0);
            this.buttonContinue.MaximumSize = new System.Drawing.Size(137, 25);
            this.buttonContinue.MinimumSize = new System.Drawing.Size(137, 25);
            this.buttonContinue.Name = "buttonContinue";
            this.buttonContinue.Size = new System.Drawing.Size(137, 25);
            this.buttonContinue.TabIndex = 2;
            this.buttonContinue.Text = "Continue";
            this.toolTipButtonContinue.SetToolTip(this.buttonContinue, "Only select \"Continue\" after closing the above listed application(s).");
            this.buttonContinue.UseVisualStyleBackColor = true;
            this.buttonContinue.Click += new System.EventHandler(this.ButtonRight_Click);
            // 
            // CloseAppsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 466);
            this.Controls.Add(this.flowLayoutPanelDialog);
            this.Name = "CloseAppsDialog";
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
        private Button buttonCloseProcesses;
        private Button buttonDefer;
        private Label labelCustomMessage;
        private ToolTip toolTipButtonContinue;
        private Button buttonContinue;
    }
}
