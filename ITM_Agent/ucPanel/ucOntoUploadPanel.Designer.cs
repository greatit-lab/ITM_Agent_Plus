// ITM_Agent_Plus/ucPanel/ucOntoUploadPanel.Designer.cs
namespace ITM_Agent.ucPanel
{
    partial class ucOntoUploadPanel
    {
        private System.ComponentModel.IContainer components = null;

        // --- [수정] 기존 GroupBox 및 버튼/콤보박스 선언 모두 제거 ---
        // private System.Windows.Forms.GroupBox groupBox1;
        // ... (cb_WaferFlat_Path, btn_FlatSet 등 모두 제거) ...

        // --- [추가] 새로운 UI 컨트롤 선언 ---
        private System.Windows.Forms.TabControl tabControlUpload;
        private System.Windows.Forms.TabPage tpCategorized; // 탭 1
        private System.Windows.Forms.TabPage tpLiveMonitoring; // 탭 2
        private System.Windows.Forms.DataGridView dgvCategorized; // 탭 1 그리드
        private System.Windows.Forms.DataGridView dgvLiveMonitoring; // 탭 2 그리드
        private System.Windows.Forms.Button btnCatSave;
        private System.Windows.Forms.Button btnCatRemove;
        private System.Windows.Forms.Button btnCatAdd;
        private System.Windows.Forms.Panel panelTab1Buttons;
        private System.Windows.Forms.Panel panelTab2Buttons;
        private System.Windows.Forms.Button btnLiveSave;
        private System.Windows.Forms.Button btnLiveRemove;
        private System.Windows.Forms.Button btnLiveAdd;

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.tabControlUpload = new System.Windows.Forms.TabControl();
            this.tpCategorized = new System.Windows.Forms.TabPage();
            this.dgvCategorized = new System.Windows.Forms.DataGridView();
            this.panelTab1Buttons = new System.Windows.Forms.Panel();
            this.btnCatSave = new System.Windows.Forms.Button();
            this.btnCatRemove = new System.Windows.Forms.Button();
            this.btnCatAdd = new System.Windows.Forms.Button();
            this.tpLiveMonitoring = new System.Windows.Forms.TabPage();
            this.dgvLiveMonitoring = new System.Windows.Forms.DataGridView();
            this.dgvLive_TaskName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvLive_WatchFolder = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvLive_btnSelectFolder = new System.Windows.Forms.DataGridViewButtonColumn();
            this.dgvLive_FileFilter = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvLive_PluginName = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.panelTab2Buttons = new System.Windows.Forms.Panel();
            this.btnLiveSave = new System.Windows.Forms.Button();
            this.btnLiveRemove = new System.Windows.Forms.Button();
            this.btnLiveAdd = new System.Windows.Forms.Button();
            this.dgvCat_TaskName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvCat_WatchFolder = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.dgvCat_PluginName = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.tabControlUpload.SuspendLayout();
            this.tpCategorized.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorized)).BeginInit();
            this.panelTab1Buttons.SuspendLayout();
            this.tpLiveMonitoring.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLiveMonitoring)).BeginInit();
            this.panelTab2Buttons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlUpload
            // 
            this.tabControlUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlUpload.Controls.Add(this.tpCategorized);
            this.tabControlUpload.Controls.Add(this.tpLiveMonitoring);
            this.tabControlUpload.Location = new System.Drawing.Point(12, 6);
            this.tabControlUpload.Name = "tabControlUpload";
            this.tabControlUpload.SelectedIndex = 0;
            this.tabControlUpload.Size = new System.Drawing.Size(653, 330);
            this.tabControlUpload.TabIndex = 0;
            // 
            // tpCategorized
            // 
            this.tpCategorized.Controls.Add(this.dgvCategorized);
            this.tpCategorized.Controls.Add(this.panelTab1Buttons);
            this.tpCategorized.Location = new System.Drawing.Point(4, 22);
            this.tpCategorized.Name = "tpCategorized";
            this.tpCategorized.Padding = new System.Windows.Forms.Padding(3);
            this.tpCategorized.Size = new System.Drawing.Size(645, 304);
            this.tpCategorized.TabIndex = 0;
            this.tpCategorized.Text = global::ITM_Agent.Properties.Resources.UPLOAD_TAB1_HEADER;
            this.tpCategorized.UseVisualStyleBackColor = true;
            // 
            // dgvCategorized
            // 
            this.dgvCategorized.AllowUserToAddRows = false;
            this.dgvCategorized.AllowUserToDeleteRows = false;
            this.dgvCategorized.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvCategorized.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCategorized.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvCat_TaskName,
            this.dgvCat_WatchFolder,
            this.dgvCat_PluginName});
            this.dgvCategorized.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCategorized.Location = new System.Drawing.Point(3, 3);
            this.dgvCategorized.Name = "dgvCategorized";
            this.dgvCategorized.RowHeadersVisible = false;
            this.dgvCategorized.RowTemplate.Height = 23;
            this.dgvCategorized.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCategorized.Size = new System.Drawing.Size(639, 261);
            this.dgvCategorized.TabIndex = 0;
            // 
            // panelTab1Buttons
            // 
            this.panelTab1Buttons.Controls.Add(this.btnCatSave);
            this.panelTab1Buttons.Controls.Add(this.btnCatRemove);
            this.panelTab1Buttons.Controls.Add(this.btnCatAdd);
            this.panelTab1Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelTab1Buttons.Location = new System.Drawing.Point(3, 264);
            this.panelTab1Buttons.Name = "panelTab1Buttons";
            this.panelTab1Buttons.Size = new System.Drawing.Size(639, 37);
            this.panelTab1Buttons.TabIndex = 1;
            // 
            // btnCatSave
            // 
            this.btnCatSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCatSave.Location = new System.Drawing.Point(559, 7);
            this.btnCatSave.Name = "btnCatSave";
            this.btnCatSave.Size = new System.Drawing.Size(75, 23);
            this.btnCatSave.TabIndex = 2;
            this.btnCatSave.Text = Properties.Resources.BUTTON_SAVE; // "저장"
            this.btnCatSave.UseVisualStyleBackColor = true;
            // 
            // btnCatRemove
            // 
            this.btnCatRemove.Location = new System.Drawing.Point(86, 7);
            this.btnCatRemove.Name = "btnCatRemove";
            this.btnCatRemove.Size = new System.Drawing.Size(75, 23);
            this.btnCatRemove.TabIndex = 1;
            this.btnCatRemove.Text = Properties.Resources.BUTTON_REMOVE; // "삭제"
            this.btnCatRemove.UseVisualStyleBackColor = true;
            // 
            // btnCatAdd
            // 
            this.btnCatAdd.Location = new System.Drawing.Point(5, 7);
            this.btnCatAdd.Name = "btnCatAdd";
            this.btnCatAdd.Size = new System.Drawing.Size(75, 23);
            this.btnCatAdd.TabIndex = 0;
            this.btnCatAdd.Text = Properties.Resources.BUTTON_ADD; // "추가"
            this.btnCatAdd.UseVisualStyleBackColor = true;
            // 
            // tpLiveMonitoring
            // 
            this.tpLiveMonitoring.Controls.Add(this.dgvLiveMonitoring);
            this.tpLiveMonitoring.Controls.Add(this.panelTab2Buttons);
            this.tpLiveMonitoring.Location = new System.Drawing.Point(4, 22);
            this.tpLiveMonitoring.Name = "tpLiveMonitoring";
            this.tpLiveMonitoring.Padding = new System.Windows.Forms.Padding(3);
            this.tpLiveMonitoring.Size = new System.Drawing.Size(645, 304);
            this.tpLiveMonitoring.TabIndex = 1;
            this.tpLiveMonitoring.Text = global::ITM_Agent.Properties.Resources.UPLOAD_TAB2_HEADER;
            this.tpLiveMonitoring.UseVisualStyleBackColor = true;
            // 
            // dgvLiveMonitoring
            // 
            this.dgvLiveMonitoring.AllowUserToAddRows = false;
            this.dgvLiveMonitoring.AllowUserToDeleteRows = false;
            this.dgvLiveMonitoring.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvLiveMonitoring.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLiveMonitoring.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.dgvLive_TaskName,
            this.dgvLive_WatchFolder,
            this.dgvLive_btnSelectFolder,
            this.dgvLive_FileFilter,
            this.dgvLive_PluginName});
            this.dgvLiveMonitoring.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvLiveMonitoring.Location = new System.Drawing.Point(3, 3);
            this.dgvLiveMonitoring.Name = "dgvLiveMonitoring";
            this.dgvLiveMonitoring.RowHeadersVisible = false;
            this.dgvLiveMonitoring.RowTemplate.Height = 23;
            this.dgvLiveMonitoring.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvLiveMonitoring.Size = new System.Drawing.Size(639, 261);
            this.dgvLiveMonitoring.TabIndex = 1;
            // 
            // dgvLive_TaskName
            // 
            this.dgvLive_TaskName.DataPropertyName = "TaskName";
            this.dgvLive_TaskName.FillWeight = 15F;
            this.dgvLive_TaskName.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_TASKNAME;
            this.dgvLive_TaskName.Name = "dgvLive_TaskName";
            // 
            // dgvLive_WatchFolder
            // 
            this.dgvLive_WatchFolder.DataPropertyName = "WatchFolder";
            this.dgvLive_WatchFolder.FillWeight = 36F;
            this.dgvLive_WatchFolder.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_LIVE_FOLDER;
            this.dgvLive_WatchFolder.Name = "dgvLive_WatchFolder";
            // 
            // dgvLive_btnSelectFolder
            // 
            this.dgvLive_btnSelectFolder.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.dgvLive_btnSelectFolder.FillWeight = 5F;
            this.dgvLive_btnSelectFolder.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_SELECT;
            this.dgvLive_btnSelectFolder.Name = "dgvLive_btnSelectFolder";
            this.dgvLive_btnSelectFolder.Text = "...";
            this.dgvLive_btnSelectFolder.UseColumnTextForButtonValue = true;
            this.dgvLive_btnSelectFolder.Width = 40;
            // 
            // dgvLive_FileFilter
            // 
            this.dgvLive_FileFilter.DataPropertyName = "FileFilter";
            this.dgvLive_FileFilter.FillWeight = 20F;
            this.dgvLive_FileFilter.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_FILTER;
            this.dgvLive_FileFilter.Name = "dgvLive_FileFilter";
            // 
            // dgvLive_PluginName
            // 
            this.dgvLive_PluginName.DataPropertyName = "PluginName";
            this.dgvLive_PluginName.FillWeight = 28F;
            this.dgvLive_PluginName.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_PLUGIN;
            this.dgvLive_PluginName.Name = "dgvLive_PluginName";
            this.dgvLive_PluginName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // panelTab2Buttons
            // 
            this.panelTab2Buttons.Controls.Add(this.btnLiveSave);
            this.panelTab2Buttons.Controls.Add(this.btnLiveRemove);
            this.panelTab2Buttons.Controls.Add(this.btnLiveAdd);
            this.panelTab2Buttons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelTab2Buttons.Location = new System.Drawing.Point(3, 264);
            this.panelTab2Buttons.Name = "panelTab2Buttons";
            this.panelTab2Buttons.Size = new System.Drawing.Size(639, 37);
            this.panelTab2Buttons.TabIndex = 2;
            // 
            // btnLiveSave
            // 
            this.btnLiveSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnLiveSave.Location = new System.Drawing.Point(559, 7);
            this.btnLiveSave.Name = "btnLiveSave";
            this.btnLiveSave.Size = new System.Drawing.Size(75, 23);
            this.btnLiveSave.TabIndex = 5;
            this.btnLiveSave.Text = Properties.Resources.BUTTON_SAVE; // "저장"
            this.btnLiveSave.UseVisualStyleBackColor = true;
            // 
            // btnLiveRemove
            // 
            this.btnLiveRemove.Location = new System.Drawing.Point(86, 7);
            this.btnLiveRemove.Name = "btnLiveRemove";
            this.btnLiveRemove.Size = new System.Drawing.Size(75, 23);
            this.btnLiveRemove.TabIndex = 4;
            this.btnLiveRemove.Text = Properties.Resources.BUTTON_REMOVE; // "삭제"
            this.btnLiveRemove.UseVisualStyleBackColor = true;
            // 
            // btnLiveAdd
            // 
            this.btnLiveAdd.Location = new System.Drawing.Point(5, 7);
            this.btnLiveAdd.Name = "btnLiveAdd";
            this.btnLiveAdd.Size = new System.Drawing.Size(75, 23);
            this.btnLiveAdd.TabIndex = 3;
            this.btnLiveAdd.Text = Properties.Resources.BUTTON_ADD; // "추가"
            this.btnLiveAdd.UseVisualStyleBackColor = true;
            // 
            // dgvCat_TaskName
            // 
            this.dgvCat_TaskName.DataPropertyName = "TaskName";
            this.dgvCat_TaskName.FillWeight = 14F;
            this.dgvCat_TaskName.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_TASKNAME;
            this.dgvCat_TaskName.Name = "dgvCat_TaskName";
            // 
            // dgvCat_WatchFolder
            // 
            this.dgvCat_WatchFolder.DataPropertyName = "WatchFolder";
            this.dgvCat_WatchFolder.FillWeight = 59F;
            this.dgvCat_WatchFolder.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_CAT_FOLDER;
            this.dgvCat_WatchFolder.Name = "dgvCat_WatchFolder";
            this.dgvCat_WatchFolder.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvCat_WatchFolder.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // dgvCat_PluginName
            // 
            this.dgvCat_PluginName.DataPropertyName = "PluginName";
            this.dgvCat_PluginName.FillWeight = 27F;
            this.dgvCat_PluginName.HeaderText = global::ITM_Agent.Properties.Resources.UPLOAD_COL_PLUGIN;
            this.dgvCat_PluginName.Name = "dgvCat_PluginName";
            this.dgvCat_PluginName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvCat_PluginName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
            // 
            // ucOntoUploadPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControlUpload);
            this.Name = "ucOntoUploadPanel";
            this.Size = new System.Drawing.Size(676, 340);
            this.tabControlUpload.ResumeLayout(false);
            this.tpCategorized.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCategorized)).EndInit();
            this.panelTab1Buttons.ResumeLayout(false);
            this.tpLiveMonitoring.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLiveMonitoring)).EndInit();
            this.panelTab2Buttons.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvLive_TaskName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvLive_WatchFolder;
        private System.Windows.Forms.DataGridViewButtonColumn dgvLive_btnSelectFolder;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvLive_FileFilter;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvLive_PluginName;
        private System.Windows.Forms.DataGridViewTextBoxColumn dgvCat_TaskName;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvCat_WatchFolder;
        private System.Windows.Forms.DataGridViewComboBoxColumn dgvCat_PluginName;
    }
}
