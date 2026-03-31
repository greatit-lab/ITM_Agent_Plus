// ITM_Agent_Plus/ucPanel/ucOntoOverrideNamesPanel.Designer.cs
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    partial class ucOntoOverrideNamesPanel
    {
        #pragma warning disable 0414 // 경고 비활성화
        private System.ComponentModel.IContainer components = null;
        #pragma warning restore 0414 // 경고 다시 활성화

        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btn_SelectFolder;
        private System.Windows.Forms.ComboBox cb_BaseDatePath;
        private System.Windows.Forms.Button btn_BaseClear;
        private System.Windows.Forms.ListBox lb_TargetComparePath;
        private System.Windows.Forms.Button btn_Remove;

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btn_Remove = new System.Windows.Forms.Button();
            this.lb_TargetComparePath = new System.Windows.Forms.ListBox();
            this.btn_SelectFolder = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_BaseClear = new System.Windows.Forms.Button();
            this.cb_BaseDatePath = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            //
            // splitContainer2
            //
            this.splitContainer2.Location = new System.Drawing.Point(21, 18);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            //
            // splitContainer2.Panel2
            //
            this.splitContainer2.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer2.Size = new System.Drawing.Size(632, 310);
            this.splitContainer2.SplitterDistance = 71;
            this.splitContainer2.TabIndex = 2;
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.btn_Remove);
            this.groupBox2.Controls.Add(this.lb_TargetComparePath);
            this.groupBox2.Controls.Add(this.btn_SelectFolder);
            this.groupBox2.Location = new System.Drawing.Point(4, 16);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(624, 216);
            this.groupBox2.TabIndex = 0;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "● Target Compare File Paths (Slot)";
            //
            // btn_Remove
            //
            this.btn_Remove.Location = new System.Drawing.Point(485, 71);
            this.btn_Remove.Name = "btn_Remove";
            this.btn_Remove.Size = new System.Drawing.Size(133, 33);
            this.btn_Remove.TabIndex = 7;
            this.btn_Remove.Text = "Remove";
            this.btn_Remove.UseVisualStyleBackColor = true;
            //
            // lb_TargetComparePath
            //
            this.lb_TargetComparePath.FormattingEnabled = true;
            this.lb_TargetComparePath.ItemHeight = 12;
            this.lb_TargetComparePath.Location = new System.Drawing.Point(20, 20);
            this.lb_TargetComparePath.Name = "lb_TargetComparePath";
            this.lb_TargetComparePath.Size = new System.Drawing.Size(450, 184);
            this.lb_TargetComparePath.TabIndex = 6;
            //
            // btn_SelectFolder
            //
            this.btn_SelectFolder.Location = new System.Drawing.Point(485, 20);
            this.btn_SelectFolder.Name = "btn_SelectFolder";
            this.btn_SelectFolder.Size = new System.Drawing.Size(133, 33);
            this.btn_SelectFolder.TabIndex = 5;
            this.btn_SelectFolder.Text = "Select Folder";
            this.btn_SelectFolder.UseVisualStyleBackColor = true;
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.btn_BaseClear);
            this.groupBox1.Controls.Add(this.cb_BaseDatePath);
            this.groupBox1.Location = new System.Drawing.Point(25, 21);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(624, 65);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "● Base Date File Path (Date)";
            //
            // btn_BaseClear
            //
            this.btn_BaseClear.Location = new System.Drawing.Point(485, 28);
            this.btn_BaseClear.Name = "btn_BaseClear";
            this.btn_BaseClear.Size = new System.Drawing.Size(133, 20);
            this.btn_BaseClear.TabIndex = 6;
            this.btn_BaseClear.Text = "Clear";
            this.btn_BaseClear.UseVisualStyleBackColor = true;
            //
            // cb_BaseDatePath
            //
            this.cb_BaseDatePath.FormattingEnabled = true;
            this.cb_BaseDatePath.Location = new System.Drawing.Point(20, 28);
            this.cb_BaseDatePath.Name = "cb_BaseDatePath";
            this.cb_BaseDatePath.Size = new System.Drawing.Size(450, 20);
            this.cb_BaseDatePath.TabIndex = 0;
            //
            // ucOntoOverrideNamesPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.splitContainer2);
            this.Name = "ucOntoOverrideNamesPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
