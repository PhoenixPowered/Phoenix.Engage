namespace TestFormsApplication
{
    partial class Form1
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
            this.engageWidget1 = new Phoenix.Forms.Engage.EngageWidget();
            this.SuspendLayout();
            // 
            // engageWidget1
            // 
            this.engageWidget1.ApplicationName = "phx-jabbr-dev";
            this.engageWidget1.CanSwitchAccounts = false;
            this.engageWidget1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.engageWidget1.ForceReauth = false;
            this.engageWidget1.Location = new System.Drawing.Point(0, 0);
            this.engageWidget1.Name = "engageWidget1";
            this.engageWidget1.Size = new System.Drawing.Size(426, 170);
            this.engageWidget1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(426, 170);
            this.Controls.Add(this.engageWidget1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private Phoenix.Forms.Engage.EngageWidget engageWidget1;
    }
}

