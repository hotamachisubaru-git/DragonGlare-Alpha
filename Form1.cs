using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Security;
using DragonGlareAlpha.Services;
using Timer = System.Windows.Forms.Timer;

namespace DragonGlareAlpha;

public partial class Form1 : Form
{
    private const int TileSize = 32;
    private const int CompactFieldViewportWidthTiles = 13;
    private const int ExpandedFieldViewportWidthTiles = 17;
    private const int CompactFieldViewportHeightTiles = 9;
    private const int ExpandedFieldViewportHeightTiles = 11;
    private const int ExpandedFieldViewportVerticalTrim = 16;
    private const int FieldMovementAnimationDuration = 6;
    private const int EncounterTransitionDuration = 26;
    private static readonly Point PlayerStartTile = new(3, 12);
    private static readonly Point HubFromCastleTile = new(9, 2);
    private static readonly Point CastleEntryTile = new(9, 12);
    private static readonly Point HubFromFieldTile = new(15, 7);
    private static readonly Point FieldEntryTile = new(2, 7);
    private static readonly TimeSpan BgmLoopLeadTime = TimeSpan.FromMilliseconds(120);

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
    private readonly AntiCheatService antiCheatService = new();
    private readonly BattleService battleService = new();
    private readonly ProgressionService progressionService = new();
    private readonly ShopService shopService = new();
    private readonly FieldEventService fieldEventService = new();

    private Font uiFont = new(UiTypography.DefaultFontFamilyName, UiTypography.FontPixelSize, GraphicsUnit.Pixel);
    private Font smallFont = new(UiTypography.DefaultFontFamilyName, UiTypography.FontPixelSize, GraphicsUnit.Pixel);

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
    private int saveSlotCursor;
    private int activeSaveSlot;
    private int movementCooldown;
    private bool isFieldDialogOpen;
    private bool isFieldStatusVisible;
    private bool fontLoaded;
    private int frameCounter;
    private int startupFadeFrames = 20;
    private Point fieldMovementAnimationDirection = Point.Empty;
    private int fieldMovementAnimationFramesRemaining;
    private int battleCursorRow;
    private int battleCursorColumn;
    private BattleFlowState battleFlowState = BattleFlowState.CommandSelection;
    private int shopPromptCursor;
    private int shopItemCursor;
    private ShopPhase shopPhase = ShopPhase.Welcome;
    private SaveSlotSelectionMode saveSlotSelectionMode = SaveSlotSelectionMode.Save;
    private string battleMessage = "まものが あらわれた！";
    private string shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
    private BgmTrack? currentBgmTrack;
    private string menuNotice = string.Empty;
    private int menuNoticeFrames;
    private bool skipSaveOnClose;
    private int encounterTransitionFrames;
    private int fieldEncounterStepsRemaining = 7;
    private int enemyHitFlashFramesRemaining;
    private BattleEncounter? pendingEncounter;
    private IReadOnlyList<string> activeFieldDialogPages = [];
    private int activeFieldDialogPageIndex;
    private IReadOnlyList<SaveSlotSummary> saveSlotSummaries = [];

    private string LegacySaveFilePath => Path.Combine(AppContext.BaseDirectory, "savegame.json");

    public Form1()
    {
        InitializeComponent();
        ConfigureWindow();
        LoadCustomFont();
        InitializeAudio();
        saveService.TryMigrateLegacySave(LegacySaveFilePath);
        RefreshSaveSlotSummaries();

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += (_, _) => CleanupResources();

        gameTimer.Tick += (_, _) =>
        {
            try
            {
                UpdateGame();
                Invalidate();
            }
            catch (TamperDetectedException ex)
            {
                HandleSecurityViolation(ex.Message);
            }
            finally
            {
                pressedKeys.Clear();
            }
        };
        gameTimer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            base.OnPaint(e);

            e.Graphics.Clear(Color.Black);
            e.Graphics.SmoothingMode = SmoothingMode.None;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            e.Graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            var scale = Math.Min((float)ClientSize.Width / UiCanvas.VirtualWidth, (float)ClientSize.Height / UiCanvas.VirtualHeight);
            var drawWidth = UiCanvas.VirtualWidth * scale;
            var drawHeight = UiCanvas.VirtualHeight * scale;
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
                case GameState.SaveSlotSelection:
                    DrawSaveSlotSelection(e.Graphics);
                    break;
                case GameState.Field:
                    DrawField(e.Graphics);
                    break;
                case GameState.EncounterTransition:
                    DrawEncounterTransition(e.Graphics);
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
                DrawWindow(e.Graphics, UiCanvas.FontFallbackWindow);
                DrawText(e.Graphics, "TTF NOT FOUND: USING FALLBACK FONT", 20, 20);
            }

            if (startupFadeFrames > 0)
            {
                var alpha = (int)(255f * startupFadeFrames / 20f);
                using var fadeBrush = new SolidBrush(Color.FromArgb(alpha, Color.Black));
                e.Graphics.FillRectangle(fadeBrush, 0, 0, UiCanvas.VirtualWidth, UiCanvas.VirtualHeight);
            }
        }
        catch (TamperDetectedException ex)
        {
            HandleSecurityViolation(ex.Message);
        }
    }

    private void ConfigureWindow()
    {
        Text = $"DragonGlare Alpha v{Application.ProductVersion}";
        ClientSize = UiCanvas.WindowClientSize;
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
        if (!skipSaveOnClose)
        {
            try
            {
                SaveGame();
            }
            catch (TamperDetectedException)
            {
            }
        }

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

    private void HandleSecurityViolation(string message)
    {
        if (skipSaveOnClose)
        {
            return;
        }

        skipSaveOnClose = true;
        gameTimer.Stop();
        Hide();
        MessageBox.Show(message, "DragonGlare Alpha", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        Close();
    }
}
