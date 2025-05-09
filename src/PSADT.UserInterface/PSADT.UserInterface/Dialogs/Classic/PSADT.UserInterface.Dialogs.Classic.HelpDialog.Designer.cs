namespace PSADT.UserInterface.Dialogs.Classic
{
    partial class HelpDialog
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
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.comboBox = new System.Windows.Forms.ComboBox();
            this.listBox = new System.Windows.Forms.ListBox();
            this.richTextBoxPanel = new System.Windows.Forms.Panel();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.tableLayoutPanel.SuspendLayout();
            this.richTextBoxPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.AutoSize = true;
            this.tableLayoutPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 308F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.comboBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.listBox, 0, 1);
            this.tableLayoutPanel.Controls.Add(this.richTextBoxPanel, 1, 0);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(1322, 579);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // comboBox
            // 
            this.comboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.comboBox.FormattingEnabled = true;
            this.comboBox.Location = new System.Drawing.Point(4, 3);
            this.comboBox.Margin = new System.Windows.Forms.Padding(4, 3, 0, 4);
            this.comboBox.Name = "comboBox";
            this.comboBox.Size = new System.Drawing.Size(304, 22);
            this.comboBox.Sorted = true;
            this.comboBox.TabIndex = 0;
            // 
            // listBox
            // 
            this.listBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox.FormattingEnabled = true;
            this.listBox.IntegralHeight = false;
            this.listBox.ItemHeight = 14;
            this.listBox.Location = new System.Drawing.Point(4, 29);
            this.listBox.Margin = new System.Windows.Forms.Padding(4, 0, 0, 4);
            this.listBox.Name = "listBox";
            this.listBox.Size = new System.Drawing.Size(304, 546);
            this.listBox.Sorted = true;
            this.listBox.TabIndex = 1;
            // 
            // richTextBoxPanel
            // 
            this.richTextBoxPanel.AutoSize = true;
            this.richTextBoxPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.richTextBoxPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxPanel.Controls.Add(this.richTextBox);
            this.richTextBoxPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxPanel.Location = new System.Drawing.Point(312, 3);
            this.richTextBoxPanel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 4);
            this.richTextBoxPanel.Name = "richTextBoxPanel";
            this.tableLayoutPanel.SetRowSpan(this.richTextBoxPanel, 2);
            this.richTextBoxPanel.Size = new System.Drawing.Size(1006, 572);
            this.richTextBoxPanel.TabIndex = 2;
            // 
            // richTextBox
            // 
            this.richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox.Location = new System.Drawing.Point(0, 0);
            this.richTextBox.Margin = new System.Windows.Forms.Padding(0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.Size = new System.Drawing.Size(1004, 570);
            this.richTextBox.TabIndex = 2;
            this.richTextBox.Text = "";
            this.richTextBox.WordWrap = false;
            // 
            // HelpDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(1322, 579);
            this.Controls.Add(this.tableLayoutPanel);
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "HelpDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PSAppDeployToolkit Help Console";
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.richTextBoxPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.ComboBox comboBox;
        private System.Windows.Forms.ListBox listBox;
        private System.Windows.Forms.Panel richTextBoxPanel;
        private System.Windows.Forms.RichTextBox richTextBox;
    }
}
