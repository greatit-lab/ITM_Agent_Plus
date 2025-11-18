// ITM_Agent/ucPanel/ucOptionPanel.cs
using System;
using System.Windows.Forms;
using ITM_Agent.Services;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using ConnectInfo;
using System.Net.Sockets;
using System.Drawing.Drawing2D; // [추가] GraphicsPath 사용

namespace ITM_Agent.ucPanel
{
    /// <summary>
    /// MenuStrip1 → tsm_Option 클릭 시 표시되는 옵션(UserControl)  
    /// Debug Mode 체크 상태를 SettingsManager · LogManager · MainForm 에 즉시 전파합니다.
    /// </summary>
    public partial class ucOptionPanel : UserControl
    {
        private bool isRunning = false;
        private const string OptSection = "Option";
        private const string Key_PerfLog = "EnablePerfoLog";
        private const string Key_InfoAutoDel = "EnableInfoAutoDel";
        private const string Key_InfoRetention = "InfoRetentionDays";

        private readonly SettingsManager settingsManager;

        public event Action<bool> DebugModeChanged;

        // 동시 새로고침 방지 플래그
        private bool _isRefreshing = false;
        private readonly object _refreshLock = new object();

        public ucOptionPanel(SettingsManager settings)
        {
            this.settingsManager = settings ?? throw new ArgumentNullException(nameof(settings));
            InitializeComponent();

            /* 1) Retention 콤보박스 고정 값 • DropDownList */
            cb_info_Retention.Items.Clear();
            cb_info_Retention.Items.AddRange(new object[] { "1", "3", "5" });
            cb_info_Retention.DropDownStyle = ComboBoxStyle.DropDownList;

            /* 2) UI 기본 비활성화 */
            UpdateRetentionControls(false);

            /* Settings.ini ↔ UI 동기화 */
            chk_infoDel.Checked = settingsManager.IsInfoDeletionEnabled;
            cb_info_Retention.Enabled = label3.Enabled = label4.Enabled = chk_infoDel.Checked;
            if (chk_infoDel.Checked)
            {
                string d = settingsManager.InfoRetentionDays.ToString();
                cb_info_Retention.SelectedItem = cb_info_Retention.Items.Contains(d) ? d : "1";
            }

            /* 3) 이벤트 연결 */
            chk_PerfoMode.CheckedChanged += chk_PerfoMode_CheckedChanged;
            chk_infoDel.CheckedChanged += chk_infoDel_CheckedChanged;
            cb_info_Retention.SelectedIndexChanged += cb_info_Retention_SelectedIndexChanged;

            /* 4) Settings.ini → UI 복원 */
            LoadOptionSettings();

            // VisibleChanged 이벤트를 제거하고 수동 버튼 클릭만 남김
            this.btnRefreshStatus.Click += BtnRefreshStatus_Click;

            // PictureBox를 원형으로 만들기 위한 Paint 이벤트 핸들러 연결
            this.pbDbStatus.Paint += PbStatus_Paint;
            this.pbObjStatus.Paint += PbStatus_Paint;

            // [추가] 타이머는 처음에 중지 상태로 시작 (ActivatePanel에서 제어)
            this.statusRefreshTimer.Stop();
        }

        #region ====== Run 상태 동기화 ======
        /// <summary>Run/Stop 상태에 따라 모든 입력 컨트롤 Enable 토글</summary>
        private void SetControlsEnabled(bool enabled)
        {
            chk_DebugMode.Enabled = enabled;
            chk_PerfoMode.Enabled = enabled;
            chk_infoDel.Enabled = enabled;

            /* Retention-관련 컨트롤은 ‘Info Delete’ 체크 여부와 동기화 */
            UpdateRetentionControls(enabled && chk_infoDel.Checked);

            // [수정] 새로고침 버튼은 _isRefreshing 플래그에 따라 활성화/비활성화
            btnRefreshStatus.Enabled = !_isRefreshing;
        }

        /// <summary>MainForm 에서 Run/Stop 전환 시 호출</summary>
        public void UpdateStatusOnRun(bool isRunning)
        {
            this.isRunning = isRunning;
            SetControlsEnabled(!isRunning);
        }

        /// <summary>처음 패널 로드 또는 화면 전환 시 상태 맞춤</summary>
        public void InitializePanel(bool isRunning)
        {
            this.isRunning = isRunning;
            SetControlsEnabled(!isRunning);
        }
        #endregion

        private void LoadOptionSettings()
        {
            // DebugMode 는 기존 로직 유지
            chk_DebugMode.Checked = settingsManager.IsDebugMode;

            /* Perf-Log */
            bool perf = settingsManager.GetValueFromSection(OptSection, Key_PerfLog) == "1";
            chk_PerfoMode.Checked = perf;

            /* Info 자동 삭제 */
            bool infoDel = settingsManager.GetValueFromSection(OptSection, Key_InfoAutoDel) == "1";
            chk_infoDel.Checked = infoDel;

            /* Retention 일수 */
            string days = settingsManager.GetValueFromSection(OptSection, Key_InfoRetention);
            if (days == "1" || days == "3" || days == "5")
                cb_info_Retention.SelectedItem = days;

            /* UI 동기화 */
            UpdateRetentionControls(infoDel);
        }

        private void UpdateRetentionControls(bool enableCombo)
        {
            /* 콤보박스는 “Run 중이 아님” && Delete 기능 ON 일 때만 선택 가능 */
            cb_info_Retention.Enabled = enableCombo && !isRunning; // [수정] isRunning 조건 반영

            /* 라벨은 Run 상태 무관, 오직 chk_infoDel 체크 여부로만 활성화 */
            label3.Enabled = label4.Enabled = chk_infoDel.Checked;
        }

        private void chk_PerfoMode_CheckedChanged(object sender, EventArgs e)
        {
            bool enable = chk_PerfoMode.Checked;
            PerformanceMonitor.Instance.StartSampling();
            PerformanceMonitor.Instance.SetFileLogging(enable);

            settingsManager.IsPerformanceLogging = enable;
        }

        private void chk_infoDel_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chk_infoDel.Checked;

            /* Settings 동기화 */
            settingsManager.IsInfoDeletionEnabled = enabled;
            settingsManager.InfoRetentionDays = enabled
                                                    ? int.Parse(cb_info_Retention.SelectedItem?.ToString() ?? "1")
                                                    : 0;

            /* 라벨·콤보박스 Enable 상태 반영 */
            UpdateRetentionControls(enabled && !isRunning);

            if (enabled)
            {
                if (cb_info_Retention.SelectedIndex < 0)
                    cb_info_Retention.SelectedItem = "1";
            }
            else
            {
                cb_info_Retention.SelectedIndex = -1;
            }
        }

        private void cb_info_Retention_SelectedIndexChanged(object s, EventArgs e)
        {
            if (!chk_infoDel.Checked) return;                  // Info-삭제 기능 Off 시 무시

            object item = cb_info_Retention.SelectedItem;
            if (item == null)     // 선택 해제 상태
                return;

            if (int.TryParse(item.ToString(), out int days))   // 파싱 안전 처리
                settingsManager.InfoRetentionDays = days;
        }

        private void chk_DebugMode_CheckedChanged(object sender, EventArgs e)
        {
            bool isDebug = chk_DebugMode.Checked;

            // ① Settings.ini 동기화
            settingsManager.IsDebugMode = isDebug;

            // ② 메인 로거 전역 플래그
            LogManager.GlobalDebugEnabled = isDebug;

            /* [추가] ③ 모든 플러그인(SimpleLogger)에 Debug 모드 일괄 전파 (플러그인명 미지정) */
            ITM_Agent.Services.LogManager.BroadcastPluginDebug(isDebug);

            // ④ MainForm 등 외부 알림(기존 이벤트 유지)
            DebugModeChanged?.Invoke(isDebug);
        }

        // IP 마스킹 헬퍼 (LogManager의 로직과 동일)
        private static readonly Regex _ipMaskRegex = new Regex(
            @"\b(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})\b",
            RegexOptions.Compiled);

        private string MaskIpAddress(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return "N/A";

            // "10.0.0.1" -> "*.*.*.1"
            return _ipMaskRegex.Replace(ip, "*.*.*.$4");
        }

        private string ExtractHostFromConnectionString(string cs)
        {
            try
            {
                var builder = new NpgsqlConnectionStringBuilder(cs);
                return builder.Host;
            }
            catch { return "Invalid CS"; }
        }

        // PictureBox를 원형으로 그리는 이벤트 핸들러
        private void PbStatus_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = sender as PictureBox;
            if (pb == null) return;

            // PictureBox의 영역을 원형으로 설정
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(0, 0, pb.Width - 1, pb.Height - 1);
                pb.Region = new Region(gp);
            }

            // 원형 배경색 채우기
            using (SolidBrush brush = new SolidBrush(pb.BackColor))
            {
                e.Graphics.FillEllipse(brush, 0, 0, pb.Width - 1, pb.Height - 1);
            }

            // 원형 테두리 그리기
            using (Pen pen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawEllipse(pen, 0, 0, pb.Width - 1, pb.Height - 1);
            }
        }

        // 연결 상태 확인 로직 (타이머 및 중복 실행 방지)
        private void BtnRefreshStatus_Click(object sender, EventArgs e)
        {
            // 수동 새로고침 (강제 실행)
            _ = RefreshStatusAsync(true);
        }

        /// <summary>
        /// [추가] MainForm이 이 패널을 표시할 때 호출하는 메서드
        /// </summary>
        public void ActivatePanel()
        {
            // 패널이 화면에 보일 때
            _ = RefreshStatusAsync(true);
            statusRefreshTimer.Start();
        }

        /// <summary>
        /// [추가] MainForm이 이 패널을 숨길 때 호출하는 메서드
        /// </summary>
        public void DeactivatePanel()
        {
            // 패널이 숨겨질 때
            statusRefreshTimer.Stop();
        }

        private void statusRefreshTimer_Tick(object sender, EventArgs e)
        {
            // 타이머에 의한 주기적 새로고침 (강제 실행 아님)
            _ = RefreshStatusAsync(false);
        }

        /// <summary>
        /// DB와 Object Story(FTP) 연결 상태를 비동기로 새로고침합니다.
        /// </summary>
        /// <param name="force">true일 경우 이미 새로고침 중이어도 무시하고 즉시 시작, false는 중복 방지</param>
        private async Task RefreshStatusAsync(bool force = false)
        {
            // [수정] 중복 실행 방지
            lock (_refreshLock)
            {
                // 강제 실행이 아닌데 이미 새로고침 중이면 반환
                if (_isRefreshing && !force)
                    return;

                _isRefreshing = true;
            }

            // UI 스레드에서 UI 상태 변경
            this.Invoke(new Action(() =>
            {
                pbDbStatus.BackColor = Color.Gray;
                lblDbHost.Text = "Checking...";
                pbObjStatus.BackColor = Color.Gray;
                lblObjHost.Text = "Checking...";
                btnRefreshStatus.Enabled = false; // 새로고침 버튼 비활성화
            }));

            try
            {
                // 두 작업을 병렬로 실행
                await Task.WhenAll(CheckDatabaseAsync(), CheckObjectStoryAsync());
            }
            finally
            {
                // [수정] 작업 완료 후 플래그 해제 및 버튼 활성화
                lock (_refreshLock)
                {
                    _isRefreshing = false;
                }

                // UI 스레드에서 버튼 활성화
                this.Invoke(new Action(() =>
                {
                    btnRefreshStatus.Enabled = true;
                }));
            }
        }

        /// <summary>
        /// (1) DB 연결 확인
        /// </summary>
        private async Task CheckDatabaseAsync()
        {
            string host = "N/A";
            try
            {
                // ConnectInfo.dll을 통해 현재 설정된 DB 연결 문자열을 가져옵니다.
                string cs = DatabaseInfo.CreateDefault().GetConnectionString();
                host = ExtractHostFromConnectionString(cs);

                // UI 스레드에서 IP 업데이트 (MaskIpAddress 호출)
                this.Invoke(new Action(() => lblDbHost.Text = MaskIpAddress(host)));

                using (var conn = new NpgsqlConnection(cs))
                {
                    // 5초 타임아웃 설정
                    conn.ConnectionString += ";Timeout=5";
                    await conn.OpenAsync();
                } // conn.Close()

                // UI 스레드에서 아이콘 업데이트
                this.Invoke(new Action(() => pbDbStatus.BackColor = Color.LimeGreen));
            }
            catch (Exception) // [수정] CS0168 경고 제거 (ex 변수 제거)
            {
                // 실패 시
                this.Invoke(new Action(() =>
                {
                    pbDbStatus.BackColor = Color.Red;
                    // 호스트 IP가 확인된 상태에서 접속 실패 시 IP는 표시
                    if (host != "N/A" && host != "Invalid CS")
                        lblDbHost.Text = MaskIpAddress(host);
                    else
                        lblDbHost.Text = "Connection Failed";
                }));
                // (로그는 기록하지 않음 - 단순 UI 상태 표시용)
            }
        }

        /// <summary>
        /// (2) Object Story (FTP) 연결 확인
        /// </summary>
        private async Task CheckObjectStoryAsync()
        {
            string host = "N/A";
            int port = 21; // 기본값
            try
            {
                // ConnectInfo.dll을 통해 현재 설정된 FTP 정보를 가져옵니다.
                var ftpInfo = FtpsInfo.CreateDefault();
                host = ftpInfo.Host;
                port = ftpInfo.Port; // [수정] Connection.ini에서 Port 값을 읽어옵니다.

                if (string.IsNullOrEmpty(host))
                {
                    throw new Exception("FTP Host not configured.");
                }

                this.Invoke(new Action(() => lblObjHost.Text = MaskIpAddress(host)));

                // 간단한 TCP 포트 연결 테스트 (2초 타임아웃)
                bool connected = await Task.Run(() =>
                {
                    try
                    {
                        using (var tcp = new TcpClient())
                        {
                            // [수정] 이제 이 port 변수는 Connection.ini에서 읽어온 동적인 값입니다.
                            return tcp.ConnectAsync(host, port).Wait(2000);
                        }
                    }
                    catch { return false; }
                });

                if (connected)
                {
                    this.Invoke(new Action(() => pbObjStatus.BackColor = Color.LimeGreen));
                }
                else
                {
                    throw new Exception($"Failed to connect to {host}:{port} (Timeout/Rejected)");
                }
            }
            catch (Exception) // [수정] CS0168 경고 제거 (ex 변수 제거)
            {
                // 실패 시
                this.Invoke(new Action(() =>
                {
                    pbObjStatus.BackColor = Color.Red;
                    if (host != "N/A")
                        lblObjHost.Text = MaskIpAddress(host);
                    else
                        lblObjHost.Text = "Connection Failed";
                }));
            }
        }
    }
}
