using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Diagnostics;
using System.IO;

public class BuildAndDeploy : EditorWindow
{
    private const string ADB_PATH      = @"C:\Users\IO\AppData\Local\Android\Sdk\platform-tools\adb.exe";
    private const string BUILD_PATH    = @"C:\Builds\PerseeTemplate.apk";
    private const string PACKAGE_NAME  = "com.InteractiveOmaha.PerseeTests";
    private const string DEVICE_IP_KEY = "BuildAndDeploy_DeviceIP";

    private static string deviceIP = "";
    private static Process logcatProcess = null;

    [MenuItem("Build/Build and Deploy to Persee")]
    public static void ShowWindow()
    {
        var window = GetWindow<BuildAndDeploy>("Build & Deploy");
        window.minSize = new Vector2(360, 160);
        deviceIP = EditorPrefs.GetString(DEVICE_IP_KEY, "");
    }

    void OnGUI()
    {
        // Restore IP from prefs if lost after recompile
        if(string.IsNullOrEmpty(deviceIP))
            deviceIP = EditorPrefs.GetString(DEVICE_IP_KEY, "");
            
        GUILayout.Space(10);
        GUILayout.Label("Persee 2 Build & Deploy", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Device IP:", GUILayout.Width(80));
        string newIP = GUILayout.TextField(deviceIP, GUILayout.Width(160));
        if(newIP != deviceIP)
        {
            deviceIP = newIP;
            EditorPrefs.SetString(DEVICE_IP_KEY, deviceIP);
        }
        GUILayout.Label(":5555", GUILayout.Width(45));
        GUILayout.EndHorizontal();

        GUILayout.Space(5);
        GUILayout.Label($"APK: {BUILD_PATH}", EditorStyles.miniLabel);
        GUILayout.Label($"Package: {PACKAGE_NAME}", EditorStyles.miniLabel);

        GUILayout.Space(10);

        if(GUILayout.Button("Connect to Device", GUILayout.Height(30)))
            ConnectToDevice();

        GUILayout.Space(5);

        bool logcatRunning = logcatProcess != null && !logcatProcess.HasExited;
        if(logcatRunning)
        {
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if(GUILayout.Button("■ Stop Logcat", GUILayout.Height(30)))
                StopLogcat();
        }
        else
        {
            GUI.backgroundColor = Color.white;
            if(GUILayout.Button("▶ Start Logcat", GUILayout.Height(30)))
                StartLogcat();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);

        if(GUILayout.Button("BUILD + DEPLOY", GUILayout.Height(40)))
            BuildAndDeployAPK();
    }

    private static void StartLogcat()
    {
        if(!File.Exists(ADB_PATH))
        {
            UnityEngine.Debug.LogError($"Build & Deploy: adb not found at {ADB_PATH}");
            return;
        }

        StopLogcat();

        var psi = new ProcessStartInfo
        {
            FileName               = ADB_PATH,
            Arguments              = "logcat -s Unity",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        logcatProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

        logcatProcess.OutputDataReceived += (sender, e) =>
        {
            if(!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.Log($"[Logcat] {e.Data}");
        };
        logcatProcess.ErrorDataReceived += (sender, e) =>
        {
            if(!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.LogWarning($"[Logcat] {e.Data}");
        };

        logcatProcess.Start();
        logcatProcess.BeginOutputReadLine();
        logcatProcess.BeginErrorReadLine();

        UnityEngine.Debug.Log("Build & Deploy: Logcat started. Unity logs will appear here.");
    }

    private static void StopLogcat()
    {
        if(logcatProcess != null && !logcatProcess.HasExited)
        {
            logcatProcess.Kill();
            logcatProcess = null;
            UnityEngine.Debug.Log("Build & Deploy: Logcat stopped.");
        }
    }

    private static void ConnectToDevice()
    {
        if(string.IsNullOrEmpty(deviceIP))
        {
            UnityEngine.Debug.LogError("Build & Deploy: No device IP set.");
            return;
        }
        RunAdb($"connect {deviceIP}:5555", "Connect");
    }

    private static void BuildAndDeployAPK()
    {
        if(string.IsNullOrEmpty(deviceIP))
        {
            UnityEngine.Debug.LogError("Build & Deploy: No device IP set.");
            return;
        }

        // Ensure output directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(BUILD_PATH));

        // Build
        UnityEngine.Debug.Log("Build & Deploy: Starting build...");
        var report = BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            BUILD_PATH,
            BuildTarget.Android,
            BuildOptions.None
        );

        if(report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"Build & Deploy: Build failed with {report.summary.totalErrors} errors.");
            return;
        }

        UnityEngine.Debug.Log("Build & Deploy: Build succeeded. Installing...");

        // Install
        bool installed = RunAdb($"install -r \"{BUILD_PATH}\"", "Install");
        if(!installed) return;

        UnityEngine.Debug.Log("Build & Deploy: Install succeeded. Launching...");

        // Launch
        RunAdb($"shell am start -n {PACKAGE_NAME}/com.unity3d.player.UnityPlayerActivity", "Launch");
    }

    // Returns true if the process exited with code 0
    private static bool RunAdb(string args, string label)
    {
        if(!File.Exists(ADB_PATH))
        {
            UnityEngine.Debug.LogError($"Build & Deploy: adb not found at {ADB_PATH}");
            return false;
        }

        var psi = new ProcessStartInfo
        {
            FileName               = ADB_PATH,
            Arguments              = args,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using(var process = Process.Start(psi))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error  = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if(!string.IsNullOrEmpty(output))
                UnityEngine.Debug.Log($"Build & Deploy [{label}]: {output.Trim()}");
            if(!string.IsNullOrEmpty(error))
                UnityEngine.Debug.LogWarning($"Build & Deploy [{label}]: {error.Trim()}");

            if(process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError($"Build & Deploy [{label}]: adb exited with code {process.ExitCode}");
                return false;
            }
        }

        return true;
    }
}
