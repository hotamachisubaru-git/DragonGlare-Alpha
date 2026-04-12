using DragonGlareAlpha.Domain.Startup;

namespace DragonGlareAlpha;

internal sealed class LaunchOptionsDialog : Form
{
    private readonly RadioButton window640Radio;
    private readonly RadioButton window720Radio;
    private readonly RadioButton window1080Radio;
    private readonly RadioButton fullscreenRadio;
    private readonly CheckBox promptOnStartupCheckBox;

    public LaunchSettings SelectedSettings { get; private set; }

    public LaunchOptionsDialog(LaunchSettings initialSettings)
    {
        SelectedSettings = initialSettings;

        Text = "DragonGlare Alpha";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(280, 210);

        var titleLabel = new Label
        {
            AutoSize = true,
            Location = new Point(12, 12),
            Text = "ウィンドウモードを選択してください"
        };

        fullscreenRadio = new RadioButton
        {
            AutoSize = true,
            Location = new Point(12, 42),
            Text = "フルスクリーン（モニターに合わせる）"
        };

        window640Radio = new RadioButton
        {
            AutoSize = true,
            Location = new Point(12, 66),
            Text = "ウィンドウ(640x480)"
        };

        window720Radio = new RadioButton
        {
            AutoSize = true,
            Location = new Point(12, 90),
            Text = "ウィンドウ(720p)"
        };

        window1080Radio = new RadioButton
        {
            AutoSize = true,
            Location = new Point(12, 114),
            Text = "ウィンドウ(1080p)"
        };

        promptOnStartupCheckBox = new CheckBox
        {
            AutoSize = true,
            Location = new Point(28, 146),
            Text = "起動時に毎回聞く",
            Checked = initialSettings.PromptOnStartup
        };

        var startButton = new Button
        {
            AutoSize = true,
            Location = new Point(94, 174),
            Size = new Size(92, 28),
            Text = "ゲーム起動",
            UseVisualStyleBackColor = true
        };
        startButton.Click += (_, _) => ConfirmSelection();

        AcceptButton = startButton;

        Controls.Add(titleLabel);
        Controls.Add(fullscreenRadio);
        Controls.Add(window640Radio);
        Controls.Add(window720Radio);
        Controls.Add(window1080Radio);
        Controls.Add(promptOnStartupCheckBox);
        Controls.Add(startButton);

        SetSelectedDisplayMode(initialSettings.DisplayMode);
    }

    private void SetSelectedDisplayMode(LaunchDisplayMode mode)
    {
        fullscreenRadio.Checked = mode == LaunchDisplayMode.Fullscreen;
        window640Radio.Checked = mode == LaunchDisplayMode.Window640x480;
        window720Radio.Checked = mode == LaunchDisplayMode.Window720p;
        window1080Radio.Checked = mode == LaunchDisplayMode.Window1080p;
    }

    private LaunchDisplayMode GetSelectedDisplayMode()
    {
        if (fullscreenRadio.Checked)
        {
            return LaunchDisplayMode.Fullscreen;
        }

        if (window720Radio.Checked)
        {
            return LaunchDisplayMode.Window720p;
        }

        if (window1080Radio.Checked)
        {
            return LaunchDisplayMode.Window1080p;
        }

        return LaunchDisplayMode.Window640x480;
    }

    private void ConfirmSelection()
    {
        SelectedSettings = new LaunchSettings
        {
            DisplayMode = GetSelectedDisplayMode(),
            PromptOnStartup = promptOnStartupCheckBox.Checked
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}
