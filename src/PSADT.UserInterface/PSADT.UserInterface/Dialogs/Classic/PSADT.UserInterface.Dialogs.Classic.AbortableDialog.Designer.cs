namespace PSADT.UserInterface.Dialogs.Classic
{
    partial class AbortableDialog
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
            this.buttonAbort = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonAbort
            // 
            this.buttonAbort.BackColor = System.Drawing.Color.Transparent;
            this.buttonAbort.DialogResult = System.Windows.Forms.DialogResult.Abort;
            this.buttonAbort.FlatAppearance.BorderSize = 0;
            this.buttonAbort.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonAbort.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonAbort.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonAbort.ForeColor = System.Drawing.Color.Transparent;
            this.buttonAbort.Location = new System.Drawing.Point(0, 0);
            this.buttonAbort.Margin = new System.Windows.Forms.Padding(0);
            this.buttonAbort.Name = "buttonAbort";
            this.buttonAbort.Size = new System.Drawing.Size(0, 0);
            this.buttonAbort.TabIndex = 2;
            this.buttonAbort.TabStop = false;
            this.buttonAbort.Text = "button1";
            this.buttonAbort.UseVisualStyleBackColor = true;
            // 
            // AbortableDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(450, 63);
            this.Controls.Add(this.buttonAbort);
            this.Name = "AbortableDialog";
            this.Text = "AbortableDialog";
            this.Controls.SetChildIndex(this.buttonAbort, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private Button buttonAbort;
    }
}
