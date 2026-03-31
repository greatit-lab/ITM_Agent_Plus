// ITM_Agent_Plus/Services/SettingsManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ITM_Agent.Services
{
    /// <summary>
    /// Settings.ini 파일을 관리하며, 특정 섹션([Eqpid], [BaseFolder], [TargetFolders], [ExcludeFolders], [Regex]) 값들을
    /// 읽고/쓰고/수정하는 기능을 제공하는 클래스입니다.
    /// 공통 기능과 Maker 전용(Onto 등) 섹션을 분리하여 관리합니다.
    /// </summary>
    public class SettingsManager
    {
        private readonly string settingsFilePath;
        private readonly object fileLock = new object();
        private readonly LogManager logManager;
        public event Action RegexSettingsUpdated;

        private bool isDebugMode; // DebugMode 상태 저장
        private bool isPerformanceLogging;    // 성능 로깅

        public SettingsManager(string settingsFilePath)
        {
            this.settingsFilePath = settingsFilePath;

            // 🌟 로그 매니저 주입 — 기본 실행 경로 Logs 폴더 사용
            logManager = new LogManager(AppDomain.CurrentDomain.BaseDirectory);
            logManager.LogEvent("[SettingsManager] Instantiated");

            EnsureSettingsFileExists();
        }

        // AutoRunOnStart 속성 (Agent 섹션 - 공통)
        public bool AutoRunOnStart
        {
            get => GetValueFromSection("Agent", "AutoRunOnStart") == "1";
            set => SetValueToSection("Agent", "AutoRunOnStart", value ? "1" : "0");
        }

        // DebugMode 속성 (공통)
        public bool IsDebugMode
        {
            get => isDebugMode;
            set
            {
                isDebugMode = value;
            }
        }

        // 성능 로깅 속성 (Option 섹션 - 공통)
        public bool IsPerformanceLogging
        {
            get => GetValueFromSection("Option", "EnablePerfoLog") == "1";
            set
            {
                isPerformanceLogging = value;
                SetValueToSection("Option", "EnablePerfoLog", value ? "1" : "0");
            }
        }

        // 정보 자동 삭제 속성 (Option 섹션 - 공통)
        public bool IsInfoDeletionEnabled
        {
            get => GetValueFromSection("Option", "EnableInfoAutoDel") == "1";
            set => SetValueToSection("Option", "EnableInfoAutoDel", value ? "1" : "0");
        }

        // 정보 보존 기간 속성 (Option 섹션 - 공통)
        public int InfoRetentionDays
        {
            get
            {
                var raw = GetValueFromSection("Option", "InfoRetentionDays");
                return int.TryParse(raw, out var d) ? d : 1;
            }
            set => SetValueToSection("Option", "InfoRetentionDays", value.ToString());
        }

        // ─────────────────────────────────────────────────────────────
        // [Onto 전용 속성] Lamp Life Collector
        // INI 섹션명: [OntoLampLife] 로 명시적 분리
        // ─────────────────────────────────────────────────────────────
        public bool IsOntoLampLifeCollectorEnabled
        {
            get => GetValueFromSection("OntoLampLife", "Enabled") == "1";
            set => SetValueToSection("OntoLampLife", "Enabled", value ? "1" : "0");
        }

        public int OntoLampLifeCollectorInterval
        {
            get
            {
                var raw = GetValueFromSection("OntoLampLife", "IntervalMinutes");
                if (int.TryParse(raw, out int interval) && interval > 0)
                {
                    return interval;
                }
                return 60; // 기본값 60분
            }
            set => SetValueToSection("OntoLampLife", "IntervalMinutes", value.ToString());
        }

        private void EnsureSettingsFileExists()
        {
            if (!File.Exists(settingsFilePath))
            {
                using (File.Create(settingsFilePath)) { }
            }
        }

        public string GetEqpid()
        {
            if (!File.Exists(settingsFilePath)) return null;

            var lines = File.ReadAllLines(settingsFilePath);
            bool eqpidSectionFound = false;
            foreach (string line in lines)
            {
                if (line.Trim() == "[Eqpid]")
                {
                    eqpidSectionFound = true;
                    continue;
                }
                if (eqpidSectionFound && line.StartsWith("Eqpid = "))
                {
                    return line.Substring("Eqpid =".Length).Trim();
                }
            }
            return null;
        }

        private void WriteToFileSafely(string[] lines)
        {
            try
            {
                lock (fileLock)
                {
                    File.WriteAllLines(settingsFilePath, lines);
                    logManager.LogEvent($"[SettingsManager] Wrote {lines.Length} lines -> {settingsFilePath}");
                }
            }
            catch (Exception ex)
            {
                logManager.LogError($"[SettingsManager] WRITE failed: {ex.Message}");
                throw;
            }
        }

        public void SetEqpid(string eqpid)
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();
            int eqpidIndex = lines.FindIndex(l => l.Trim() == "[Eqpid]");

            if (eqpidIndex == -1)
            {
                lines.Add("[Eqpid]");
                lines.Add("Eqpid = " + eqpid);
            }
            else
            {
                lines[eqpidIndex + 1] = "Eqpid = " + eqpid;
            }

            WriteToFileSafely(lines.ToArray());
        }

        // 공통 기능인 Categorize 설정들이 존재하는지 확인
        public bool IsReadyToRun()
        {
            return HasValuesInSection("[BaseFolder]") &&
                   HasValuesInSection("[TargetFolders]") &&
                   HasValuesInSection("[Regex]");
        }

        private bool HasValuesInSection(string section)
        {
            if (!File.Exists(settingsFilePath)) return false;

            var lines = File.ReadAllLines(settingsFilePath).ToList();
            int sectionIndex = lines.FindIndex(line => line.Trim() == section);
            if (sectionIndex == -1) return false;

            int endIndex = lines.FindIndex(sectionIndex + 1, line => line.StartsWith("[") || string.IsNullOrWhiteSpace(line));
            if (endIndex == -1) endIndex = lines.Count;

            return lines.Skip(sectionIndex + 1).Take(endIndex - sectionIndex - 1)
                        .Any(line => !string.IsNullOrWhiteSpace(line));
        }

        public List<string> GetFoldersFromSection(string section)
        {
            var folders = new List<string>();
            if (!File.Exists(settingsFilePath))
                return folders;

            var lines = File.ReadAllLines(settingsFilePath);
            bool inSection = false;
            foreach (var line in lines)
            {
                if (line.Trim() == section)
                {
                    inSection = true;
                    continue;
                }
                if (inSection)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        break;
                    folders.Add(line.Trim());
                }
            }
            return folders;
        }

        public Dictionary<string, string> GetRegexList()
        {
            var regexList = new Dictionary<string, string>();
            if (!File.Exists(settingsFilePath)) return regexList;

            var lines = File.ReadAllLines(settingsFilePath);
            bool inRegexSection = false;

            foreach (var line in lines)
            {
                if (line.Trim() == "[Regex]")
                {
                    inRegexSection = true;
                    continue;
                }

                if (inRegexSection)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        break;

                    var parts = line.Split(new[] { "->" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        regexList[parts[0].Trim()] = parts[1].Trim();
                    }
                }
            }
            return regexList;
        }

        public void SetFoldersToSection(string section, List<string> folders)
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();

            int sectionIndex = lines.FindIndex(l => l.Trim() == section);
            if (sectionIndex == -1)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.Add("");
                }
                lines.Add(section);
                foreach (var folder in folders)
                {
                    lines.Add(folder);
                }
                lines.Add("");
            }
            else
            {
                int endIndex = lines.FindIndex(sectionIndex + 1, line => line.StartsWith("[") || string.IsNullOrWhiteSpace(line));
                if (endIndex == -1) endIndex = lines.Count;

                lines.RemoveRange(sectionIndex + 1, endIndex - sectionIndex - 1);

                foreach (var folder in folders)
                {
                    lines.Insert(sectionIndex + 1, folder);
                    sectionIndex++;
                }

                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.Add("");
                }
            }
            File.WriteAllLines(settingsFilePath, lines);
        }

        public void SetBaseFolder(string folderPath)
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();

            int sectionIndex = lines.FindIndex(l => l.Trim() == "[BaseFolder]");
            if (sectionIndex == -1)
            {
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                {
                    lines.Add("");
                }
                lines.Add("[BaseFolder]");
                lines.Add(folderPath);
                lines.Add("");
            }
            else
            {
                int endIndex = lines.FindIndex(sectionIndex + 1, line => line.StartsWith("[") || string.IsNullOrWhiteSpace(line));
                if (endIndex == -1) endIndex = lines.Count;

                var updatedSection = new List<string> { "[BaseFolder]", folderPath, "" };
                lines = lines.Take(sectionIndex)
                             .Concat(updatedSection)
                             .Concat(lines.Skip(endIndex))
                             .ToList();
            }

            File.WriteAllLines(settingsFilePath, lines);
        }

        public void SetRegexList(Dictionary<string, string> regexDict)
        {
            var lines = File.Exists(settingsFilePath)
                ? File.ReadAllLines(settingsFilePath).ToList()
                : new List<string>();

            int sectionIndex = lines.FindIndex(l => l.Trim() == "[Regex]");
            if (sectionIndex != -1)
            {
                int endIndex = lines.FindIndex(sectionIndex + 1,
                    line => line.StartsWith("[") || string.IsNullOrWhiteSpace(line));
                if (endIndex == -1) endIndex = lines.Count;
                lines.RemoveRange(sectionIndex, endIndex - sectionIndex);
            }

            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                lines.Add("");

            lines.Add("[Regex]");
            foreach (var kvp in regexDict)
                lines.Add($"{kvp.Key} -> {kvp.Value}");
            lines.Add("");

            File.WriteAllLines(settingsFilePath, lines);

            NotifyRegexSettingsUpdated();
        }

        public void ResetExceptEqpid()
        {
            var lines = File.ReadAllLines(settingsFilePath).ToList();

            int eqpidStartIndex = lines.FindIndex(line => line.Trim().Equals("[Eqpid]", StringComparison.OrdinalIgnoreCase));
            int eqpidEndIndex = lines.FindIndex(eqpidStartIndex + 1, line => line.StartsWith("[") || string.IsNullOrWhiteSpace(line));

            if (eqpidStartIndex == -1)
            {
                throw new InvalidOperationException("[Eqpid] 섹션이 설정 파일에 존재하지 않습니다.");
            }

            eqpidEndIndex = (eqpidEndIndex == -1) ? lines.Count : eqpidEndIndex;
            var eqpidSectionLines = lines.Skip(eqpidStartIndex).Take(eqpidEndIndex - eqpidStartIndex).ToList();

            File.WriteAllText(settingsFilePath, string.Empty);
            File.AppendAllLines(settingsFilePath, eqpidSectionLines);
            File.AppendAllText(settingsFilePath, Environment.NewLine);
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found.", filePath);

                File.Copy(filePath, settingsFilePath, overwrite: true);
                logManager.LogEvent($"[SettingsManager] Loaded settings from {filePath}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[SettingsManager] LOAD failed: {ex.Message}");
                throw;
            }
        }

        public void SaveToFile(string filePath)
        {
            try
            {
                File.Copy(settingsFilePath, filePath, overwrite: true);
                logManager.LogEvent($"[SettingsManager] Saved settings to {filePath}");
            }
            catch (Exception ex)
            {
                logManager.LogError($"[SettingsManager] SAVE failed: {ex.Message}");
                throw;
            }
        }

        public void SetType(string type)
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();
            int sectionIndex = lines.FindIndex(l => l.Trim() == "[Eqpid]");
            if (sectionIndex == -1)
            {
                lines.Add("[Eqpid]");
                lines.Add($"Type = {type}");
            }
            else
            {
                int typeIndex = lines.FindIndex(sectionIndex + 1, l => l.StartsWith("Type ="));
                if (typeIndex != -1)
                    lines[typeIndex] = $"Type = {type}";
                else
                    lines.Insert(sectionIndex + 1, $"Type = {type}");
            }
            WriteToFileSafely(lines.ToArray());
        }

        public string GetEqpType()
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();
            int sectionIndex = lines.FindIndex(l => l.Trim() == "[Eqpid]");
            if (sectionIndex != -1)
            {
                var typeLine = lines.Skip(sectionIndex + 1).FirstOrDefault(l => l.StartsWith("Type ="));
                if (!string.IsNullOrEmpty(typeLine))
                    return typeLine.Split('=')[1].Trim();
            }
            return null;
        }

        public string GetValueFromSection(string section, string key)
        {
            if (!File.Exists(settingsFilePath)) return null;

            var lines = File.ReadAllLines(settingsFilePath);
            bool inSection = false;

            foreach (string line in lines)
            {
                if (line.Trim() == $"[{section}]")
                {
                    inSection = true;
                    continue;
                }

                if (inSection)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        break;

                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2 && keyValue[0].Trim() == key)
                    {
                        return keyValue[1].Trim();
                    }
                }
            }

            return null;
        }

        public void SetValueToSection(string section, string key, string value)
        {
            lock (fileLock)
            {
                var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();
                int sectionIndex = lines.FindIndex(l => l.Trim() == $"[{section}]");

                if (sectionIndex == -1)
                {
                    if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.Last()))
                    {
                        lines.Add("");
                    }
                    lines.Add($"[{section}]");
                    lines.Add($"{key} = {value}");
                }
                else
                {
                    int endIndex = lines.FindIndex(sectionIndex + 1, l => l.StartsWith("[") || string.IsNullOrWhiteSpace(l));
                    if (endIndex == -1) endIndex = lines.Count;

                    bool keyFound = false;
                    for (int i = sectionIndex + 1; i < endIndex; i++)
                    {
                        if (lines[i].StartsWith($"{key} ="))
                        {
                            lines[i] = $"{key} = {value}";
                            keyFound = true;
                            break;
                        }
                    }

                    if (!keyFound)
                    {
                        lines.Insert(endIndex, $"{key} = {value}");
                    }
                }

                File.WriteAllLines(settingsFilePath, lines);
            }
        }

        public void RemoveSection(string section)
        {
            var lines = File.Exists(settingsFilePath) ? File.ReadAllLines(settingsFilePath).ToList() : new List<string>();
            int sectionIndex = lines.FindIndex(l => l.Trim() == $"[{section}]");

            if (sectionIndex != -1)
            {
                int endIndex = lines.FindIndex(sectionIndex + 1, l => l.StartsWith("[") || string.IsNullOrWhiteSpace(l));
                if (endIndex == -1) endIndex = lines.Count;

                lines.RemoveRange(sectionIndex, endIndex - sectionIndex);
                File.WriteAllLines(settingsFilePath, lines);
            }
        }

        public List<string> GetRegexFolders()
        {
            var folders = new List<string>();
            if (!File.Exists(settingsFilePath))
                return folders;

            var lines = File.ReadAllLines(settingsFilePath);
            bool inRegexSection = false;

            foreach (var line in lines)
            {
                if (line.Trim() == "[Regex]")
                {
                    inRegexSection = true;
                    continue;
                }

                if (inRegexSection)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                        break;

                    var parts = line.Split(new[] { "->" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                        folders.Add(parts[1].Trim());
                }
            }
            return folders;
        }

        public void NotifyRegexSettingsUpdated()
        {
            RegexSettingsUpdated?.Invoke();
            ReloadSettings();
        }

        private void ReloadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                var lines = File.ReadAllLines(settingsFilePath);
            }
        }

        public string GetBaseFolder()
        {
            var baseFolders = GetFoldersFromSection("[BaseFolder]");
            if (baseFolders.Count > 0)
            {
                return baseFolders[0];
            }

            return null;
        }

        public void RemoveKeyFromSection(string section, string key)
        {
            if (!File.Exists(settingsFilePath))
                return;

            var lines = File.ReadAllLines(settingsFilePath).ToList();
            bool inSection = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (trimmed.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase))
                {
                    inSection = true;
                    continue;
                }

                if (inSection)
                {
                    if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                        break;

                    int equalIndex = line.IndexOf('=');
                    if (equalIndex >= 0)
                    {
                        string currentKey = line.Substring(0, equalIndex).Trim();
                        if (currentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        {
                            lines.RemoveAt(i);
                            i--;
                        }
                    }
                }
            }

            File.WriteAllLines(settingsFilePath, lines);
        }
    }
}
