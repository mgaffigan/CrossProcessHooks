﻿namespace Itp.Win32.MdiHook.DemoClient
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.btCloseLast = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.btHookNotepad = new System.Windows.Forms.Button();
            this.btHookWpf = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(72, 56);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(149, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Show in demo window";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(72, 85);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(149, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "Show in non-resizable";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(72, 114);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(149, 23);
            this.button3.TabIndex = 2;
            this.button3.Text = "Show and move";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // btCloseLast
            // 
            this.btCloseLast.Enabled = false;
            this.btCloseLast.Location = new System.Drawing.Point(227, 56);
            this.btCloseLast.Margin = new System.Windows.Forms.Padding(2);
            this.btCloseLast.Name = "btCloseLast";
            this.btCloseLast.Size = new System.Drawing.Size(27, 23);
            this.btCloseLast.TabIndex = 3;
            this.btCloseLast.Text = "X";
            this.btCloseLast.UseVisualStyleBackColor = true;
            this.btCloseLast.Click += new System.EventHandler(this.btCloseLast_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(72, 143);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(149, 23);
            this.button4.TabIndex = 4;
            this.button4.Text = "Show immovable";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // btHookNotepad
            // 
            this.btHookNotepad.Location = new System.Drawing.Point(72, 172);
            this.btHookNotepad.Name = "btHookNotepad";
            this.btHookNotepad.Size = new System.Drawing.Size(149, 23);
            this.btHookNotepad.TabIndex = 5;
            this.btHookNotepad.Text = "Hook Notepad";
            this.btHookNotepad.UseVisualStyleBackColor = true;
            this.btHookNotepad.Click += new System.EventHandler(this.button5_Click);
            // 
            // btHookWpf
            // 
            this.btHookWpf.Location = new System.Drawing.Point(72, 201);
            this.btHookWpf.Name = "btHookWpf";
            this.btHookWpf.Size = new System.Drawing.Size(149, 23);
            this.btHookWpf.TabIndex = 6;
            this.btHookWpf.Text = "Hook WPF";
            this.btHookWpf.UseVisualStyleBackColor = true;
            this.btHookWpf.Click += new System.EventHandler(this.btHookWpf_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.btHookWpf);
            this.Controls.Add(this.btHookNotepad);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.btCloseLast);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button btCloseLast;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button btHookNotepad;
        private System.Windows.Forms.Button btHookWpf;
    }
}

