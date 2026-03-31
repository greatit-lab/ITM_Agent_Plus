// ITM_Agent_Plus/MainForm.cs
using ITM_Agent.Services;
using ITM_Agent.Startup;
using ITM_Agent.ucPanel;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ITM_Agent
{
    public partial class MainForm : Form
    {
        private bool isExiting = false;
        private SettingsManager settingsManager;
        private LogManager logManager;
        
        // --- Common Services ---
        private FileWatcherManager fileWatcherManager;
        private EqpidManager eqpidManager;
        private InfoRetentionCleaner infoCleaner;
        private ConfigUpdateService configUpdateService;
        private ServerConnectionManager serverConnectionManager;

        // --- Onto Specific Services ---
        private OntoLampLifeService ontoLampLifeService;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem titleItem;
        private ToolStripMenuItem runItem;
        private ToolStripMenuItem stopItem;
        private ToolStripMenuItem quitItem;

        internal static string VersionInfo
        {
            get
            {
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                    return $"v{fvi.FileVersion}";
                }
                catch
                {
                    return "vUnknown";
                }
            }
        }

        ucPanel.ucConfigurationPanel ucSc1;

        // --- Common Panels ---
        private ucConfigurationPanel ucConfigPanel;
        private ucOptionPanel ucOptionPanel;
        private ucPluginPanel ucPluginPanel;

        // --- Onto Specific Panels ---
        private ucOntoOverrideNamesPanel ucOntoOverrideNamesPanel;
        private ucOntoImageTransPanel ucOntoImageTransPanel;
        private ucOntoUploadPanel ucOntoUploadPanel;
        private ucOntoLampLifePanel ucOntoLampLifePanel;

        private bool isRunning = false; 
        private bool isDebugMode = false;   

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PerformanceWarmUp.Run();
        }

        public MainForm(SettingsManager settingsManager)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            InitializeComponent();

            this.HandleCreated += (sender, e) => UpdateMainStatus("Stopped", Color.Red);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            logManager = new LogManager(baseDir);

            // Onto 전용 서비스 초기화
            ontoLampLifeService = new OntoLampLifeService(this.settingsManager, this.logManager, this);

            InitializeUserControls();
            RegisterMenuEvents();

            ucOntoImageTransPanel.ImageSaveFolderChanged += ucOntoUploadPanel.LoadImageSaveFolder_PathChanged;

            ucSc1 = new ucPanel.ucConfigurationPanel(settingsManager);
            
            fileWatcherManager = new FileWatcherManager(settingsManager, logManager, isDebugMode);

            eqpidManager = new EqpidManager(settingsManager, logManager, VersionInfo);
            eqpidManager.InitializeEqpid();

            string eqpid = settingsManager.GetEqpid();
            if (!string.IsNullOrEmpty(eqpid))
            {
                ProceedWithMainFunctionality(eqpid);
                configUpdateService = new ConfigUpdateService(settingsManager, logManager, this, eqpid);
            }

            infoCleaner = new InfoRetentionCleaner(settingsManager);

            serverConnectionManager = new ServerConnectionManager(logManager);
            serverConnectionManager.ConnectionStatusChanged += OnServerConnectionStatusChanged;

            SetFormIcon();

            this.Text = $"ITM Agent - {VersionInfo}";
            this.MaximizeBox = false;

            InitializeTrayIcon();
            this.FormClosing += MainForm_FormClosing;

            fileWatcherManager.InitializeWatchers();

            btn_Run.Click += btn_Run_Click;
            btn_Stop.Click += btn_Stop_Click;

            UpdateUIBasedOnSettings();
        }

        private void OnServerConnectionStatusChanged(bool isConnected, bool isDbOk, bool isFtpOk, string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnServerConnectionStatusChanged(isConnected, isDbOk, isFtpOk, message)));
                return;
            }

            logManager.LogEvent($"[MainForm] Server Status Update: DB={isDbOk}, API={isFtpOk} ({message})");

            ucOptionPanel?.SetDirectConnectionStatus(isDbOk, isFtpOk);

            if (isDbOk)
            {
                PerformanceDbWriter.Start(lb_eqpid.Text, eqpidManager);
                ontoLampLifeService?.Start(true);
            }
            else
            {
                PerformanceDbWriter.Stop();
                ontoLampLifeService?.Stop();
            }

            if (isConnected)
            {
                if (ts_Status.Text != "Running (Recovered)")
                {
                    UpdateMainStatus("Running (Recovered)", Color.Blue);
                    fileWatcherManager?.ResumeWatching();
                    ucOntoUploadPanel?.ResumeWatching();
                    Task.Run(() => fileWatcherManager?.StartRecoveryScan());
                }
            }
            else
            {
                if (!statusStrip1.Text.StartsWith("Holding"))
                {
                    UpdateMainStatus("Holding (Unstable Connection)", Color.Red);
                    fileWatcherManager?.PauseWatching();
                    ucOntoUploadPanel?.PauseWatching();
                }
            }
        }

        private void SetFormIcon()
        {
            this.Icon = new Icon(@"Resources\Icons\icon.ico");
        }

        private void ProceedWithMainFunctionality(string eqpid)
        {
            lb_eqpid.Text = $"Eqpid: {eqpid}";
        }

        private void InitializeTrayIcon()
        {
            trayMenu = new ContextMenuStrip();

            titleItem = new ToolStripMenuItem(this.Text);
            titleItem.Click += (sender, e) => RestoreMainForm();
            trayMenu.Items.Add(titleItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            runItem = new ToolStripMenuItem("Run", null, Tray_Run_Click);
            stopItem = new ToolStripMenuItem("Stop", null, Tray_Stop_Click);
            quitItem = new ToolStripMenuItem("Quit", null, Tray_Quit_Click);

            trayMenu.Items.AddRange(new ToolStripItem[] { runItem, stopItem, quitItem });

            trayIcon = new NotifyIcon
            {
                Icon = new Icon(@"Resources\Icons\icon.ico"),
                ContextMenuStrip = trayMenu,
                Visible = true,
                Text = this.Text
            };
            trayIcon.DoubleClick += (sender, e) => RestoreMainForm();
        }

        private void Tray_Run_Click(object sender, EventArgs e)
        {
            if (btn_Run.Enabled)
                btn_Run_Click(sender, e);
        }

        private void Tray_Stop_Click(object sender, EventArgs e)
        {
            if (btn_Stop.Enabled)
                btn_Stop_Click(sender, e);
        }

        private void Tray_Quit_Click(object sender, EventArgs e)
        {
            if (btn_Quit.Enabled)
                btn_Quit_Click(sender, e);
        }

        private void RestoreMainForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
            titleItem.Enabled = false;
        }

        private void UpdateTrayMenuStatus()
        {
            if (runItem != null) runItem.Enabled = btn_Run.Enabled;
            if (stopItem != null) stopItem.Enabled = btn_Stop.Enabled;
            if (quitItem != null) quitItem.Enabled = btn_Quit.Enabled;
        }

        public void ShowTemporarilyForAutomation()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowTemporarilyForAutomation()));
                return;
            }

            this.TopMost = true;

            if (!this.Visible)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }

            this.Activate();
            this.BringToFront();
        }

        public void HideToTrayAfterAutomation()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => HideToTrayAfterAutomation()));
                return;
            }
            this.Hide();
            this.TopMost = false;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isExiting)
            {
                e.Cancel = true;
                this.Hide();
                trayIcon.BalloonTipTitle = "ITM Agent";
                trayIcon.BalloonTipText = "ITM Agent가 백그라운드에서 실행 중입니다.";
                trayIcon.ShowBalloonTip(3000);
                return;
            }

            if (!isExiting)
            {
                e.Cancel = true;
                isExiting = true;
                PerformQuit();
            }
        }

        private void UpdateUIBasedOnSettings()
        {
            if (settingsManager.IsReadyToRun())
            {
                UpdateMainStatus("Ready to Run", Color.Green);
                btn_Run.Enabled = true;
            }
            else
            {
                UpdateMainStatus("Stopped!", Color.Red);
                btn_Run.Enabled = false;
            }
            btn_Stop.Enabled = false;
            btn_Quit.Enabled = true;
        }

        private void UpdateMainStatus(string status, Color color)
        {
            ts_Status.Text = status;
            ts_Status.ForeColor = color;

            bool isActiveRunning = status.StartsWith("Running") || status.StartsWith("Holding");

            ucConfigPanel?.UpdateStatusOnRun(isActiveRunning);
            ucOptionPanel?.UpdateStatusOnRun(isActiveRunning);
            ucPluginPanel?.UpdateStatusOnRun(isActiveRunning);

            // Onto 전용 패널 상태 업데이트
            ucOntoOverrideNamesPanel?.UpdateStatus(status);
            ucOntoOverrideNamesPanel?.UpdateStatusOnRun(isActiveRunning);
            ucOntoImageTransPanel?.UpdateStatusOnRun(isActiveRunning);
            ucOntoUploadPanel?.UpdateStatusOnRun(isActiveRunning);
            ucOntoLampLifePanel?.UpdateStatusOnRun(isActiveRunning);

            logManager.LogEvent($"Status updated to: {status}");
            if (isDebugMode)
                logManager.LogDebug($"Status updated to: {status}. Running state: {isActiveRunning}");

            if (status == "Stopped!")
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = true;
            }
            else if (status == "Ready to Run")
            {
                btn_Run.Enabled = true;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = true;
            }
            else if (isActiveRunning)
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = true; 
                btn_Quit.Enabled = false;
            }
            else
            {
                btn_Run.Enabled = false;
                btn_Stop.Enabled = false;
                btn_Quit.Enabled = false;
            }

            UpdateTrayMenuStatus();
            UpdateMenuItemsState(isActiveRunning);
            UpdateButtonsState();
        }

        private void UpdateMenuItemsState(bool isRunning)
        {
            if (menuStrip1 != null)
            {
                foreach (ToolStripMenuItem item in menuStrip1.Items)
                {
                    if (item.Text == "File")
                    {
                        foreach (ToolStripItem subItem in item.DropDownItems)
                        {
                            if (subItem.Text == "New" || subItem.Text == "Open" || subItem.Text == "Quit")
                            {
                                subItem.Enabled = !isRunning;
                            }
                        }
                    }
                }
            }
        }

        private void btn_Run_Click(object sender, EventArgs e)
        {
            logManager.LogEvent("Run button clicked.");
            PerformRunLogic();
        }

        private void PerformRunLogic()
        {
            if (ucOntoUploadPanel != null)
            {
                string validationError;
                if (ucOntoUploadPanel.HasInvalidRules(out validationError))
                {
                    MessageBox.Show($"실행할 수 없습니다. Upload 패널 설정을 확인하세요.\n\n{validationError}",
                                    "실행 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    logManager.LogEvent($"Run blocked: {validationError}");
                    return;
                }
            }

            try
            {
                if (eqpidManager != null)
                {
                    eqpidManager.InitializeEqpid();
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"Error updating agent_info during Run logic: {ex.Message}");
            }

            try
            {
                fileWatcherManager.StartWatching();
                ucOntoUploadPanel?.UpdateStatusOnRun(true);

                PerformanceDbWriter.Start(lb_eqpid.Text, this.eqpidManager);
                ontoLampLifeService.Start(false);

                serverConnectionManager.Start();

                isRunning = true;
                UpdateMainStatus("Running...", Color.Blue);

                if (isDebugMode)
                {
                    logManager.LogDebug("FileWatcherManager and ucUploadPanel Watchers started successfully.");
                }

                if (settingsManager.AutoRunOnStart)
                {
                    logManager.LogEvent("[MainForm] AutoRun successful. Resetting flag and confirming update.");
                    settingsManager.AutoRunOnStart = false;

                    if (configUpdateService != null)
                    {
                        _ = configUpdateService.ConfirmUpdateSuccessAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"Error starting monitoring: {ex.Message}");
                UpdateMainStatus("Stopped!", Color.Red);
            }
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "프로그램을 중지하시겠습니까?\n모든 파일 감시 및 업로드 기능이 중단됩니다.",
                "작업 중지 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                logManager.LogEvent("Stop button clicked and confirmed.");
                PerformStopLogic();
            }
            else
            {
                logManager.LogEvent("Stop action was canceled by the user.");
            }
        }

        private void PerformStopLogic()
        {
            try
            {
                serverConnectionManager.Stop();

                fileWatcherManager.StopWatchers();
                ucOntoUploadPanel?.UpdateStatusOnRun(false);

                PerformanceDbWriter.Stop();
                ontoLampLifeService.Stop();

                isRunning = false;

                bool isReady = ucConfigPanel?.IsReadyToRun() ?? false;
                UpdateMainStatus(isReady ? "Ready to Run" : "Stopped!",
                                 isReady ? Color.Green : Color.Red);

                if (isDebugMode)
                    logManager.LogDebug("FileWatcherManager & Watchers stopped successfully.");
            }
            catch (Exception ex)
            {
                logManager.LogError($"Error stopping processes: {ex.Message}");
                UpdateMainStatus("Error Stopping!", Color.Red);
            }
        }

        private void UpdateButtonsState()
        {
            UpdateTrayMenuStatus();
        }

        private void btn_Quit_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "프로그램을 완전히 종료하시겠습니까?",
                "종료 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                PerformQuit();
            }
        }

        private void PerformQuit()
        {
            logManager?.LogEvent("[MainForm] Quit requested.");

            try
            {
                fileWatcherManager?.StopWatchers();
                fileWatcherManager = null;

                ontoLampLifeService?.Stop();
                ontoLampLifeService = null;

                configUpdateService?.Dispose();
                configUpdateService = null;

                serverConnectionManager?.Dispose();
                serverConnectionManager = null;

                infoCleaner?.Dispose();
                infoCleaner = null;

                PerformanceDbWriter.Stop();

                settingsManager = null;
            }
            catch (Exception ex)
            {
                logManager?.LogError($"[MainForm] Clean-up error during service stop: {ex}");
            }

            try
            {
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                    trayIcon = null;
                }
                trayMenu?.Dispose();
                trayMenu = null;
            }
            catch (Exception ex)
            {
                logManager?.LogError($"[MainForm] Tray clean-up error: {ex}");
            }

            try { infoCleaner?.Dispose(); } catch { /* ignore */ }

            BeginInvoke(new Action(() =>
            {
                logManager?.LogEvent("[MainForm] Application.Exit invoked.");
                Application.Exit();
            }));
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            pMain.Controls.Add(ucSc1);
            UpdateMenusBasedOnType();

            ShowUserControl(ucConfigPanel);

            bool isReady = ucConfigPanel.IsReadyToRun();

            if (settingsManager.AutoRunOnStart && isReady)
            {
                logManager.LogEvent("[MainForm] AutoRunOnStart=1 detected on load. Starting Run logic...");
                this.BeginInvoke(new Action(() => {
                    PerformRunLogic();
                }));
            }
            else
            {
                if (isReady)
                {
                    UpdateMainStatus("Ready to Run", Color.Green);
                }
                else
                {
                    UpdateMainStatus("Stopped!", Color.Red);
                }
            }
        }

        private void RefreshUI()
        {
            string eqpid = settingsManager.GetEqpid();
            lb_eqpid.Text = $"Eqpid: {eqpid}";

            ucSc1.RefreshUI();
            ucConfigPanel?.RefreshUI();
            ucOntoOverrideNamesPanel?.RefreshUI();

            UpdateUIBasedOnSettings();
        }

        private void NewMenuItem_Click(object sender, EventArgs e)
        {
            settingsManager.ResetExceptEqpid();
            MessageBox.Show("Settings 초기화 완료 (Eqpid 제외)", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);

            RefreshUI();
        }

        private void OpenMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        settingsManager.LoadFromFile(openFileDialog.FileName);
                        MessageBox.Show("새로운 Settings.ini 파일이 로드되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        RefreshUI();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일 로드 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveAsMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "INI files (*.ini)|*.ini|All files (*.*)|*.*";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        settingsManager.SaveToFile(saveFileDialog.FileName);
                        MessageBox.Show("Settings.ini가 저장되었습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일 저장 중 오류 발생: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            btn_Quit.PerformClick();
        }

        private void InitializeUserControls()
        {
            // Common 패널 생성
            ucConfigPanel = new ucConfigurationPanel(settingsManager);
            ucPluginPanel = new ucPluginPanel(settingsManager);
            ucOptionPanel = new ucOptionPanel(settingsManager);
            ucOptionPanel.DebugModeChanged += OptionPanel_DebugModeChanged;

            // Onto 전용 패널 생성
            ucOntoOverrideNamesPanel = new ucOntoOverrideNamesPanel(
                settingsManager, ucConfigPanel, logManager, settingsManager.IsDebugMode);
            ucOntoImageTransPanel = new ucOntoImageTransPanel(settingsManager, ucConfigPanel);
            ucOntoUploadPanel = new ucOntoUploadPanel(
                ucConfigPanel, ucPluginPanel, settingsManager, ucOntoOverrideNamesPanel, ucOntoImageTransPanel);
            ucOntoLampLifePanel = new ucOntoLampLifePanel(settingsManager, ontoLampLifeService);

            this.Controls.Add(ucOntoOverrideNamesPanel);

            // 초기화
            ucConfigPanel.InitializePanel(isRunning);
            ucPluginPanel.InitializePanel(isRunning);
            ucOptionPanel.InitializePanel(isRunning);
            
            ucOntoOverrideNamesPanel.InitializePanel(isRunning);
        }

        private void RegisterMenuEvents()
        {
            // Common 메뉴
            tsm_Categorize.Click += (s, e) => ShowUserControl(ucConfigPanel);
            tsm_Option.Click += (s, e) => ShowUserControl(ucOptionPanel);
            tsm_PluginList.Click += (s, e) => ShowUserControl(ucPluginPanel);
            tsm_AboutInfo.Click += tsm_AboutInfo_Click;

            // ONTO 전용 메뉴
            tsm_OverrideNames.Click += (s, e) => ShowUserControl(ucOntoOverrideNamesPanel);
            tsm_ImageTrans.Click += (s, e) => ShowUserControl(ucOntoImageTransPanel);
            tsm_UploadData.Click += (s, e) => ShowUserControl(ucOntoUploadPanel);
            tsm_LampLifeCollector.Click += (s, e) => ShowUserControl(ucOntoLampLifePanel);

            // NOVA 전용 메뉴 (구현 예정)
            toolStripMenuItem4.Click += (s, e) => MessageBox.Show("Nova Override Names 로직은 아직 구현되지 않았습니다.", "알림");
            toolStripMenuItem5.Click += (s, e) => MessageBox.Show("Nova Image Trans 로직은 아직 구현되지 않았습니다.", "알림");
            toolStripMenuItem6.Click += (s, e) => MessageBox.Show("Nova Upload Data 로직은 아직 구현되지 않았습니다.", "알림");
        }

        private void OptionPanel_DebugModeChanged(bool isDebug)
        {
            isDebugMode = isDebug;
            fileWatcherManager.UpdateDebugMode(isDebugMode);

            if (isDebugMode)
            {
                logManager.LogEvent("Debug Mode: Enabled");
                logManager.LogDebug("debug mode enabled.");
            }
            else
            {
                logManager.LogEvent("Debug Mode: Disabled");
                logManager.LogDebug("debug mode disabled.");
            }
        }

        private void ShowUserControl(UserControl control)
        {
            pMain.Controls.Clear();
            pMain.Controls.Add(control);
            control.Dock = DockStyle.Fill;

            if (control is ucConfigurationPanel cfg) cfg.InitializePanel(isRunning);
            else if (control is ucOptionPanel opt) opt.InitializePanel(isRunning);
            else if (control is ucPluginPanel plg) plg.InitializePanel(isRunning);
            else if (control is ucOntoOverrideNamesPanel ov) ov.InitializePanel(isRunning);
            else if (control is ucOntoUploadPanel upload) upload.InitializePanel(isRunning); 

            if (control == ucOptionPanel)
            {
                ucOptionPanel.ActivatePanel();
            }
            else
            {
                ucOptionPanel?.DeactivatePanel();
            }
        }

        private void UpdateMenusBasedOnType()
        {
            string type = settingsManager.GetEqpType();
            if (type == "ONTO")
            {
                tsm_Nova.Visible = false;
                tsm_Onto.Visible = true;
            }
            else if (type == "NOVA")
            {
                tsm_Onto.Visible = false;
                tsm_Nova.Visible = true;
            }
            else
            {
                tsm_Onto.Visible = false;
                tsm_Nova.Visible = false;
                return;
            }

            tsm_Onto.Visible = type.Equals("ONTO", StringComparison.OrdinalIgnoreCase);
            tsm_Nova.Visible = type.Equals("NOVA", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeMainMenu()
        {
            UpdateMenusBasedOnType();
        }

        public MainForm()
            : this(new SettingsManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.ini")))
        {
        }

        private void tsm_AboutInfo_Click(object sender, EventArgs e)
        {
            using (var dlg = new AboutInfoForm())
            {
                dlg.ShowDialog(this);
            }
        }

        public void TriggerRestartCycle()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => TriggerRestartCycle()));
                return;
            }

            _ = RestartAsync();
        }

        private async Task RestartAsync()
        {
            logManager.LogEvent("[MainForm] RestartCycle triggered by ConfigUpdateService.");

            if (btn_Stop.Enabled)
            {
                logManager.LogEvent("[MainForm] Calling Stop logic...");
                PerformStopLogic();
            }

            logManager.LogEvent("[MainForm] Waiting 10 seconds before auto-run...");

            await Task.Delay(10000);

            if (btn_Run.Enabled)
            {
                logManager.LogEvent("[MainForm] Calling Run logic (AutoRun)...");
                PerformRunLogic();
            }
            else
            {
                logManager.LogEvent("[MainForm] AutoRun canceled (Button disabled or settings invalid).");
            }
        }
    }
}
