namespace DNS_Tunneling_Client
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.domain_txt = new System.Windows.Forms.TextBox();
            this.start_btn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // domain_txt
            // 
            this.domain_txt.Location = new System.Drawing.Point(455, 116);
            this.domain_txt.Name = "domain_txt";
            this.domain_txt.Size = new System.Drawing.Size(200, 39);
            this.domain_txt.TabIndex = 0;
            this.domain_txt.Text = "oszimt.gq";
            // 
            // start_btn
            // 
            this.start_btn.Location = new System.Drawing.Point(441, 330);
            this.start_btn.Name = "start_btn";
            this.start_btn.Size = new System.Drawing.Size(150, 46);
            this.start_btn.TabIndex = 1;
            this.start_btn.Text = "Start";
            this.start_btn.UseVisualStyleBackColor = true;
            this.start_btn.MouseClick += new System.Windows.Forms.MouseEventHandler(this.start_btn_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 482);
            this.Controls.Add(this.start_btn);
            this.Controls.Add(this.domain_txt);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox domain_txt;
        private System.Windows.Forms.Button start_btn;
    }
}

