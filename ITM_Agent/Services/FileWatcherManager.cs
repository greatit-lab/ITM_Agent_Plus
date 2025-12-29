// ITM_Agent/Services/FileWatcherManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ITM_Agent.Services
{
    public class FileWatcherManager
    {
        private SettingsManager settingsManager;
        private LogManager logManager;
        private readonly List<FileSystemWatcher> watchers = new List<FileSystemWatcher>();
        private readonly Dictionary<string, DateTime> fileProcessTracker = new Dictionary<string, DateTime>();
        private readonly TimeSpan duplicateEventThreshold = TimeSpan.FromSeconds(5);

        private bool isRunning = false;
        private bool isPaused = false; // [신규] 서버 끊김으로 인한 일시정지 상태

        // 안정화 감지용
        private readonly Dictionary<string, FileTrackingInfo> trackedFiles = new Dictionary<string, FileTrackingInfo>(StringComparer.OrdinalIgnoreCase);
        private System.Threading.Timer stabilityCheckTimer;
        private readonly object trackingLock = new object();
        private const int StabilityCheckIntervalMs = 1000;
        private const double FileStableThresholdSeconds = 5.0;

        private class FileTrackingInfo
        {
            public DateTime LastEventTime { get; set; }
            public long LastSize { get; set; }
            public DateTime LastWriteTime { get; set; }
            public WatcherChangeTypes LastChangeType { get; set; }
        }

        // 복구 작업 락
        private readonly object recoveryLock = new object();

        public FileWatcherManager(SettingsManager settingsManager, LogManager logManager, bool isDebugMode)
        {
            this.settingsManager = settingsManager;
            this.logManager = logManager;
            LogManager.GlobalDebugEnabled = isDebugMode;
        }

        public void UpdateDebugMode(bool isDebug)
        {
            LogManager.GlobalDebugEnabled = isDebug;
            logManager.LogEvent($"[FileWatcherManager] Debug mode updated to: {isDebug}");
        }

        // [신규] 서버 끊김 시 호출: 이벤트 발생을 원천 차단
        public void PauseWatching()
        {
            if (!isRunning) return;
            isPaused = true;
            foreach (var w in watchers)
            {
                try { w.EnableRaisingEvents = false; } catch { }
            }
            logManager.LogEvent("[FileWatcherManager] Paused watching (Server Holding).");
        }

        // [신규] 서버 복구 시 호출: 이벤트 발생 재개
        public void ResumeWatching()
        {
            if (!isRunning) return;
            isPaused = false;
            foreach (var w in watchers)
            {
                try { w.EnableRaisingEvents = true; } catch { }
            }
            logManager.LogEvent("[FileWatcherManager] Resumed watching.");
        }

        public void InitializeWatchers()
        {
            StopWatchers();
            var targetFolders = settingsManager.GetFoldersFromSection("[TargetFolders]");
            if (targetFolders.Count == 0)
            {
                logManager.LogEvent("[FileWatcherManager] No target folders configured for monitoring.");
                return;
            }

            foreach (var folder in targetFolders)
            {
                if (!Directory.Exists(folder))
                {
                    logManager.LogEvent($"[FileWatcherManager] Folder does not exist: {folder}", LogManager.GlobalDebugEnabled);
                    continue;
                }

                try
                {
                    var watcher = new FileSystemWatcher(folder)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                        InternalBufferSize = 131072 // 128KB
                    };

                    watcher.Created += OnFileChanged;
                    watcher.Changed += OnFileChanged;
                    watcher.Error += OnWatcherError;

                    watchers.Add(watcher);

                    if (LogManager.GlobalDebugEnabled)
                        logManager.LogDebug($"[FileWatcherManager] Initialized watcher for folder: {folder}");
                }
                catch (Exception ex)
                {
                    logManager.LogError($"[FileWatcherManager] Failed to create watcher for {folder}. Error: {ex.Message}");
                }
            }
            logManager.LogEvent($"[FileWatcherManager] {watchers.Count} watcher(s) initialized.");
        }

        public void StartWatching()
        {
            if (isRunning) return;

            InitializeWatchers();
            if (watchers.Count == 0) return;

            foreach (var watcher in watchers)
            {
                try { watcher.EnableRaisingEvents = true; } catch { }
            }

            isRunning = true;
            isPaused = false;
            logManager.LogEvent("[FileWatcherManager] File monitoring started.");
        }

        public void StopWatchers()
        {
            foreach (var w in watchers)
            {
                try
                {
                    w.EnableRaisingEvents = false;
                    w.Created -= OnFileChanged;
                    w.Changed -= OnFileChanged;
                    w.Error -= OnWatcherError;
                    w.Dispose();
                }
                catch { }
            }
            watchers.Clear();

            lock (trackingLock)
            {
                stabilityCheckTimer?.Dispose();
                stabilityCheckTimer = null;
                trackedFiles.Clear();
            }

            isRunning = false;
            isPaused = false;
            logManager.LogEvent("[FileWatcherManager] File monitoring stopped.");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!isRunning || isPaused) return; // Paused 상태면 무시

            var excludeFolders = settingsManager.GetFoldersFromSection("[ExcludeFolders]");
            string changedFolderPath = null;
            try
            {
                changedFolderPath = Path.GetDirectoryName(e.FullPath);
                if (string.IsNullOrEmpty(changedFolderPath)) return;
            }
            catch { return; }

            foreach (var excludeFolder in excludeFolders)
            {
                try
                {
                    string normalizedExclude = Path.GetFullPath(excludeFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string normalizedChanged = Path.GetFullPath(changedFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (normalizedChanged.StartsWith(normalizedExclude, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
                catch { return; }
            }

            if (IsDuplicateEvent(e.FullPath)) return;

            try
            {
                lock (trackingLock)
                {
                    DateTime now = DateTime.UtcNow;
                    long currentSize = GetFileSizeSafe(e.FullPath);
                    DateTime currentWriteTime = GetLastWriteTimeSafe(e.FullPath);

                    if (currentSize == 0 && e.ChangeType == WatcherChangeTypes.Changed) return;

                    if (!trackedFiles.TryGetValue(e.FullPath, out FileTrackingInfo info))
                    {
                        info = new FileTrackingInfo();
                        trackedFiles[e.FullPath] = info;
                    }

                    info.LastEventTime = now;
                    info.LastSize = currentSize;
                    info.LastWriteTime = currentWriteTime;
                    info.LastChangeType = e.ChangeType;

                    if (stabilityCheckTimer == null)
                    {
                        stabilityCheckTimer = new Timer(CheckFileStability, null, StabilityCheckIntervalMs, StabilityCheckIntervalMs);
                    }
                    else
                    {
                        stabilityCheckTimer.Change(StabilityCheckIntervalMs, StabilityCheckIntervalMs);
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"[FileWatcherManager] OnFileChanged Error: {ex.Message}");
            }
        }

        private void CheckFileStability(object state)
        {
            if (isPaused) return;

            try
            {
                DateTime now = DateTime.UtcNow;
                var stableFilesToProcess = new List<string>();

                lock (trackingLock)
                {
                    if (!isRunning || trackedFiles.Count == 0)
                    {
                        stabilityCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                        return;
                    }

                    var currentTrackedFiles = trackedFiles.ToList();

                    foreach (var kvp in currentTrackedFiles)
                    {
                        string filePath = kvp.Key;
                        FileTrackingInfo info = kvp.Value;

                        long currentSize = GetFileSizeSafe(filePath);
                        DateTime currentWriteTime = GetLastWriteTimeSafe(filePath);

                        if (currentSize == -1 || currentWriteTime == DateTime.MinValue)
                        {
                            trackedFiles.Remove(filePath);
                            continue;
                        }

                        if (currentSize != info.LastSize || currentWriteTime != info.LastWriteTime)
                        {
                            info.LastEventTime = now;
                            info.LastSize = currentSize;
                            info.LastWriteTime = currentWriteTime;
                            continue;
                        }

                        double elapsedSeconds = (now - info.LastEventTime).TotalSeconds;

                        if (elapsedSeconds >= FileStableThresholdSeconds)
                        {
                            if (IsFileReady(filePath))
                            {
                                stableFilesToProcess.Add(filePath);
                                trackedFiles.Remove(filePath);
                            }
                        }
                    }

                    if (trackedFiles.Count == 0)
                    {
                        stabilityCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else
                    {
                        stabilityCheckTimer?.Change(StabilityCheckIntervalMs, StabilityCheckIntervalMs);
                    }
                }

                foreach (string stableFilePath in stableFilesToProcess)
                {
                    try { ProcessFile(stableFilePath); }
                    catch (Exception ex)
                    {
                        logManager.LogError($"[FileWatcherManager] Error processing stable file {stableFilePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"[FileWatcherManager] CheckFileStability Error: {ex.Message}");
                try { stabilityCheckTimer?.Change(Timeout.Infinite, Timeout.Infinite); } catch { }
            }
        }

        // [신규/개선] 복구 스캔 (Slow Recovery Mode)
        public void StartRecoveryScan()
        {
            if (!Monitor.TryEnter(recoveryLock)) return; // 이미 실행 중이면 스킵

            Task.Run(() =>
            {
                try
                {
                    // ★ 스레드 우선순위 낮춤 (장비 부하 최소화)
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                    logManager.LogEvent("[Recovery] Starting Slow Recovery Scan...");

                    var targetFolders = settingsManager.GetFoldersFromSection("[TargetFolders]");
                    int totalProcessed = 0;

                    // 제외 폴더 목록 준비
                    var excludeFolders = settingsManager.GetFoldersFromSection("[ExcludeFolders]")
                        .Select(p =>
                        {
                            try { return Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
                            catch { return null; }
                        })
                        .Where(p => p != null)
                        .ToList();

                    foreach (var folder in targetFolders)
                    {
                        if (!Directory.Exists(folder) || isPaused) continue;

                        // 하위 폴더까지 검색
                        foreach (string filePath in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                        {
                            if (isPaused) break; // 실행 중 서버 끊기면 즉시 중단

                            // 제외 폴더 체크
                            try
                            {
                                string currentFileDir = Path.GetDirectoryName(filePath);
                                if (string.IsNullOrEmpty(currentFileDir)) continue;
                                string normalizedCurrentDir = Path.GetFullPath(currentFileDir)
                                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                                if (excludeFolders.Any(ex => normalizedCurrentDir.StartsWith(ex, StringComparison.OrdinalIgnoreCase)))
                                    continue;
                            }
                            catch { continue; }

                            // 중복 체크
                            if (IsDuplicateEvent(filePath)) continue;

                            // 파일 처리 시도 (IsFileReady 체크 포함)
                            if (IsFileReady(filePath))
                            {
                                try
                                {
                                    string result = ProcessFile(filePath);
                                    if (result != null)
                                    {
                                        totalProcessed++;
                                        // ★ Throttling: 파일 하나당 200ms 휴식
                                        Thread.Sleep(200);

                                        // 처리된 파일 기록
                                        lock (fileProcessTracker)
                                        {
                                            fileProcessTracker[filePath] = DateTime.UtcNow;
                                        }
                                    }
                                    else
                                    {
                                        // 실패했거나 대상 아님 -> 약간의 텀 (CPU 양보)
                                        Thread.Sleep(10);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logManager.LogError($"[Recovery] Error processing file '{filePath}': {ex.Message}");
                                }
                            }
                        }
                    }
                    logManager.LogEvent($"[Recovery] Scan completed. Processed: {totalProcessed} files.");
                }
                catch (Exception ex)
                {
                    logManager.LogError($"[Recovery] Scan failed: {ex.Message}");
                }
                finally
                {
                    Monitor.Exit(recoveryLock);
                }
            });
        }

        private string ProcessFile(string filePath)
        {
            string fileName;
            try { fileName = Path.GetFileName(filePath); if (string.IsNullOrEmpty(fileName)) return null; } catch { return null; }

            var regexList = settingsManager.GetRegexList();

            foreach (var kvp in regexList)
            {
                try
                {
                    if (Regex.IsMatch(fileName, kvp.Key))
                    {
                        string destinationFolder = kvp.Value;
                        string destinationFile = Path.Combine(destinationFolder, fileName);
                        try
                        {
                            Directory.CreateDirectory(destinationFolder);

                            if (!CopyFileWithSharedRead(filePath, destinationFile, true))
                            {
                                return null;
                            }

                            logManager.LogEvent($"[FileWatcherManager] File Copied: {fileName} -> {destinationFolder}");
                            return destinationFolder;
                        }
                        catch (Exception ex) { logManager.LogError($"[FileWatcherManager] Error copying file {fileName}: {ex.Message}"); }
                        return null;
                    }
                }
                catch { }
            }
            return null;
        }

        private bool CopyFileWithSharedRead(string sourcePath, string destPath, bool overwrite)
        {
            int maxRetries = 5;
            int delayMs = 300;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var destStream = new FileStream(destPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            sourceStream.CopyTo(destStream);
                        }
                    }
                    return true;
                }
                catch (FileNotFoundException)
                {
                    return false;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    Thread.Sleep(delayMs);
                }
                catch
                {
                    return false;
                }
            }
            logManager.LogError($"[FileWatcherManager] Copy failed after retries: {sourcePath}");
            return false;
        }

        private bool IsDuplicateEvent(string filePath)
        {
            DateTime now = DateTime.UtcNow;
            lock (fileProcessTracker)
            {
                if (fileProcessTracker.Count > 1000)
                {
                    var keysToRemove = fileProcessTracker
                        .Where(kvp => (now - kvp.Value).TotalMinutes > 5)
                        .Select(kvp => kvp.Key)
                        .ToList();
                    foreach (var key in keysToRemove) fileProcessTracker.Remove(key);
                }
                if (fileProcessTracker.TryGetValue(filePath, out var lastProcessed))
                {
                    if ((now - lastProcessed) < duplicateEventThreshold) return true;
                }
                fileProcessTracker[filePath] = now;
                return false;
            }
        }

        private bool IsFileReady(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return true;
                }
            }
            catch { return false; }
        }

        private long GetFileSizeSafe(string filePath)
        {
            try { return File.Exists(filePath) ? new FileInfo(filePath).Length : -1; }
            catch { return -1; }
        }

        private DateTime GetLastWriteTimeSafe(string filePath)
        {
            try { return File.Exists(filePath) ? File.GetLastWriteTimeUtc(filePath) : DateTime.MinValue; }
            catch { return DateTime.MinValue; }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            Exception ex = e.GetException();
            string errorMessage = ex?.Message ?? "Unknown watcher error";
            logManager.LogError($"[FileWatcherManager] Watcher error: {errorMessage}");

            if (ex is InternalBufferOverflowException)
            {
                FileSystemWatcher watcher = sender as FileSystemWatcher;
                if (watcher != null)
                {
                    logManager.LogEvent($"[FileWatcherManager] Buffer overflow on '{watcher.Path}'. Scheduling recovery scan.");
                    StartRecoveryScan();
                }
            }
        }

        private void ManuallyScanAndProcessFolder(string folderPath)
        {
            // 이 메서드는 이제 StartRecoveryScan으로 대체됩니다.
        }
    }
}
