using System.Windows.Forms;

namespace PSADT.UserInterface.Dialogs.Classic
{
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomDialog));
            this.iconMessageTableLayout = new System.Windows.Forms.TableLayoutPanel();
            this.pictureIcon = new System.Windows.Forms.PictureBox();
            this.labelMessage = new System.Windows.Forms.Label();
            this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.buttonLeft = new System.Windows.Forms.Button();
            this.buttonMiddle = new System.Windows.Forms.Button();
            this.buttonRight = new System.Windows.Forms.Button();
            this.dialogFlowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.iconMessageTableLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).BeginInit();
            this.buttonTableLayoutPanel.SuspendLayout();
            this.dialogFlowLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // iconMessageTableLayout
            // 
            this.iconMessageTableLayout.AutoSize = true;
            this.iconMessageTableLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.iconMessageTableLayout.ColumnCount = 2;
            this.iconMessageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.iconMessageTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.iconMessageTableLayout.Controls.Add(this.pictureIcon, 0, 0);
            this.iconMessageTableLayout.Controls.Add(this.labelMessage, 1, 0);
            this.iconMessageTableLayout.Location = new System.Drawing.Point(24, 24);
            this.iconMessageTableLayout.Margin = new System.Windows.Forms.Padding(0);
            this.iconMessageTableLayout.Name = "iconMessageTableLayout";
            this.iconMessageTableLayout.RowCount = 1;
            this.iconMessageTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.iconMessageTableLayout.TabIndex = 3;
            // 
            // pictureIcon
            // 
            this.pictureIcon.Location = new System.Drawing.Point(0, 0);
            this.pictureIcon.Margin = new System.Windows.Forms.Padding(0, 0, 18, 18);
            this.pictureIcon.Name = "pictureIcon";
            this.pictureIcon.Size = new System.Drawing.Size(48, 48);
            this.pictureIcon.TabIndex = 0;
            this.pictureIcon.TabStop = false;
            this.pictureIcon.WaitOnLoad = true;
            // 
            // labelMessage
            // 
            this.labelMessage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelMessage.AutoSize = true;
            this.labelMessage.Location = new System.Drawing.Point(84, 3);
            this.labelMessage.Margin = new System.Windows.Forms.Padding(18, 3, 3, 18);
            this.labelMessage.MaximumSize = new System.Drawing.Size(317, 0);
            this.labelMessage.Name = "labelMessage";
            this.labelMessage.TabIndex = 1;
            this.labelMessage.Text = resources.GetString("labelMessage.Text");
            // 
            // buttonTableLayoutPanel
            // 
            this.buttonTableLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.buttonTableLayoutPanel.AutoSize = true;
            this.buttonTableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonTableLayoutPanel.ColumnCount = 3;
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.buttonTableLayoutPanel.Controls.Add(this.buttonLeft, 0, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.buttonMiddle, 1, 0);
            this.buttonTableLayoutPanel.Controls.Add(this.buttonRight, 2, 0);
            this.buttonTableLayoutPanel.Location = new System.Drawing.Point(25, 423);
            this.buttonTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0, 18, 0, 0);
            this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
            this.buttonTableLayoutPanel.RowCount = 1;
            this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this.buttonTableLayoutPanel.TabIndex = 4;
            // 
            // buttonLeft
            // 
            this.buttonLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonLeft.Location = new System.Drawing.Point(0, 0);
            this.buttonLeft.Margin = new System.Windows.Forms.Padding(0);
            this.buttonLeft.Name = "buttonLeft";
            this.buttonLeft.Size = new System.Drawing.Size(130, 25);
            this.buttonLeft.TabIndex = 0;
            this.buttonLeft.Text = "ButtonLeft";
            this.buttonLeft.UseVisualStyleBackColor = true;
            // 
            // buttonMiddle
            // 
            this.buttonMiddle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonMiddle.Location = new System.Drawing.Point(136, 0);
            this.buttonMiddle.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.buttonMiddle.Name = "buttonMiddle";
            this.buttonMiddle.Size = new System.Drawing.Size(130, 25);
            this.buttonMiddle.TabIndex = 1;
            this.buttonMiddle.Text = "ButtonMiddle";
            this.buttonMiddle.UseVisualStyleBackColor = true;
            // 
            // buttonRight
            // 
            this.buttonRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonRight.Location = new System.Drawing.Point(272, 0);
            this.buttonRight.Margin = new System.Windows.Forms.Padding(0);
            this.buttonRight.Name = "buttonRight";
            this.buttonRight.Size = new System.Drawing.Size(130, 25);
            this.buttonRight.TabIndex = 2;
            this.buttonRight.Text = "ButtonRight";
            this.buttonRight.UseVisualStyleBackColor = true;
            // 
            // dialogFlowLayoutPanel
            // 
            this.dialogFlowLayoutPanel.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.dialogFlowLayoutPanel.AutoSize = true;
            this.dialogFlowLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.dialogFlowLayoutPanel.Controls.Add(this.iconMessageTableLayout);
            this.dialogFlowLayoutPanel.Controls.Add(this.buttonTableLayoutPanel);
            this.dialogFlowLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.dialogFlowLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.dialogFlowLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.dialogFlowLayoutPanel.Name = "dialogFlowLayoutPanel";
            this.dialogFlowLayoutPanel.Padding = new System.Windows.Forms.Padding(24);
            this.dialogFlowLayoutPanel.TabIndex = 5;
            // 
            // CustomDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(450, 0);
            this.Controls.Add(this.dialogFlowLayoutPanel);
            this.Name = "CustomDialog";
            this.Text = "CustomDialog";
            this.Controls.SetChildIndex(this.dialogFlowLayoutPanel, 0);
            this.iconMessageTableLayout.ResumeLayout(false);
            this.iconMessageTableLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureIcon)).EndInit();
            this.buttonTableLayoutPanel.ResumeLayout(false);
            this.dialogFlowLayoutPanel.ResumeLayout(false);
            this.dialogFlowLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TableLayoutPanel iconMessageTableLayout;
        private PictureBox pictureIcon;
        private TableLayoutPanel buttonTableLayoutPanel;
        private Label labelMessage;
        private Button buttonLeft;
        private Button buttonRight;
        private Button buttonMiddle;
        private FlowLayoutPanel dialogFlowLayoutPanel;
    }
}
