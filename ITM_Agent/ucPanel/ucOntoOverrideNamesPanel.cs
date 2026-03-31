// ITM_Agent_Plus/ucPanel/ucOntoOverrideNamesPanel.cs
using ITM_Agent.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ITM_Agent.ucPanel
{
    public partial class ucOntoOverrideNamesPanel : UserControl
    {
        private readonly SettingsManager settingsManager;
        private readonly ucConfigurationPanel configPanel;
        private readonly LogManager logManager;
        private readonly bool isDebugMode;

        private FileSystemWatcher folderWatcher;
        public event Action<string, Color> StatusUpdated;
        public event Action<string> FileRenamed;
        private FileSystemWatcher baselineWatcher;

        private readonly List<FileSystemWatcher> targetWatchers = new List<FileSystemWatcher>();
        private bool isRunning = false;

        private readonly ConcurrentDictionary<string, (string TimeInfo, string Prefix, string CInfo)> _baselineCache =
            new ConcurrentDictionary<string, (string, string, string)>(StringComparer.OrdinalIgnoreCase);

        public ucOntoOverrideNamesPanel(SettingsManager settingsManager, ucConfigurationPanel configPanel, LogManager logManager, bool isDebugMode)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.configPanel = configPanel ?? throw new ArgumentNullException(nameof(configPanel));
            this.logManager = logManager ?? throw new ArgumentNullException(nameof(logManager));
            this.isDebugMode = isDebugMode;

            InitializeComponent();

            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] 생성자 호출 - 초기화 시작");

            InitializeBaselineWatcher();
            InitializeCustomEvents();

            LoadDataFromSettings();
            LoadRegexFolderPaths();
            LoadSelectedBaseDatePath();

            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] 생성자 호출 - 초기화 완료");
        }

        #region 안정화 감지용 내부 클래스/메서드

        private void ProcessStableFile(string filePath)
        {
            try
            {
                if (!WaitForFileReady(filePath, maxRetries: 60, delayMilliseconds: 1000))
                {
                    logManager.LogEvent($"[ucOntoOverrideNamesPanel] 파일을 처리할 수 없습니다.(장기 잠김): {filePath}");
                    return;
                }

                if (File.Exists(filePath))
                {
                    DateTime? dateTimeInfo = ExtractDateTimeFromFile(filePath);
                    if (dateTimeInfo.HasValue)
                    {
                        string fileName = Path.GetFileName(filePath);
                        string infoPath = CreateBaselineInfoFile(filePath, dateTimeInfo.Value);

                        if (!string.IsNullOrEmpty(infoPath))
                        {
                            logManager.LogEvent($"[ucOntoOverrideNamesPanel] Baseline 대상 파일 감지: {fileName} -> info 파일 생성");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"[ucOntoOverrideNamesPanel] ProcessStableFile() 중 오류: {ex.Message}\n파일: {filePath}");
            }
        }

        private long GetFileSizeSafe(string filePath)
        {
            try { if (File.Exists(filePath)) { var fi = new FileInfo(filePath); return fi.Length; } }
            catch { /* 무시 */ }
            return 0;
        }

        private DateTime GetLastWriteTimeSafe(string filePath)
        {
            try { if (File.Exists(filePath)) return File.GetLastWriteTime(filePath); }
            catch { /* 무시 */ }
            return DateTime.MinValue;
        }

        #endregion

        #region 기존 로직 + FileSystemWatcher 이벤트 처리 수정

        private void InitializeCustomEvents()
        {
            logManager.LogEvent("[ucOntoOverrideNamesPanel] InitializeCustomEvents() 호출됨");
            cb_BaseDatePath.SelectedIndexChanged += cb_BaseDatePath_SelectedIndexChanged;
            btn_BaseClear.Click += btn_BaseClear_Click;
            btn_SelectFolder.Click += Btn_SelectFolder_Click;
            btn_Remove.Click += Btn_Remove_Click;
        }

        private void LoadRegexFolderPaths()
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] LoadRegexFolderPaths() 시작");
            cb_BaseDatePath.Items.Clear();
            var folderPaths = settingsManager.GetRegexList().Values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            cb_BaseDatePath.Items.AddRange(folderPaths);
            cb_BaseDatePath.SelectedIndex = -1;
            logManager.LogEvent("[ucOntoOverrideNamesPanel] 정규식 경로 목록 로드 완료");
        }

        private void LoadSelectedBaseDatePath()
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] LoadSelectedBaseDatePath() 시작");
            // [수정] 섹션명 분리
            string selectedPath = settingsManager.GetValueFromSection("OntoSelectedBaseDatePath", "Path");
            if (!string.IsNullOrEmpty(selectedPath) && cb_BaseDatePath.Items.Contains(selectedPath))
            {
                cb_BaseDatePath.SelectedItem = selectedPath;
                StartFolderWatcher(selectedPath);
            }
            logManager.LogEvent("[ucOntoOverrideNamesPanel] 저장된 BaseDatePath 로드 및 감시 시작");
        }

        private void cb_BaseDatePath_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cb_BaseDatePath.SelectedItem is string selectedPath)
            {
                // [수정] 섹션명 분리
                settingsManager.SetValueToSection("OntoSelectedBaseDatePath", "Path", selectedPath);
                StartFolderWatcher(selectedPath);
                if (settingsManager.IsDebugMode) logManager.LogDebug($"[ucOntoOverrideNamesPanel] cb_BaseDatePath_SelectedIndexChanged -> {selectedPath} 설정");
            }
        }

        private void StartFolderWatcher(string path)
        {
            folderWatcher?.Dispose();
            logManager.LogEvent($"[ucOntoOverrideNamesPanel] StartFolderWatcher() 호출 - 감시 경로: {path}");

            if (Directory.Exists(path))
            {
                folderWatcher = new FileSystemWatcher
                {
                    Path = path,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    Filter = "*.*",
                    EnableRaisingEvents = true
                };
                folderWatcher.Created += OnFileSystemEvent;
                folderWatcher.Changed += OnFileSystemEvent;
            }
            else
            {
                logManager.LogError($"[ucOntoOverrideNamesPanel] 지정된 경로가 존재하지 않습니다: {path}");
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            if (!isRunning) return;
            logManager.LogDebug($"[ucOntoOverrideNamesPanel] File event received, processing immediately: {e.FullPath}");
            ThreadPool.QueueUserWorkItem(_ => { ProcessStableFile(e.FullPath); });
        }

        private void btn_BaseClear_Click(object sender, EventArgs e)
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] btn_BaseClear_Click() - BaseDatePath 초기화");
            cb_BaseDatePath.SelectedIndex = -1;
            // [수정] 섹션명 분리
            settingsManager.RemoveSection("OntoSelectedBaseDatePath");
            folderWatcher?.Dispose();
            logManager.LogEvent("[ucOntoOverrideNamesPanel] BaseDatePath 해제 및 감시 중지");
        }

        private void Btn_SelectFolder_Click(object sender, EventArgs e)
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] Btn_SelectFolder_Click() 호출");
            var baseFolder = settingsManager.GetFoldersFromSection("[BaseFolder]").FirstOrDefault() ?? AppDomain.CurrentDomain.BaseDirectory;

            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = baseFolder;
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (!lb_TargetComparePath.Items.Contains(folderDialog.SelectedPath))
                    {
                        lb_TargetComparePath.Items.Add(folderDialog.SelectedPath);
                        UpdateTargetComparePathInSettings();
                        logManager.LogEvent($"[ucOntoOverrideNamesPanel] 새로운 비교 경로 추가: {folderDialog.SelectedPath}");
                    }
                    else
                    {
                        MessageBox.Show("해당 폴더는 이미 추가되어 있습니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void Btn_Remove_Click(object sender, EventArgs e)
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug("[ucOntoOverrideNamesPanel] Btn_Remove_Click() 호출");

            if (lb_TargetComparePath.SelectedItems.Count > 0)
            {
                if (MessageBox.Show("선택한 항목을 삭제하시겠습니까?", "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    var selectedItems = lb_TargetComparePath.SelectedItems.Cast<string>().ToList();
                    foreach (var item in selectedItems) lb_TargetComparePath.Items.Remove(item);
                    UpdateTargetComparePathInSettings();
                    logManager.LogEvent("[ucOntoOverrideNamesPanel] 선택한 비교 경로 삭제 완료");
                }
            }
            else
            {
                MessageBox.Show("삭제할 항목을 선택하세요.", "경고", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateTargetComparePathInSettings()
        {
            var folders = lb_TargetComparePath.Items.Cast<string>().ToList();
            // [수정] 섹션명 분리
            settingsManager.SetFoldersToSection("[OntoTargetComparePath]", folders);
        }

        #endregion

        #region 파일 처리 및 정보 추출

        private void RefreshBaselineCache()
        {
            string baseFolder = settingsManager.GetBaseFolder();
            if (string.IsNullOrEmpty(baseFolder)) return;

            string baselineFolder = Path.Combine(baseFolder, "Baseline");
            if (Directory.Exists(baselineFolder))
            {
                var files = Directory.GetFiles(baselineFolder, "*.info");
                var newData = ExtractBaselineData(files);

                _baselineCache.Clear();
                foreach (var kvp in newData) _baselineCache[kvp.Key] = kvp.Value;

                if (settingsManager.IsDebugMode) logManager.LogDebug($"[ucOntoOverrideNamesPanel] Baseline cache refreshed. Items: {_baselineCache.Count}");
            }
        }

        private string CreateBaselineInfoFile(string filePath, DateTime dateTime)
        {
            if (settingsManager.IsDebugMode) logManager.LogDebug($"[ucOntoOverrideNamesPanel] CreateBaselineInfoFile() 호출 - 대상: {Path.GetFileName(filePath)}");

            string baseFolder = configPanel.BaseFolderPath;
            if (string.IsNullOrEmpty(baseFolder) || !Directory.Exists(baseFolder)) return null;

            string baselineFolder = System.IO.Path.Combine(baseFolder, "Baseline");
            if (!Directory.Exists(baselineFolder))
            {
                Directory.CreateDirectory(baselineFolder);
                logManager.LogEvent($"[ucOntoOverrideNamesPanel] Baseline 폴더 생성: {baselineFolder}");
            }

            string originalName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            if (!Regex.IsMatch(originalName, @"C\dW\d+", RegexOptions.IgnoreCase))
            {
                var match = Regex.Match(originalName, @"_(\d{2})_");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int waferNum))
                {
                    originalName = originalName.Substring(0, match.Index) + $"_C5W{waferNum}_" + originalName.Substring(match.Index + match.Length);
                }
            }

            string newFileName = $"{dateTime:yyyyMMdd_HHmmss}_{originalName}.info";
            string newFilePath = System.IO.Path.Combine(baselineFolder, newFileName);

            try
            {
                if (File.Exists(newFilePath)) return newFilePath;
                using (File.Create(newFilePath)) { }
                return newFilePath;
            }
            catch (IOException)
            {
                Thread.Sleep(250);
                if (File.Exists(newFilePath)) return newFilePath;
                return null;
            }
            catch (Exception ex)
            {
                logManager.LogError($"[ucOntoOverrideNamesPanel] .info 파일 생성 중 오류: {ex.Message}");
                return null;
            }
        }

        private bool WaitForFileReady(string filePath, int maxRetries = 30, int delayMilliseconds = 500)
        {
            int retries = 0;
            while (retries < maxRetries)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return true;
                }
                catch (IOException) { Thread.Sleep(delayMilliseconds); retries++; }
                catch (UnauthorizedAccessException) { return false; }
                catch (Exception) { return false; }
            }
            return false;
        }

        private DateTime? ExtractDateTimeFromFile(string filePath)
        {
            string datePattern = @"Date and Time:\s*(\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2} (AM|PM))";
            const int maxRetries = 10;
            const int delayMs = 1000;
            const int maxBytesToRead = 8192;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(fileStream))
                    {
                        char[] buffer = new char[maxBytesToRead];
                        int charsRead = reader.Read(buffer, 0, buffer.Length);
                        string fileContent = new string(buffer, 0, charsRead);

                        Match match = Regex.Match(fileContent, datePattern);
                        if (match.Success && DateTime.TryParse(match.Groups[1].Value, out DateTime result)) return result;
                    }
                }
                catch (IOException) { if (i == maxRetries - 1) return null; }
                catch (Exception) { return null; }
                Thread.Sleep(delayMs);
            }
            return null;
        }

        #endregion

        #region BaselineWatcher & TargetWatcher 감시

        private void InitializeBaselineWatcher()
        {
            if (baselineWatcher != null)
            {
                baselineWatcher.EnableRaisingEvents = false;
                baselineWatcher.Dispose();
                baselineWatcher = null;
            }

            var baseFolder = settingsManager.GetFoldersFromSection("[BaseFolder]").FirstOrDefault();
            if (string.IsNullOrEmpty(baseFolder) || !Directory.Exists(baseFolder)) return;

            var baselineFolder = Path.Combine(baseFolder, "Baseline");
            if (!Directory.Exists(baselineFolder)) return;

            baselineWatcher = new FileSystemWatcher(baselineFolder, "*.info")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
            };

            baselineWatcher.Created += OnBaselineFileChanged;
            baselineWatcher.Changed += OnBaselineFileChanged;
            baselineWatcher.EnableRaisingEvents = true;

            logManager.LogEvent($"[ucOntoOverrideNamesPanel] BaselineWatcher 초기화 완료 - 경로: {baselineFolder}");
        }

        private void OnBaselineFileChanged(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath))
            {
                var baselineData = ExtractBaselineData(new[] { e.FullPath });
                if (baselineData.Count == 0) return;

                foreach (var kvp in baselineData) _baselineCache[kvp.Key] = kvp.Value;
                var timeInfo = baselineData.Values.First().TimeInfo;

                foreach (string targetFolder in lb_TargetComparePath.Items)
                {
                    if (!Directory.Exists(targetFolder)) continue;

                    try
                    {
                        var targetFiles = Directory.GetFiles(targetFolder, $"*{timeInfo}*_#*_*.*")
                                                   .Where(f => Regex.IsMatch(Path.GetFileName(f), @"_#\d+_"));

                        foreach (var targetFile in targetFiles)
                        {
                            try { ProcessTargetFile(targetFile, baselineData); }
                            catch (Exception innerEx) { logManager.LogError($"[ucOntoOverrideNamesPanel] 오류: {innerEx.Message}"); }
                        }
                    }
                    catch (Exception ex) { logManager.LogError($"[ucOntoOverrideNamesPanel] 스캔 오류: {ex.Message}"); }
                }
            }
        }

        private void InitializeTargetWatchers()
        {
            StopTargetWatchers();
            foreach (string folder in lb_TargetComparePath.Items)
            {
                if (!Directory.Exists(folder)) continue;
                var watcher = new FileSystemWatcher(folder)
                {
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };
                watcher.Created += OnTargetFileEvent;
                watcher.Changed += OnTargetFileEvent;
                watcher.Renamed += OnTargetFileEvent;
                watcher.EnableRaisingEvents = true;
                targetWatchers.Add(watcher);
            }
        }

        private void StopTargetWatchers()
        {
            foreach (var w in targetWatchers) { w.EnableRaisingEvents = false; w.Dispose(); }
            targetWatchers.Clear();
        }

        private void OnTargetFileEvent(object sender, FileSystemEventArgs e)
        {
            if (!isRunning) return;
            if (!Regex.IsMatch(e.Name, @"_#\d+_")) return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (!WaitForFileReady(e.FullPath, maxRetries: 20, delayMilliseconds: 500)) return;
                    ProcessTargetFile(e.FullPath, _baselineCache);
                }
                catch (Exception ex) { logManager.LogError($"[ucOntoOverrideNamesPanel] OnTargetFileEvent Error: {ex.Message}"); }
            });
        }

        private void LogFileRename(string oldPath, string newPath)
        {
            string changedFileName = Path.GetFileName(newPath);
            logManager.LogEvent($"[ucOntoOverrideNamesPanel] 파일 이름 변경: {oldPath} -> {changedFileName}");
            FileRenamed?.Invoke(newPath);
        }

        private Dictionary<string, (string TimeInfo, string Prefix, string CInfo)> ExtractBaselineData(string[] files)
        {
            var baselineData = new Dictionary<string, (string, string, string)>();
            var regex = new Regex(@"(\d{8}_\d{6})_(.+?)_(C\dW\d+|\d{2})(?:_|\.|$)", RegexOptions.IgnoreCase);

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                var match = regex.Match(fileName);
                if (match.Success)
                {
                    string timeInfo = match.Groups[1].Value;
                    string prefix = match.Groups[2].Value;
                    string cInfo = match.Groups[3].Value;

                    if (int.TryParse(cInfo, out int waferNum))
                    {
                        cInfo = $"C5W{waferNum}";
                    }

                    baselineData[fileName] = (timeInfo, prefix, cInfo);
                }
            }
            return baselineData;
        }

        private string ProcessTargetFile(string targetFile, IDictionary<string, (string TimeInfo, string Prefix, string CInfo)> baselineData)
        {
            if (!WaitForFileReady(targetFile, maxRetries: 10, delayMilliseconds: 300)) return null;

            string fileName = Path.GetFileName(targetFile);

            var targetWaferMatch = Regex.Match(fileName, @"_#(\d+)_");
            if (!targetWaferMatch.Success) return null;
            string targetWaferStr = targetWaferMatch.Value;

            var sortedData = baselineData.Values.OrderByDescending(d => d.TimeInfo).ToList();
            string cleanFileName = Regex.Replace(fileName, @"[^a-zA-Z0-9]", "").ToUpperInvariant();

            foreach (var data in sortedData)
            {
                if (!fileName.Contains(data.TimeInfo)) continue;

                string[] prefixTokens = data.Prefix.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                bool lotMatch = true;
                foreach (string token in prefixTokens)
                {
                    string cleanToken = Regex.Replace(token, @"[^a-zA-Z0-9]", "").ToUpperInvariant();
                    if (!cleanFileName.Contains(cleanToken))
                    {
                        lotMatch = false;
                        break;
                    }
                }

                if (lotMatch)
                {
                    string newName = fileName.Replace(targetWaferStr, $"_{data.CInfo}_");

                    if (newName.Equals(fileName, StringComparison.Ordinal)) continue;

                    string newPath = Path.Combine(Path.GetDirectoryName(targetFile), newName);

                    const int maxRetries = 10;
                    for (int i = 0; i < maxRetries; i++)
                    {
                        try
                        {
                            if (!File.Exists(targetFile)) return null;
                            if (File.Exists(newPath)) { try { File.Delete(newPath); } catch { } }

                            File.Move(targetFile, newPath);
                            LogFileRename(targetFile, newPath);
                            return newPath;
                        }
                        catch (System.IO.FileNotFoundException) { return null; }
                        catch (UnauthorizedAccessException) { return null; }
                        catch (IOException) when (i < maxRetries - 1) { Thread.Sleep(500); }
                        catch (Exception) { return null; }
                    }
                    return null;
                }
            }
            return null;
        }

        #endregion

        #region Public Methods & Status Control

        public void UpdateStatusOnRun(bool isRunning)
        {
            this.isRunning = isRunning;
            SetControlEnabled(!isRunning);

            if (isRunning)
            {
                RefreshBaselineCache();
                InitializeBaselineWatcher();
                InitializeTargetWatchers();
                Task.Run(() => CompareAndRenameFiles());
            }
            else
            {
                baselineWatcher?.Dispose();
                baselineWatcher = null;
                StopTargetWatchers();
            }

            string status = isRunning ? "Running" : "Stopped";
            Color statusColor = isRunning ? Color.Green : Color.Red;
            StatusUpdated?.Invoke($"Status: {status}", statusColor);
        }

        public void InitializePanel(bool isRunning) { UpdateStatusOnRun(isRunning); }
        public void LoadDataFromSettings()
        {
            cb_BaseDatePath.Items.Clear();
            cb_BaseDatePath.Items.AddRange(settingsManager.GetFoldersFromSection("[BaseFolder]").ToArray());
            lb_TargetComparePath.Items.Clear();
            // [수정] 섹션명 분리
            foreach (var path in settingsManager.GetFoldersFromSection("[OntoTargetComparePath]")) lb_TargetComparePath.Items.Add(path);
        }

        public void RefreshUI() { LoadDataFromSettings(); }

        public void SetControlEnabled(bool isEnabled)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => SetControlEnabled(isEnabled))); return; }
            btn_BaseClear.Enabled = isEnabled; btn_SelectFolder.Enabled = isEnabled; btn_Remove.Enabled = isEnabled;
            cb_BaseDatePath.Enabled = isEnabled; lb_TargetComparePath.Enabled = isEnabled;
        }

        public void UpdateStatus(string status) { }

        public void CompareAndRenameFiles()
        {
            try
            {
                RefreshBaselineCache();
                if (_baselineCache.Count == 0) return;

                foreach (string targetFolder in lb_TargetComparePath.Items.Cast<string>())
                {
                    if (!Directory.Exists(targetFolder)) continue;

                    var targetFiles = Directory.GetFiles(targetFolder, "*_#*_*.*")
                                               .Where(f => Regex.IsMatch(Path.GetFileName(f), @"_#\d+_"));

                    foreach (var targetFile in targetFiles)
                    {
                        try { ProcessTargetFile(targetFile, _baselineCache); }
                        catch (Exception innerEx) { logManager.LogError($"[ucOntoOverrideNamesPanel] 오류: {innerEx.Message}"); }
                    }
                }
            }
            catch (Exception ex) { logManager.LogError($"[ucOntoOverrideNamesPanel] CompareAndRenameFiles() 중 오류: {ex.Message}"); }
        }

        public void StartProcessing()
        {
            while (true)
            {
                if (IsRunning()) { CompareAndRenameFiles(); System.Threading.Thread.Sleep(1000); }
            }
        }

        private bool IsRunning() { return isRunning; }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { baselineWatcher?.Dispose(); folderWatcher?.Dispose(); StopTargetWatchers(); }
            base.Dispose(disposing);
        }

        public string EnsureOverrideAndReturnPath(string originalPath, int timeoutMs = 180_000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string baseFolder = settingsManager.GetBaseFolder();
            if (string.IsNullOrEmpty(baseFolder)) return originalPath;

            string baselineFolder = Path.Combine(baseFolder, "Baseline");
            string fileName = Path.GetFileName(originalPath);
            var timeMatch = Regex.Match(fileName, @"\d{8}_\d{6}");
            string searchFilter = timeMatch.Success ? $"*{timeMatch.Value}*.info" : "*.info";

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (!Directory.Exists(baselineFolder)) { System.Threading.Thread.Sleep(500); continue; }

                var infos = Directory.GetFiles(baselineFolder, searchFilter);
                if (infos.Length > 0)
                {
                    var baselineData = ExtractBaselineData(infos);
                    string renamed = TryRenameTargetFile(originalPath, baselineData);
                    if (!string.IsNullOrEmpty(renamed)) return renamed;
                }
                System.Threading.Thread.Sleep(500);
            }
            return originalPath;
        }

        private string TryRenameTargetFile(string srcPath, IDictionary<string, (string TimeInfo, string Prefix, string CInfo)> baselineData)
        {
            try
            {
                string newName = ProcessTargetFile(srcPath, baselineData);
                if (!string.IsNullOrEmpty(newName)) return newName;
            }
            catch (Exception ex) { logManager.LogError($"[Override] Rename 실패: {ex.Message}"); }
            return null;
        }

        #endregion
    }
}
