namespace DragonGlareAlpha.Security;

public sealed class TamperDetectedException(string message) : InvalidOperationException(message);
