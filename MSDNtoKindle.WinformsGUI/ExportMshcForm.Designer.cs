// Copyright (c) Microsoft Corporation.  All rights reserved.
//
namespace PackageThis.GUI
{
    partial class ExportMshcForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.BrowseBtn = new System.Windows.Forms.Button();
            this.OKBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.VendorName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.ProdName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.BookName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.MshcFileTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Mshc File:";
            // 
            // BrowseBtn
            // 
            this.BrowseBtn.Location = new System.Drawing.Point(461, 13);
            this.BrowseBtn.Name = "BrowseBtn";
            this.BrowseBtn.Size = new System.Drawing.Size(75, 23);
            this.BrowseBtn.TabIndex = 0;
            this.BrowseBtn.Text = "&Browse...";
            this.BrowseBtn.UseVisualStyleBackColor = true;
            this.BrowseBtn.Click += new System.EventHandler(this.BrowseBtn_Click);
            // 
            // OKBtn
            // 
            this.OKBtn.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OKBtn.Enabled = false;
            this.OKBtn.Location = new System.Drawing.Point(299, 135);
            this.OKBtn.Name = "OKBtn";
            this.OKBtn.Size = new System.Drawing.Size(75, 23);
            this.OKBtn.TabIndex = 9;
            this.OKBtn.Text = "OK";
            this.OKBtn.UseVisualStyleBackColor = true;
            // 
            // CancelBtn
            // 
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(380, 135);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(75, 23);
            this.CancelBtn.TabIndex = 10;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            // 
            // saveFileDialog1
            // 
            this.saveFileDialog1.Filter = "Mshc files (*.mshc)|*.mshc";
            // 
            // VendorName
            // 
            this.VendorName.Location = new System.Drawing.Point(91, 49);
            this.VendorName.Name = "VendorName";
            this.VendorName.Size = new System.Drawing.Size(364, 20);
            this.VendorName.TabIndex = 4;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 52);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(75, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Vendor Name:";
            // 
            // ProdName
            // 
            this.ProdName.Location = new System.Drawing.Point(91, 75);
            this.ProdName.Name = "ProdName";
            this.ProdName.Size = new System.Drawing.Size(364, 20);
            this.ProdName.TabIndex = 6;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 78);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(78, 13);
            this.label4.TabIndex = 5;
            this.label4.Text = "Product Name:";
            // 
            // BookName
            // 
            this.BookName.Location = new System.Drawing.Point(91, 102);
            this.BookName.Name = "BookName";
            this.BookName.Size = new System.Drawing.Size(364, 20);
            this.BookName.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(21, 105);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(66, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Book Name:";
            // 
            // MshcFileTextBox
            // 
            this.MshcFileTextBox.BackColor = System.Drawing.SystemColors.Control;
            this.MshcFileTextBox.Location = new System.Drawing.Point(91, 15);
            this.MshcFileTextBox.Name = "MshcFileTextBox";
            this.MshcFileTextBox.ReadOnly = true;
            this.MshcFileTextBox.Size = new System.Drawing.Size(364, 20);
            this.MshcFileTextBox.TabIndex = 2;
            this.MshcFileTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // ExportMshcForm
            // 
            this.AcceptButton = this.OKBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(548, 168);
            this.Controls.Add(this.MshcFileTextBox);
            this.Controls.Add(this.BookName);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.ProdName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.VendorName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OKBtn);
            this.Controls.Add(this.BrowseBtn);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportMshcForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Export to Mshc File";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ExportMshcForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button BrowseBtn;
        private System.Windows.Forms.Button OKBtn;
        private System.Windows.Forms.Button CancelBtn;
        //public System.Windows.Forms.TextBox MshcFileTextBox;
        public System.Windows.Forms.TextBox MshcVendorNameBox;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        public System.Windows.Forms.TextBox VendorName;
        private System.Windows.Forms.Label label3;
        public System.Windows.Forms.TextBox ProdName;
        private System.Windows.Forms.Label label4;
        public System.Windows.Forms.TextBox BookName;
        private System.Windows.Forms.Label label5;
        public System.Windows.Forms.TextBox MshcFileTextBox;
    }
}