namespace Phoenix.Forms.Engage
{
    partial class EngageWidget
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
            if (disposing)
            {
                ReleaseResources();
                if(components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AuthBrowser = new Awesomium.Windows.Forms.WebControl();
            this.SuspendLayout();
            // 
            // AuthBrowser
            // 
            this.AuthBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.AuthBrowser.Location = new System.Drawing.Point(0, 0);
            this.AuthBrowser.Name = "AuthBrowser";
            this.AuthBrowser.Size = new System.Drawing.Size(150, 150);
            this.AuthBrowser.TabIndex = 0;
            // 
            // EngageWidget
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.AuthBrowser);
            this.Name = "EngageWidget";
            this.ResumeLayout(false);

        }

        #endregion

        private Awesomium.Windows.Forms.WebControl AuthBrowser;
    }
}
