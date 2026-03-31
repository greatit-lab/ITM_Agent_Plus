// ITM_Agnet_Plus/Program.cs
using ITM_Agent.Services;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Security.Principal;

namespace ITM_Agent
{
    internal static class Program
    {
        private static Mutex mutex = null;
        private const string appGuid = "c0a76b5a-12ab-45c5-b9d9-d693faa6e7b9";

        [STAThread]
        static void Main()
        {
            // (OS 언어 설정)
            var ui = CultureInfo.CurrentUICulture;
            if (!ui.Name.StartsWith("ko", StringComparison.OrdinalIgnoreCase))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            }

            // (관리자 권한 확인)
            if (!IsRunningAsAdmin())
            {
                string title;
                string message;

                if (CultureInfo.CurrentUICulture.Name.StartsWith("ko"))
                {
                    title = "권한 필요";
                    message = "ITM Agent+는 하드웨어 센서 정보 수집을 위해 관리자 권한이 필요합니다.\n\n" +
                              "프로그램을 종료한 후, 실행 파일을 마우스 오른쪽 버튼으로 클릭하여 '관리자 권한으로 실행' 해주세요.";
                }
                else // 기본값은 영문
                {
                    title = "Administrator Rights Required";
                    message = "ITM Agent requires administrator rights to collect hardware sensor data.\n\n" +
                              "Please close the program, then right-click the executable and select 'Run as administrator'.";
                }
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // 권한이 없으면 프로그램 즉시 종료
            }

            // (중복 실행 Mutex 체크)
            mutex = new Mutex(true, appGuid, out bool createdNew);
            if (!createdNew)
            {
                string title = CultureInfo.CurrentUICulture.Name.StartsWith("ko") ? "실행 확인" : "Already Running";
                string message = CultureInfo.CurrentUICulture.Name.StartsWith("ko") ? "ITM Agent가 이미 실행 중입니다." : "ITM Agent is already running.";
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Connection.ini 존재 여부만 확인
            string connectionIniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Connection.ini");
            if (!File.Exists(connectionIniPath))
            {
                MessageBox.Show("핵심 설정 파일(Connection.ini)이 없습니다. 설치가 잘못되었거나 파일이 누락되었습니다.", "설정 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // (AssemblyResolve)
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string asmFile = new AssemblyName(args.Name).Name + ".dll";
                string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library", asmFile);
                return File.Exists(libPath) ? Assembly.LoadFrom(libPath) : null;
            };

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var settingsManager = new SettingsManager(Path.Combine(baseDir, "Settings.ini"));

            Application.Run(new MainForm(settingsManager));

            mutex.ReleaseMutex();
        }

        // (IsRunningAsAdmin 메서드)
        private static bool IsRunningAsAdmin()
        {
            try
            {
                using (var identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
