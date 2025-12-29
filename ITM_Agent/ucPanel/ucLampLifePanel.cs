// ITM_Agent/ucPanel/ucLampLifePanel.cs
using ITM_Agent.Services;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    public partial class ucLampLifePanel : UserControl
    {
        private readonly SettingsManager _settingsManager;
        private readonly LampLifeService _lampLifeService;
        private bool _isAgentRunning = false;

        public ucLampLifePanel(SettingsManager settingsManager, LampLifeService lampLifeService)
        {
            InitializeComponent();
            _settingsManager = settingsManager;
            _lampLifeService = lampLifeService;

            _lampLifeService.CollectionCompleted += OnCollectionCompleted;

            LoadSettings();
        }

        private void OnCollectionCompleted(bool success, DateTime timestamp)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateLastCollectLabel(success, timestamp)));
            }
            else
            {
                UpdateLastCollectLabel(success, timestamp);
            }
        }

        private void UpdateLastCollectLabel(bool success, DateTime timestamp)
        {
            if (success)
            {
                lblLastCollect.Text = $"Success at {timestamp:yyyy-MM-dd HH:mm:ss}";
                lblLastCollect.ForeColor = Color.Green;
            }
            else
            {
                lblLastCollect.Text = $"Failed at {timestamp:yyyy-MM-dd HH:mm:ss}";
                lblLastCollect.ForeColor = Color.Red;
            }
        }

        private void LoadSettings()
        {
            chkEnable.Checked = _settingsManager.IsLampLifeCollectorEnabled;
            numInterval.Value = _settingsManager.LampLifeCollectorInterval;
            UpdateControlsEnabled();
        }

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            // 실행 중이 아닐 때만 설정 변경 가능
            if (_isAgentRunning) return;
            _settingsManager.IsLampLifeCollectorEnabled = chkEnable.Checked;
            UpdateControlsEnabled();
        }

        private void numInterval_ValueChanged(object sender, EventArgs e)
        {
            // 실행 중이 아닐 때만 설정 변경 가능
            if (_isAgentRunning) return;
            _settingsManager.LampLifeCollectorInterval = (int)numInterval.Value;
        }

        private async void btnManualCollect_Click(object sender, EventArgs e)
        {
            btnManualCollect.Enabled = false;
            lblLastCollect.Text = "Collecting...";
            lblLastCollect.ForeColor = Color.Blue;

            try
            {
                // ▼▼▼ [수정] 변경된 메서드 이름(ExecuteUiCollectionAsync) 사용 ▼▼▼
                bool success = await _lampLifeService.ExecuteUiCollectionAsync();
                
                // (참고: LampLifeService 내부에서 CollectionCompleted 이벤트를 발생시키므로
                //  UpdateLastCollectLabel이 자동으로 호출되지만, 
                //  확실한 UI 반응을 위해 여기서도 상태 업데이트를 수행합니다.)
                UpdateLastCollectLabel(success, DateTime.Now);
            }
            catch (Exception)
            {
                UpdateLastCollectLabel(false, DateTime.Now);
            }
            finally
            {
                // Agent가 실행 중이 아닐 때만 버튼을 다시 활성화
                if (!_isAgentRunning)
                {
                    btnManualCollect.Enabled = true;
                }
            }
        }

        public void UpdateStatusOnRun(bool isRunning)
        {
            _isAgentRunning = isRunning;
            UpdateControlsEnabled();
        }

        private void UpdateControlsEnabled()
        {
            // Agent가 실행 중이 아닐 때만 설정 컨트롤들을 활성화합니다.
            bool canEditSettings = !_isAgentRunning;

            chkEnable.Enabled = canEditSettings;
            numInterval.Enabled = canEditSettings && chkEnable.Checked;

            // 수동 수집 버튼은 Agent 실행 여부와 관계 없이, 기능이 활성화되어 있으면 항상 활성화합니다.
            // 단, 클릭 시에는 비활성화되고 작업 완료 후 다시 상태에 맞게 활성화됩니다.
            btnManualCollect.Enabled = chkEnable.Checked;
        }
    }
}
