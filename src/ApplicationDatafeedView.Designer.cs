namespace SyncroSim.GCAM
{
    partial class ApplicationDatafeedView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.LabelGCAMFolder = new System.Windows.Forms.Label();
            this.TextBoxGCAMFolder = new System.Windows.Forms.TextBox();
            this.ButtonBrowseGCAMFolder = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LabelGCAMFolder
            // 
            this.LabelGCAMFolder.AutoSize = true;
            this.LabelGCAMFolder.Location = new System.Drawing.Point(15, 18);
            this.LabelGCAMFolder.Margin = new System.Windows.Forms.Padding(3, 7, 3, 0);
            this.LabelGCAMFolder.Name = "LabelGCAMFolder";
            this.LabelGCAMFolder.Size = new System.Drawing.Size(124, 13);
            this.LabelGCAMFolder.TabIndex = 2;
            this.LabelGCAMFolder.Text = "GCAM application folder:";
            this.LabelGCAMFolder.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // TextBoxGCAMFolder
            // 
            this.TextBoxGCAMFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TextBoxGCAMFolder.Location = new System.Drawing.Point(18, 43);
            this.TextBoxGCAMFolder.Name = "TextBoxGCAMFolder";
            this.TextBoxGCAMFolder.Size = new System.Drawing.Size(545, 20);
            this.TextBoxGCAMFolder.TabIndex = 3;
            // 
            // ButtonBrowseGCAMFolder
            // 
            this.ButtonBrowseGCAMFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonBrowseGCAMFolder.Location = new System.Drawing.Point(569, 41);
            this.ButtonBrowseGCAMFolder.Name = "ButtonBrowseGCAMFolder";
            this.ButtonBrowseGCAMFolder.Size = new System.Drawing.Size(75, 23);
            this.ButtonBrowseGCAMFolder.TabIndex = 4;
            this.ButtonBrowseGCAMFolder.Text = "Browse...";
            this.ButtonBrowseGCAMFolder.UseVisualStyleBackColor = true;
            this.ButtonBrowseGCAMFolder.Click += new System.EventHandler(this.ButtonBrowseGCAMFolder_Click);
            // 
            // ApplicationDatafeedView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LabelGCAMFolder);
            this.Controls.Add(this.TextBoxGCAMFolder);
            this.Controls.Add(this.ButtonBrowseGCAMFolder);
            this.Name = "ApplicationDatafeedView";
            this.Size = new System.Drawing.Size(671, 92);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Label LabelGCAMFolder;
        internal System.Windows.Forms.TextBox TextBoxGCAMFolder;
        internal System.Windows.Forms.Button ButtonBrowseGCAMFolder;
    }
}
