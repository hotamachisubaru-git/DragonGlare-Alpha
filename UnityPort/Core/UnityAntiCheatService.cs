namespace DragonGlareAlpha.Security;

/// <summary>
/// Unity keeps ProtectedInt validation and signed, DPAPI-protected saves. Editor
/// debugging and unrelated processes are not treated as a game integrity violation.
/// </summary>
public sealed class AntiCheatService
{
    public static bool TryDetectStartupViolation(out string message)
    {
        message = string.Empty;
        return false;
    }

    public bool TryDetectViolation(out string message)
    {
        message = string.Empty;
        return false;
    }

    private static bool ContainsSuspiciousKeyword(string value, IEnumerable<string> keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
