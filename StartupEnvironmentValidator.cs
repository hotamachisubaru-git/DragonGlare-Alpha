using System.Runtime.InteropServices;

namespace DragonGlareAlpha;

internal static class StartupEnvironmentValidator
{
    private const int Windows11Build = 22000;

    internal static string? GetUnsupportedMessage()
    {
        if (!OperatingSystem.IsWindows())
        {
            return "DragonGlare Alpha は Windows 専用です。";
        }

        if (RuntimeInformation.OSArchitecture != Architecture.X64
            || RuntimeInformation.ProcessArchitecture != Architecture.X64
            || !Environment.Is64BitProcess)
        {
            return
                $"DragonGlare Alpha は Windows 11 x64 専用です。{Environment.NewLine}"
                + $"現在の環境: OS={RuntimeInformation.OSArchitecture}, Process={RuntimeInformation.ProcessArchitecture}{Environment.NewLine}"
                + "x86 / 32 ビット環境では動作しません。";
        }

        // .NET 5+ returns the real Windows version even when compatibility mode is enabled.
        var version = Environment.OSVersion.Version;
        var isWindows11OrLater = version.Major > 10 || (version.Major == 10 && version.Build >= Windows11Build);
        if (!isWindows11OrLater)
        {
            return
                $"DragonGlare Alpha は Windows 11 x64 専用です。{Environment.NewLine}"
                + $"現在の Windows バージョン: {version.Major}.{version.Minor}.{version.Build}{Environment.NewLine}"
                + $"Windows 11 (build {Windows11Build} 以降) で実行してください。";
        }

        return null;
    }
}
