using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DragonGlareAlpha.Security;

public sealed partial class AntiCheatService
{
    private static readonly string[] SuspiciousProcessKeywords =
    [
        "cheatengine",
        "cheat engine",
        "artmoney",
        "x64dbg",
        "x32dbg",
        "ollydbg",
        "dnspy",
        "ilspy",
        "processhacker",
        "scanmem"
    ];

    private static readonly string[] SuspiciousWindowKeywords =
    [
        "cheat engine",
        "artmoney",
        "x64dbg",
        "x32dbg",
        "dnspy",
        "process hacker"
    ];

    public static bool TryDetectStartupViolation(out string message)
    {
        return new AntiCheatService().TryDetectViolation(out message);
    }

    public bool TryDetectViolation(out string message)
    {
        if (Debugger.IsAttached || IsDebuggerPresent() || IsRemoteDebuggerAttached())
        {
            message = "デバッガを検知したため起動を停止しました。";
            return true;
        }

        if (TryFindSuspiciousProcess(out var processLabel))
        {
            message = $"不正ツールを検知したため終了します。\n検知対象: {processLabel}";
            return true;
        }

        message = string.Empty;
        return false;
    }

    private static bool TryFindSuspiciousProcess(out string processLabel)
    {
        processLabel = string.Empty;

        try
        {
            using var currentProcess = Process.GetCurrentProcess();
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    if (process.Id == currentProcess.Id)
                    {
                        continue;
                    }

                    var processName = process.ProcessName ?? string.Empty;
                    var windowTitle = string.Empty;
                    try
                    {
                        windowTitle = process.MainWindowTitle ?? string.Empty;
                    }
                    catch
                    {
                    }

                    if (ContainsSuspiciousKeyword(processName, SuspiciousProcessKeywords) ||
                        ContainsSuspiciousKeyword(windowTitle, SuspiciousWindowKeywords))
                    {
                        processLabel = string.IsNullOrWhiteSpace(windowTitle)
                            ? processName
                            : $"{processName} ({windowTitle})";
                        return true;
                    }
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool ContainsSuspiciousKeyword(string value, IEnumerable<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRemoteDebuggerAttached()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var attached = false;
            return CheckRemoteDebuggerPresent(process.Handle, ref attached) && attached;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CheckRemoteDebuggerPresent(
        IntPtr processHandle,
        [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);
}
