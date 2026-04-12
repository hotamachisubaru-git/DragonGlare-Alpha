using DragonGlareAlpha.Security;
using DragonGlareAlpha.Domain.Startup;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var platformSupportService = new PlatformSupportService();
        if (platformSupportService.TryDetectUnsupportedPlatform(out var platformMessage))
        {
            MessageBox.Show(platformMessage, "DragonGlare Alpha", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return;
        }

        if (AntiCheatService.TryDetectStartupViolation(out var message))
        {
            MessageBox.Show(message, "DragonGlare Alpha", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return;
        }

        var launchSettingsService = new LaunchSettingsService();
        var launchSettings = launchSettingsService.Load();

        if (launchSettings.PromptOnStartup)
        {
            using var launchOptionsDialog = new LaunchOptionsDialog(launchSettings);
            if (launchOptionsDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            launchSettings = launchOptionsDialog.SelectedSettings;
            launchSettingsService.Save(launchSettings);
        }

        Application.Run(new Form1(launchSettings));
    }
}
