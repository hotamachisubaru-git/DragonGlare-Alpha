using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;
using Timer = System.Windows.Forms.Timer;

namespace DragonGlareAlpha;

public partial class Form1 : Form
{
    private const int VirtualWidth = 640;
    private const int VirtualHeight = 480;
    private const int TileSize = 32;
    private static readonly Point PlayerStartTile = new(3, 12);
    private static readonly Point NpcTile = new(12, 7);
    private static readonly Point HubFromCastleTile = new(9, 2);
    private static readonly Point CastleEntryTile = new(9, 12);
    private static readonly Point HubFromFieldTile = new(15, 7);
    private static readonly Point FieldEntryTile = new(2, 7);

    private readonly Timer gameTimer = new() { Interval = 16 };
    private readonly HashSet<Keys> heldKeys = [];
    private readonly HashSet<Keys> pressedKeys = [];
    private readonly PrivateFontCollection privateFontCollection = new();
    private readonly StringBuilder playerName = new();
    private readonly System.Windows.Media.MediaPlayer bgmPlayer = new();
    private readonly System.Windows.Media.MediaPlayer sePlayer = new();
    private readonly Dictionary<BgmTrack, Uri> bgmUris = [];
    private readonly Dictionary<SoundEffect, Uri> seUris = [];
    private readonly Random random = new();
    private readonly SaveService saveService = new();
    private readonly BattleService battleService = new();
    private readonly ProgressionService progressionService = new();
    private readonly ShopService shopService = new();

    private Font uiFont = new("Consolas", 20, GraphicsUnit.Pixel);
    private Font smallFont = new("Consolas", 16, GraphicsUnit.Pixel);

    private PlayerProgress player = PlayerProgress.CreateDefault(PlayerStartTile);
    private BattleEncounter? currentEncounter;
    private GameState gameState = GameState.ModeSelect;
    private FieldMapId currentFieldMap = FieldMapId.Hub;
    private int[,] map = MapFactory.CreateDefaultMap();
    private UiLanguage selectedLanguage = UiLanguage.Japanese;
    private int modeCursor;
    private int languageCursor;
    private int nameCursorRow;
    private int nameCursorColumn;
    private int movementCooldown;
    private bool isNpcDialogOpen;
    private bool isFieldStatusVisible;
    private bool fontLoaded;
    private int frameCounter;
    private int startupFadeFrames = 20;
    private int battleCursorRow;
    private int battleCursorColumn;
    private BattleFlowState battleFlowState = BattleFlowState.CommandSelection;
    private int shopPromptCursor;
    private int shopItemCursor;
    private ShopPhase shopPhase = ShopPhase.Welcome;
    private string battleMessage = "まものが あらわれた！";
    private string shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
    private BgmTrack? currentBgmTrack;
    private string menuNotice = string.Empty;
    private int menuNoticeFrames;

    private string SaveFilePath => Path.Combine(AppContext.BaseDirectory, "savegame.json");

    public Form1()
    {
        InitializeComponent();
        ConfigureWindow();
        LoadCustomFont();
        InitializeAudio();

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) => CleanupResources();

        gameTimer.Tick += (_, _) =>
        {
            UpdateGame();
            Invalidate();
            pressedKeys.Clear();
        };
        gameTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.Clear(Color.Black);
        e.Graphics.SmoothingMode = SmoothingMode.None;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
        e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        var scale = Math.Min((float)ClientSize.Width / VirtualWidth, (float)ClientSize.Height / VirtualHeight);
        var drawWidth = VirtualWidth * scale;
        var drawHeight = VirtualHeight * scale;
        var offsetX = (ClientSize.Width - drawWidth) / 2f;
        var offsetY = (ClientSize.Height - drawHeight) / 2f;

        e.Graphics.TranslateTransform(offsetX, offsetY);
        e.Graphics.ScaleTransform(scale, scale);

        switch (gameState)
        {
            case GameState.ModeSelect:
                DrawModeSelect(e.Graphics);
                break;
            case GameState.LanguageSelection:
                DrawLanguageSelection(e.Graphics);
                break;
            case GameState.NameInput:
                DrawNameInput(e.Graphics);
                break;
            case GameState.Field:
                DrawField(e.Graphics);
                break;
            case GameState.Battle:
                DrawBattle(e.Graphics);
                break;
            case GameState.ShopBuy:
                DrawShopBuy(e.Graphics);
                break;
        }

        if (!fontLoaded)
        {
            DrawWindow(e.Graphics, new Rectangle(8, 8, 624, 44));
            DrawText(e.Graphics, "TTF NOT FOUND: USING FALLBACK FONT", 20, 20);
        }

        if (startupFadeFrames > 0)
        {
            var alpha = (int)(255f * startupFadeFrames / 20f);
            using var fadeBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black));
            e.Graphics.FillRectangle(fadeBrush, 0, 0, VirtualWidth, VirtualHeight);
        }
    }

    private void ConfigureWindow()
    {
        Text = $"DragonGlare Alpha v{Application.ProductVersion}";
        ClientSize = new Size(960, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ShowIcon = true;
        KeyPreview = true;
        DoubleBuffered = true;

        try
        {
            using var applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (applicationIcon is not null)
            {
                Icon = (Icon)applicationIcon.Clone();
            }
        }
        catch
        {
        }
    }

    private void CleanupResources()
    {
        SaveGame();
        gameTimer.Stop();
        gameTimer.Dispose();
        bgmPlayer.Stop();
        bgmPlayer.Close();
        sePlayer.Stop();
        sePlayer.Close();
        uiFont.Dispose();
        smallFont.Dispose();
        privateFontCollection.Dispose();
    }
}
