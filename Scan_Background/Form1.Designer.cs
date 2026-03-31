namespace Scan_Background
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.CheckBox chkRunInTray;
        private System.Windows.Forms.CheckBox chkNotifications;
        private System.Windows.Forms.CheckBox chkDebug;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnClearLogs;

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
            SuspendLayout();
            // 
            // chkRunInTray
            // 
            this.chkRunInTray = new System.Windows.Forms.CheckBox();
            this.chkNotifications = new System.Windows.Forms.CheckBox();
            this.chkDebug = new System.Windows.Forms.CheckBox();
            this.btnApply = new System.Windows.Forms.Button();
            // 
            // chkRunInTray
            // 
            this.chkRunInTray.AutoSize = true;
            this.chkRunInTray.Location = new System.Drawing.Point(12, 12);
            this.chkRunInTray.Name = "chkRunInTray";
            this.chkRunInTray.Size = new System.Drawing.Size(166, 19);
            this.chkRunInTray.TabIndex = 0;
            this.chkRunInTray.Text = "Запускать в трее";
            this.chkRunInTray.UseVisualStyleBackColor = true;
            // 
            // chkNotifications
            // 
            this.chkNotifications.AutoSize = true;
            this.chkNotifications.Location = new System.Drawing.Point(12, 37);
            this.chkNotifications.Name = "chkNotifications";
            this.chkNotifications.Size = new System.Drawing.Size(154, 19);
            this.chkNotifications.TabIndex = 1;
            this.chkNotifications.Text = "Включить уведомления";
            this.chkNotifications.UseVisualStyleBackColor = true;
            // 
            // chkDebug
            // 
            this.chkDebug.AutoSize = true;
            this.chkDebug.Location = new System.Drawing.Point(12, 62);
            this.chkDebug.Name = "chkDebug";
            this.chkDebug.Size = new System.Drawing.Size(66, 19);
            this.chkDebug.TabIndex = 2;
            this.chkDebug.Text = "Отладка";
            this.chkDebug.UseVisualStyleBackColor = true;
            this.chkDebug.CheckedChanged += new System.EventHandler(this.chkDebug_CheckedChanged);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(12, 95);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 3;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // txtLog
            // 
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.txtLog.Location = new System.Drawing.Point(12, 130);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(296, 200);
            this.txtLog.TabIndex = 4;
            this.txtLog.Visible = false;
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Location = new System.Drawing.Point(100, 95);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(120, 23);
            this.btnClearLogs.TabIndex = 5;
            this.btnClearLogs.Text = "Обнулить логи";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Visible = false;
            this.btnClearLogs.Click += new System.EventHandler(this.btnClearLogs_Click);
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(320, 360);
            Controls.Add(this.chkRunInTray);
            Controls.Add(this.chkNotifications);
            Controls.Add(this.chkDebug);
            Controls.Add(this.btnApply);
            Controls.Add(this.txtLog);
            Controls.Add(this.btnClearLogs);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Настройки";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
