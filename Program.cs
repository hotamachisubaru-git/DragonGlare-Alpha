using DragonGlareAlpha.Security;

namespace DragonGlareAlpha;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        if (AntiCheatService.TryDetectStartupViolation(out var message))
        {
            MessageBox.Show(message, "DragonGlare Alpha", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }    
}
