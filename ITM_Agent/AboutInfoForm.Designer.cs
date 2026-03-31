// ITM_Agent_Plus/AboutInfoForm.Designer.cs
using System.Drawing;
using System.Windows.Forms;

namespace ITM_Agent
{
    partial class AboutInfoForm
    {
        /* ---------------- 디자이너 필드 ---------------- */
        private PictureBox picIcon;
        private Label lblTitle;
        private GroupBox grpDev;
        private Label lblDevList;
        private Button btnOk;

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// </summary>
        private void InitializeComponent()
        {
            this.picIcon = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.grpDev = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblDevList = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.lb_Version = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.grpDev.SuspendLayout();
            this.SuspendLayout();
            // 
            // picIcon
            // 
            this.picIcon.Location = new System.Drawing.Point(-14, 68);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new System.Drawing.Size(181, 191);
            this.picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picIcon.TabIndex = 0;
            this.picIcon.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblTitle.Location = new System.Drawing.Point(12, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(116, 25);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "ITM Agent+";
            // 
            // grpDev
            // 
            this.grpDev.Controls.Add(this.label5);
            this.grpDev.Controls.Add(this.lblDevList);
            this.grpDev.Location = new System.Drawing.Point(175, 149);
            this.grpDev.Name = "grpDev";
            this.grpDev.Size = new System.Drawing.Size(234, 59);
            this.grpDev.TabIndex = 3;
            this.grpDev.TabStop = false;
            this.grpDev.Text = "Developers";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 40);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(202, 12);
            this.label5.TabIndex = 1;
            this.label5.Text = "(Memory CMP Technology Team)";
            // 
            // lblDevList
            // 
            this.lblDevList.AutoSize = true;
            this.lblDevList.Location = new System.Drawing.Point(22, 20);
            this.lblDevList.Name = "lblDevList";
            this.lblDevList.Size = new System.Drawing.Size(157, 12);
            this.lblDevList.TabIndex = 0;
            this.lblDevList.Text = "• gily.choi@samsung.com";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(315, 212);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(94, 23);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "OK";
            // 
            // lb_Version
            // 
            this.lb_Version.AutoSize = true;
            this.lb_Version.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lb_Version.ForeColor = System.Drawing.Color.DarkOrange;
            this.lb_Version.Location = new System.Drawing.Point(121, 18);
            this.lb_Version.Name = "lb_Version";
            this.lb_Version.Size = new System.Drawing.Size(35, 21);
            this.lb_Version.TabIndex = 5;
            this.lb_Version.Text = "Ver";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(172, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 12);
            this.label1.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(172, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 12);
            this.label2.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(173, 97);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 12);
            this.label3.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.Control;
            this.label4.Location = new System.Drawing.Point(173, 117);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(0, 12);
            this.label4.TabIndex = 9;
            // 
            // AboutInfoForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(420, 240);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lb_Version);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.grpDev);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.picIcon);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutInfoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About Information...";
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.grpDev.ResumeLayout(false);
            this.grpDev.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        private Label lb_Version;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
    }
}
