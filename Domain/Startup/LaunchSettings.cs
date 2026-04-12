namespace DragonGlareAlpha.Domain.Startup;

public sealed class LaunchSettings
{
    public LaunchDisplayMode DisplayMode { get; init; } = LaunchDisplayMode.Window640x480;
    public bool PromptOnStartup { get; init; } = true;
}
