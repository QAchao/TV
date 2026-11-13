using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

internal static class Launcher
{
    private const string Address = "http://127.0.0.1:8080/";

    [STAThread]
    private static void Main()
    {
        try
        {
            using (var mutex = new Mutex(false, @"Local\LibreTV.QAchao.Launcher"))
            {
                try { mutex.WaitOne(); }
                catch (AbandonedMutexException) { }
                try { Open(); }
                finally { mutex.ReleaseMutex(); }
            }
        }
        catch (Exception error)
        {
            MessageBox.Show(error.Message, "LibreTV", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Open()
    {
        if (IsReady())
        {
            Process.Start(Address);
            return;
        }

        string home = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LibreTV");
        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        string runtime = Path.Combine(home, "runtime-" + version);
        string server = Path.Combine(runtime, "server.js");
        if (!File.Exists(Path.Combine(runtime, ".installed")))
        {
            Directory.CreateDirectory(home);
            string staging = Path.Combine(home, "extract-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LibreTV.Runtime.zip"))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    archive.ExtractToDirectory(staging);
                File.WriteAllText(Path.Combine(staging, ".installed"), version);
                Directory.Move(staging, runtime);
            }
            finally
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
            }
        }

        string config = Path.Combine(home, ".env.local");
        string portableConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env.local");
        if (!File.Exists(portableConfig))
            portableConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LibreTV", ".env.local");
        bool firstRun = !File.Exists(config) && !File.Exists(portableConfig);
        string password = null;
        if (firstRun)
        {
            password = Guid.NewGuid().ToString("N");
            string defaults = Path.Combine(runtime, "defaults.env");
            File.WriteAllText(config, File.ReadAllText(defaults) + Environment.NewLine + "PASSWORD=" + password + Environment.NewLine);
        }
        if (File.Exists(portableConfig)) config = portableConfig;

        var start = new ProcessStartInfo(Path.Combine(runtime, "node.exe"),
            "--env-file=\"" + config + "\" \"" + server + "\"")
        {
            WorkingDirectory = runtime,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.EnvironmentVariables["HOSTNAME"] = "127.0.0.1";
        start.EnvironmentVariables["PORT"] = "8080";
        start.EnvironmentVariables.Remove("PASSWORD");
        using (var service = Process.Start(start))
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                if (service.HasExited) break;
                if (IsReady())
                {
                    Process.Start(Address);
                    if (firstRun)
                    {
                        Clipboard.SetText(password);
                        MessageBox.Show("首次启动密码已复制到剪贴板。\n配置文件：" + config, "LibreTV");
                    }
                    return;
                }
                Thread.Sleep(250);
            }
            if (!service.HasExited) service.Kill();
        }
        throw new InvalidOperationException("LibreTV 启动失败。请检查 8080 端口是否被其他程序占用。");
    }

    private static bool IsReady()
    {
        try
        {
            var request = (HttpWebRequest)WebRequest.Create(Address + "api/status");
            request.Proxy = null;
            request.Timeout = 500;
            request.ReadWriteTimeout = 500;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                string status = reader.ReadToEnd();
                return status.Contains("\"passwordRequired\"") && status.Contains("\"defaultLiveSources\"");
            }
        }
        catch (WebException) { return false; }
    }
}
