namespace DragonGlareAlpha;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        var unsupportedMessage = StartupEnvironmentValidator.GetUnsupportedMessage();
        if (unsupportedMessage is not null)
        {
            MessageBox.Show(
                unsupportedMessage,
                "DragonGlare Alpha",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}
