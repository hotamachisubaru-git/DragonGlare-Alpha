using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Timer = System.Windows.Forms.Timer;

namespace DragonGlareAlpha;

public partial class Form1 : Form
{
    private const int VirtualWidth = 640;
    private const int VirtualHeight = 480;
    private const int TileSize = 32;
    private const int InitialPlayerHp = 20;
    private const int InitialPlayerMp = 2;
    private const int InitialPlayerGold = 220;
    private const int MaxPlayerGold = InitialPlayerGold;
    private const int EnemyMaxHp = 18;
    private const int CurrentSaveVersion = 1;
    private const string SaveIntegrityPepper = "DragonGlareAlpha.SaveSeal.v1";
    private static readonly Point PlayerStartTile = new(3, 12);
    private static readonly Point FieldEnemyTile = new(15, 5);

    private readonly AssetLoader assetLoader = new();
    private readonly Timer gameTimer = new() { Interval = 16 };
    private readonly HashSet<Keys> heldKeys = [];
    private readonly HashSet<Keys> pressedKeys = [];
    private readonly PrivateFontCollection privateFontCollection = new();
    private readonly StringBuilder playerName = new();
    private readonly int[,] map = new int[15, 20];
    private readonly System.Windows.Media.MediaPlayer bgmPlayer = new();
    private readonly System.Windows.Media.MediaPlayer sePlayer = new();
    private readonly Dictionary<BgmTrack, Uri> bgmUris = [];
    private readonly Dictionary<SoundEffect, Uri> seUris = [];
    private readonly Image? fieldTileSprite;
    private readonly Image heroSprite;
    private readonly Image enemySprite;

    private Font uiFont = new("Consolas", 20, GraphicsUnit.Pixel);
    private Font smallFont = new("Consolas", 16, GraphicsUnit.Pixel);

    private GameState gameState = GameState.ModeSelect;
    private UiLanguage selectedLanguage = UiLanguage.Japanese;
    private int modeCursor;
    private int languageCursor;
    private int nameCursorRow;
    private int nameCursorColumn;
    private int movementCooldown;
    private bool isNpcDialogOpen;
    private Point playerTile = PlayerStartTile;
    private Point npcTile = new(12, 7);
    private int playerHp = InitialPlayerHp;
    private int playerMp = InitialPlayerMp;
    private int playerGold = InitialPlayerGold;
    private bool fontLoaded;
    private int frameCounter;
    private int startupFadeFrames = 20;
    private int battleCursorRow;
    private int battleCursorColumn;
    private int enemyHp = EnemyMaxHp;
    private BattlePhase battlePhase = BattlePhase.CommandSelection;
    private int shopPromptCursor;
    private int shopItemCursor;
    private ShopPhase shopPhase = ShopPhase.Welcome;
    private string battleMessage = "まものが あらわれた！";
    private string shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
    private BgmTrack? currentBgmTrack;
    private string menuNotice = string.Empty;
    private int menuNoticeFrames;

    private string SaveFilePath => Path.Combine(AppContext.BaseDirectory, "savegame.json");

    private static readonly string[][] JapaneseNameTable =
    [
        ["あ", "い", "う", "え", "お", "か", "き", "く", "け", "こ"],
        ["さ", "し", "す", "せ", "そ", "た", "ち", "つ", "て", "と"],
        ["な", "に", "ぬ", "ね", "の", "は", "ひ", "ふ", "へ", "ほ"],
        ["ま", "み", "む", "め", "も", "や", "ゆ", "よ", "わ", "を"],
        ["ら", "り", "る", "れ", "ろ", "ん", "ー", "゛", "けす", "おわり"]
    ];

    private static readonly string[][] EnglishNameTable =
    [
        ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"],
        ["K", "L", "M", "N", "O", "P", "Q", "R", "S", "T"],
        ["U", "V", "W", "X", "Y", "Z", "-", "'", "DEL", "END"]
    ];

    private static readonly string[,] BattleCommands =
    {
        { "こうげき", "じゅもん" },
        { "どうぐ", "にげる" }
    };

    private static readonly (string Name, int Price)[] ShopCatalog =
    [
        ("ぼう", 16),
        ("こんぼう", 32),
        ("とげのぼう", 64),
        ("ぼくとう", 82),
        ("いしのおの", 128),
        ("どうのつるぎ", 196)
    ];

    public Form1()
    {
        InitializeComponent();
        fieldTileSprite = TryLoadFieldTile();
        heroSprite = assetLoader.LoadCharacter("hero");
        enemySprite = assetLoader.LoadEnemy("enemy_slime");
        ConfigureWindow();
        InitializeMap();
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
        Text = "DragonGlare Alpha";
        ClientSize = new Size(960, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        KeyPreview = true;
        DoubleBuffered = true;
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
        assetLoader.Dispose();
        uiFont.Dispose();
        smallFont.Dispose();
        privateFontCollection.Dispose();
    }

    private void LoadCustomFont()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "JF-Dot-ShinonomeMin14.ttf"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "JF-Dot-ShinonomeMin14.ttf"),
            Path.Combine(Directory.GetCurrentDirectory(), "JF-Dot-ShinonomeMin14.ttf")
        };

        string? fontPath = null;
        foreach (var candidate in candidates)
        {
            var normalized = Path.GetFullPath(candidate);
            if (File.Exists(normalized))
            {
                fontPath = normalized;
                break;
            }
        }

        if (fontPath is null)
        {
            return;
        }

        privateFontCollection.AddFontFile(fontPath);
        if (privateFontCollection.Families.Length == 0)
        {
            return;
        }

        uiFont.Dispose();
        smallFont.Dispose();
        uiFont = new Font(privateFontCollection.Families[0], 28, FontStyle.Regular, GraphicsUnit.Pixel);
        smallFont = new Font(privateFontCollection.Families[0], 22, FontStyle.Regular, GraphicsUnit.Pixel);
        fontLoaded = true;
    }

    private void InitializeAudio()
    {
        bgmPlayer.Volume = 0.45;
        sePlayer.Volume = 0.9;
        bgmPlayer.MediaEnded += (_, _) =>
        {
            bgmPlayer.Position = TimeSpan.Zero;
            bgmPlayer.Play();
        };

        RegisterBgm(BgmTrack.MainMenu, "SFC_glare.mp3");
        RegisterBgm(BgmTrack.Field, "SFC_field.mp3");
        RegisterBgm(BgmTrack.Castle, "SFC_castle.mp3");

        RegisterSe(SoundEffect.Dialog, "Serif_SE.mp3");
        RegisterSe(SoundEffect.Collision, "当たり判定SFC.mp3", "当たり判定SFC.wav");

        UpdateBgm();
    }

    private Image? TryLoadFieldTile()
    {
        try
        {
            return assetLoader.LoadTile("mapTile_Assets_SFCFrame1");
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private void RegisterBgm(BgmTrack track, params string[] fileNames)
    {
        var path = ResolveAssetPath("BGM", fileNames);
        if (path is not null)
        {
            bgmUris[track] = new Uri(path, UriKind.Absolute);
        }
    }

    private void RegisterSe(SoundEffect effect, params string[] fileNames)
    {
        var path = ResolveAssetPath("SE", fileNames);
        if (path is not null)
        {
            seUris[effect] = new Uri(path, UriKind.Absolute);
        }
    }

    private static string? ResolveAssetPath(string assetFolder, params string[] fileNames)
    {
        var assetRoot = Path.GetFullPath(
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", assetFolder));

        foreach (var name in fileNames)
        {
            var candidate = Path.GetFullPath(Path.Combine(assetRoot, name));
            if (!candidate.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void InitializeMap()
    {
        for (var y = 0; y < map.GetLength(0); y++)
        {
            for (var x = 0; x < map.GetLength(1); x++)
            {
                map[y, x] = x == 0 || y == 0 || x == map.GetLength(1) - 1 || y == map.GetLength(0) - 1 ? 1 : 0;
            }
        }

        for (var y = 1; y <= 4; y++)
        {
            for (var x = 1; x <= 4; x++)
            {
                map[y, x] = 2;
            }
        }

        for (var x = 4; x <= 15; x++)
        {
            map[10, x] = 1;
        }

        map[10, 9] = 0;
        map[10, 10] = 0;
        map[6, 6] = 1;
        map[6, 7] = 1;
        map[7, 6] = 1;
    }

    private void UpdateGame()
    {
        frameCounter++;
        NormalizeRuntimeState();

        if (startupFadeFrames > 0)
        {
            startupFadeFrames--;
        }

        if (menuNoticeFrames > 0)
        {
            menuNoticeFrames--;
            if (menuNoticeFrames == 0)
            {
                menuNotice = string.Empty;
            }
        }

        switch (gameState)
        {
            case GameState.ModeSelect:
                UpdateModeSelect();
                break;
            case GameState.LanguageSelection:
                UpdateLanguageSelection();
                break;
            case GameState.NameInput:
                UpdateNameInput();
                break;
            case GameState.Field:
                UpdateField();
                break;
            case GameState.Battle:
                UpdateBattle();
                break;
            case GameState.ShopBuy:
                UpdateShopBuy();
                break;
        }

        UpdateBgm();
    }

    private void UpdateModeSelect()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            modeCursor = 0;
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            modeCursor = 1;
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        if (modeCursor == 0)
        {
            StartNewGame();
            return;
        }

        if (TryLoadGame())
        {
            gameState = GameState.Field;
            return;
        }

        if (string.IsNullOrEmpty(menuNotice))
        {
            menuNotice = "NO SAVE DATA / セーブデータがありません";
            menuNoticeFrames = 180;
        }

        PlaySe(SoundEffect.Collision);
    }

    private void UpdateLanguageSelection()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            languageCursor = 0;
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            languageCursor = 1;
        }

        if (WasPressed(Keys.Enter))
        {
            selectedLanguage = languageCursor == 0 ? UiLanguage.Japanese : UiLanguage.English;
            gameState = GameState.NameInput;
            playerName.Clear();
            nameCursorRow = 0;
            nameCursorColumn = 0;
        }

        if (WasPressed(Keys.Escape))
        {
            gameState = GameState.ModeSelect;
        }
    }

    private void UpdateNameInput()
    {
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            MoveNameCursor(0, -1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            MoveNameCursor(0, 1);
        }
        else if (WasPressed(Keys.Left) || WasPressed(Keys.A))
        {
            MoveNameCursor(-1, 0);
        }
        else if (WasPressed(Keys.Right) || WasPressed(Keys.D))
        {
            MoveNameCursor(1, 0);
        }

        if (WasPressed(Keys.Back))
        {
            RemoveLastCharacter();
        }

        if (WasPressed(Keys.Escape))
        {
            gameState = GameState.LanguageSelection;
            return;
        }

        if (WasPressed(Keys.Enter))
        {
            AddSelectedCharacter();
        }
    }

    private void UpdateField()
    {
        if (isNpcDialogOpen)
        {
            if (WasPressed(Keys.Enter) || WasPressed(Keys.Escape))
            {
                isNpcDialogOpen = false;
            }
            return;
        }

        if (WasPressed(Keys.B))
        {
            EnterBattle();
            return;
        }

        if (WasPressed(Keys.V))
        {
            EnterShopBuy();
            return;
        }

        if (movementCooldown > 0)
        {
            movementCooldown--;
        }

        var movement = Point.Empty;
        if (heldKeys.Contains(Keys.Up) || heldKeys.Contains(Keys.W))
        {
            movement = new Point(0, -1);
        }
        else if (heldKeys.Contains(Keys.Down) || heldKeys.Contains(Keys.S))
        {
            movement = new Point(0, 1);
        }
        else if (heldKeys.Contains(Keys.Left) || heldKeys.Contains(Keys.A))
        {
            movement = new Point(-1, 0);
        }
        else if (heldKeys.Contains(Keys.Right) || heldKeys.Contains(Keys.D))
        {
            movement = new Point(1, 0);
        }

        if (movement != Point.Empty && movementCooldown == 0)
        {
            var moved = TryMovePlayer(movement);
            if (!moved)
            {
                PlaySe(SoundEffect.Collision);
            }

            movementCooldown = 6;
        }

        if (WasPressed(Keys.Enter) && IsAdjacent(playerTile, npcTile))
        {
            isNpcDialogOpen = true;
            PlaySe(SoundEffect.Dialog);
        }
    }

    private void UpdateBattle()
    {
        if (battlePhase == BattlePhase.CommandSelection && WasPressed(Keys.Escape))
        {
            battleMessage = "うまく にげきった！";
            battlePhase = BattlePhase.Escape;
            return;
        }

        if (battlePhase == BattlePhase.CommandSelection && (WasPressed(Keys.Up) || WasPressed(Keys.W)))
        {
            battleCursorRow = Math.Max(0, battleCursorRow - 1);
        }
        else if (battlePhase == BattlePhase.CommandSelection && (WasPressed(Keys.Down) || WasPressed(Keys.S)))
        {
            battleCursorRow = Math.Min(1, battleCursorRow + 1);
        }
        else if (battlePhase == BattlePhase.CommandSelection && (WasPressed(Keys.Left) || WasPressed(Keys.A)))
        {
            battleCursorColumn = Math.Max(0, battleCursorColumn - 1);
        }
        else if (battlePhase == BattlePhase.CommandSelection && (WasPressed(Keys.Right) || WasPressed(Keys.D)))
        {
            battleCursorColumn = Math.Min(1, battleCursorColumn + 1);
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        if (battlePhase == BattlePhase.CommandSelection)
        {
            ExecuteBattleCommand(BattleCommands[battleCursorRow, battleCursorColumn]);
            return;
        }

        switch (battlePhase)
        {
            case BattlePhase.PlayerActionResult:
                ResolveEnemyTurn();
                break;
            case BattlePhase.EnemyActionResult:
                battlePhase = BattlePhase.CommandSelection;
                battleMessage = "どうする？";
                break;
            case BattlePhase.Victory:
            case BattlePhase.Escape:
                ExitBattle(false);
                break;
            case BattlePhase.Defeat:
                ExitBattle(true);
                break;
        }
    }

    private void UpdateShopBuy()
    {
        if (shopPhase == ShopPhase.Welcome)
        {
            if (WasPressed(Keys.Up) || WasPressed(Keys.W) || WasPressed(Keys.Down) || WasPressed(Keys.S))
            {
                shopPromptCursor = 1 - shopPromptCursor;
            }

            if (WasPressed(Keys.Escape))
            {
                gameState = GameState.Field;
                return;
            }

            if (!WasPressed(Keys.Enter))
            {
                return;
            }

            if (shopPromptCursor == 0)
            {
                shopPhase = ShopPhase.BuyList;
                shopItemCursor = 0;
                shopMessage = "＊「なにを かっていくかい？」";
                return;
            }

            gameState = GameState.Field;
            return;
        }

        var maxIndex = ShopCatalog.Length;
        if (WasPressed(Keys.Up) || WasPressed(Keys.W))
        {
            shopItemCursor = Math.Max(0, shopItemCursor - 1);
        }
        else if (WasPressed(Keys.Down) || WasPressed(Keys.S))
        {
            shopItemCursor = Math.Min(maxIndex, shopItemCursor + 1);
        }

        if (WasPressed(Keys.Escape))
        {
            shopPhase = ShopPhase.Welcome;
            shopMessage = "＊「ほかに ようじは あるかい？」";
            return;
        }

        if (!WasPressed(Keys.Enter))
        {
            return;
        }

        if (shopItemCursor == maxIndex)
        {
            shopPhase = ShopPhase.Welcome;
            shopMessage = "＊「また きてくれよな！」";
            return;
        }

        var item = ShopCatalog[shopItemCursor];
        if (playerGold < item.Price)
        {
            shopMessage = "＊「おかねが たりないね。」";
            return;
        }

        playerGold -= item.Price;
        shopMessage = $"＊「{item.Name}を かった！」";
    }

    private void EnterBattle()
    {
        ResetBattleState();
        gameState = GameState.Battle;
        PlaySe(SoundEffect.Dialog);
    }

    private void ResetBattleState()
    {
        battleCursorRow = 0;
        battleCursorColumn = 0;
        enemyHp = EnemyMaxHp;
        battlePhase = BattlePhase.CommandSelection;
        battleMessage = "まものが あらわれた！";
    }

    private void ExecuteBattleCommand(string command)
    {
        switch (command)
        {
            case "こうげき":
                ResolvePlayerAttack(6, "こうげき！");
                break;
            case "じゅもん":
                if (playerMp <= 0)
                {
                    battleMessage = "MPが たりない！";
                    return;
                }

                playerMp--;
                ResolvePlayerAttack(12, "メラ！");
                break;
            case "どうぐ":
                battleMessage = "どうぐは まだない。";
                break;
            case "にげる":
                battleMessage = "うまく にげきった！";
                battlePhase = BattlePhase.Escape;
                break;
        }
    }

    private void ResolvePlayerAttack(int damage, string actionLabel)
    {
        enemyHp = Math.Max(0, enemyHp - damage);
        if (enemyHp == 0)
        {
            battleMessage = $"{actionLabel} まものを やっつけた！";
            battlePhase = BattlePhase.Victory;
            return;
        }

        battleMessage = $"{actionLabel} まものに {damage}ダメージ！";
        battlePhase = BattlePhase.PlayerActionResult;
    }

    private void ResolveEnemyTurn()
    {
        const int damage = 4;
        playerHp = Math.Max(0, playerHp - damage);
        if (playerHp == 0)
        {
            battleMessage = $"まものの こうげき！ {damage}ダメージを うけた！\n{GetBattlePlayerName()}は ちからつきた…。";
            battlePhase = BattlePhase.Defeat;
            return;
        }

        battleMessage = $"まものの こうげき！ {damage}ダメージを うけた！";
        battlePhase = BattlePhase.EnemyActionResult;
    }

    private void ExitBattle(bool defeated)
    {
        if (defeated)
        {
            playerTile = PlayerStartTile;
            playerHp = InitialPlayerHp;
            playerMp = InitialPlayerMp;
        }

        ResetBattleState();
        gameState = GameState.Field;
    }

    private string GetBattlePlayerName()
    {
        return playerName.Length == 0 ? "のりたま" : playerName.ToString();
    }

    private void EnterShopBuy()
    {
        shopPhase = ShopPhase.Welcome;
        shopPromptCursor = 0;
        shopItemCursor = 0;
        shopMessage = "＊「いらっしゃい！\n　なにを かっていくかい？」";
        gameState = GameState.ShopBuy;
        PlaySe(SoundEffect.Dialog);
    }

    private void UpdateBgm()
    {
        var desiredTrack = gameState switch
        {
            GameState.Battle => BgmTrack.Field,
            GameState.ShopBuy => BgmTrack.Field,
            GameState.Field when IsInsideCastleZone(playerTile) => BgmTrack.Castle,
            GameState.Field => BgmTrack.Field,
            _ => BgmTrack.MainMenu
        };

        if (currentBgmTrack == desiredTrack)
        {
            return;
        }

        if (!bgmUris.TryGetValue(desiredTrack, out var trackUri))
        {
            currentBgmTrack = null;
            bgmPlayer.Stop();
            return;
        }

        currentBgmTrack = desiredTrack;
        bgmPlayer.Open(trackUri);
        bgmPlayer.Position = TimeSpan.Zero;
        bgmPlayer.Play();
    }

    private void PlaySe(SoundEffect effect)
    {
        if (!seUris.TryGetValue(effect, out var seUri))
        {
            return;
        }

        sePlayer.Open(seUri);
        sePlayer.Position = TimeSpan.Zero;
        sePlayer.Play();
    }

    private static bool IsInsideCastleZone(Point tile)
    {
        return tile.X >= 1 && tile.X <= 4 && tile.Y >= 1 && tile.Y <= 4;
    }

    private void DrawModeSelect(Graphics g)
    {
        DrawMenuBackdrop(g);
        DrawTitleText(g, "メインメニュー", 24, 60);

        DrawWindow(g, new Rectangle(118, 236, 408, 172));
        DrawText(g, "モードをせんたくしてください", 154, 268, smallFont);
        DrawOption(g, modeCursor == 0, 154, 307, "さいしょから  NEW GAME");
        DrawOption(g, modeCursor == 1, 154, 342, "つづきから  LOAD GAME");
        DrawText(g, "MODE SELECT", 154, 377);

        if (!string.IsNullOrWhiteSpace(menuNotice))
        {
            DrawWindow(g, new Rectangle(84, 24, 472, 72));
            DrawText(g, menuNotice, 104, 48, smallFont);
        }
    }

    private void DrawLanguageSelection(Graphics g)
    {
        DrawWindow(g, new Rectangle(84, 64, 260, 128));
        DrawOption(g, languageCursor == 0, 104, 94, "にほんご");
        DrawOption(g, languageCursor == 1, 104, 134, "ENGLISH");

        DrawWindow(g, new Rectangle(116, 270, 410, 180));
        DrawText(g, "げんごをえらんでください", 140, 310);
        DrawText(g, "CHOOSE A LANGUAGE", 140, 350);
    }

    private void DrawNameInput(Graphics g)
    {
        var table = selectedLanguage == UiLanguage.Japanese ? JapaneseNameTable : EnglishNameTable;
        var originX = 44;
        var originY = 52;
        var cellWidth = 56;
        var cellHeight = 44;

        for (var row = 0; row < table.Length; row++)
        {
            for (var column = 0; column < table[row].Length; column++)
            {
                var textX = originX + (column * cellWidth);
                var textY = originY + (row * cellHeight);
                var isCursor = row == nameCursorRow && column == nameCursorColumn;
                if (isCursor)
                {
                    DrawText(g, "▶", textX - 24, textY);
                }

                DrawText(g, table[row][column], textX, textY);
            }
        }

        DrawWindow(g, new Rectangle(116, 270, 410, 180));
        DrawText(g, "なまえをきめてください", 140, 300);
        DrawText(g, "CHOOSE A NAME", 140, 338);
        DrawText(g, playerName.Length == 0 ? "..." : playerName.ToString(), 140, 384);

        DrawText(g, selectedLanguage == UiLanguage.Japanese ? "ESC: もどる" : "ESC: BACK", 14, 442);
    }

    private void DrawField(Graphics g)
    {
        DrawFieldScene(g);

        DrawWindow(g, new Rectangle(8, 8, 474, 84));
        DrawText(g, GetText("fieldHelpLine1"), 20, 24, smallFont);
        DrawText(g, GetText("fieldHelpLine2"), 20, 54, smallFont);

        if (isNpcDialogOpen)
        {
            DrawWindow(g, new Rectangle(46, 320, 548, 138));
            DrawText(g, GetText("npcLine1"), 72, 354, smallFont);
            DrawText(g, GetText("npcLine2"), 72, 392, smallFont);
        }
    }

    private void DrawBattle(Graphics g)
    {
        DrawMenuBackdrop(g);

        var statusName = GetBattlePlayerName();
        DrawWindow(g, new Rectangle(76, 34, 246, 132));
        DrawText(g, statusName, 102, 52);
        DrawText(g, $"HP: {playerHp}", 102, 88);
        DrawText(g, $"MP: {playerMp}", 102, 124);

        DrawWindow(g, new Rectangle(332, 34, 236, 40));
        DrawText(g, "こうどう", 420, 46);

        DrawWindow(g, new Rectangle(332, 74, 236, 92));
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 2; column++)
            {
                var x = 366 + (column * 108);
                var y = 92 + (row * 36);
                if (battlePhase == BattlePhase.CommandSelection && battleCursorRow == row && battleCursorColumn == column)
                {
                    DrawSelectionMarker(g, x - 24, y + 10);
                }

                DrawText(g, BattleCommands[row, column], x, y);
            }
        }

        DrawBattleEnemy(g, new Point(320, 210));

        DrawWindow(g, new Rectangle(112, 248, 416, 196));
        DrawText(g, battleMessage, 136, 282);
    }

    private void DrawShopBuy(Graphics g)
    {
        DrawFieldScene(g);

        var itemWindow = new Rectangle(332, 34, 240, 236);
        var messageWindow = new Rectangle(92, 292, 426, 152);
        var itemStartY = 82;
        var itemRowSpacing = 28;

        DrawWindow(g, new Rectangle(44, 74, 240, 120));
        DrawOption(g, shopPhase == ShopPhase.Welcome && shopPromptCursor == 0, 114, 104, "はい");
        DrawOption(g, shopPhase == ShopPhase.Welcome && shopPromptCursor == 1, 114, 142, "いいえ");

        DrawWindow(g, itemWindow);
        var shopTitle = "しょうひん";
        var titleSize = g.MeasureString(shopTitle, uiFont);
        var titleX = itemWindow.X + (int)Math.Round((itemWindow.Width - titleSize.Width) / 2f);
        DrawText(g, shopTitle, titleX, 48);
        for (var i = 0; i < ShopCatalog.Length; i++)
        {
            var rowY = itemStartY + (i * itemRowSpacing);
            if (shopPhase == ShopPhase.BuyList && shopItemCursor == i)
            {
                DrawSelectionMarker(g, 348, rowY + 10);
            }

            DrawText(g, ShopCatalog[i].Name, 372, rowY);
            DrawText(g, ShopCatalog[i].Price.ToString(), 500, rowY);
        }

        var quitY = itemStartY + (ShopCatalog.Length * itemRowSpacing) + 4;
        if (shopPhase == ShopPhase.BuyList && shopItemCursor == ShopCatalog.Length)
        {
            DrawSelectionMarker(g, 348, quitY + 10);
        }

        DrawText(g, "やめる", 372, quitY);
        DrawText(g, $"G {playerGold}", 488, quitY, smallFont);

        DrawWindow(g, messageWindow);
        DrawText(g, shopMessage, 120, 324);
    }

    private void DrawFieldScene(Graphics g)
    {
        using var floorBrush = new SolidBrush(Color.FromArgb(5, 5, 5));
        using var wallBrush = new SolidBrush(Color.FromArgb(140, 8, 30, 90));
        using var castleBrush = new SolidBrush(Color.FromArgb(120, 80, 20, 20));

        for (var y = 0; y < map.GetLength(0); y++)
        {
            for (var x = 0; x < map.GetLength(1); x++)
            {
                var tileRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                if (map[y, x] == 1)
                {
                    g.FillRectangle(wallBrush, tileRect);
                }
                else if (map[y, x] == 2)
                {
                    g.FillRectangle(castleBrush, tileRect);
                }
                else if (fieldTileSprite is not null)
                {
                    g.DrawImage(fieldTileSprite, tileRect);
                }
                else
                {
                    g.FillRectangle(floorBrush, tileRect);
                }
            }
        }

        DrawTileEntity(g, npcTile, Color.Cyan);
        DrawSpriteAtTile(g, enemySprite, FieldEnemyTile);
        DrawSpriteAtTile(g, heroSprite, playerTile);
    }

    private void DrawBattleEnemy(Graphics g, Point center)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(220, 230, 242));
        using var eyeBrush = new SolidBrush(Color.Black);
        using var outlinePen = new Pen(Color.FromArgb(90, 150, 220), 2);
        using var hornPen = new Pen(Color.FromArgb(220, 230, 242), 3);

        var body = new Rectangle(center.X - 16, center.Y - 12, 32, 24);
        g.FillEllipse(bodyBrush, body);
        g.DrawEllipse(outlinePen, body);

        g.DrawLine(hornPen, center.X - 10, center.Y - 14, center.X - 17, center.Y - 20);
        g.DrawLine(hornPen, center.X + 10, center.Y - 14, center.X + 17, center.Y - 20);

        g.FillEllipse(eyeBrush, center.X - 8, center.Y - 4, 4, 4);
        g.FillEllipse(eyeBrush, center.X + 4, center.Y - 4, 4, 4);
        g.DrawLine(outlinePen, center.X - 6, center.Y + 5, center.X + 6, center.Y + 5);
    }

    private void DrawTileEntity(Graphics g, Point tile, Color color)
    {
        var rect = new Rectangle(tile.X * TileSize + 4, tile.Y * TileSize + 4, TileSize - 8, TileSize - 8);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);
    }

    private static Point GetSpriteDrawPosition(Point tile)
    {
        var drawX = tile.X * TileSize + 8;
        var drawY = tile.Y * TileSize + 8;
        return new Point(drawX, drawY);
    }

    private static void DrawSpriteAtTile(Graphics g, Image sprite, Point tile)
    {
        var drawPosition = GetSpriteDrawPosition(tile);
        g.DrawImage(sprite, drawPosition.X, drawPosition.Y, sprite.Width, sprite.Height);
    }

    private void DrawWindow(Graphics g, Rectangle rect)
    {
        using var background = new SolidBrush(Color.Black);
        using var shadowBrush = new SolidBrush(Color.FromArgb(96, 0, 0, 0));
        using var glowPen = new Pen(Color.FromArgb(0, 72, 255), 6);
        using var outerPen = new Pen(Color.FromArgb(0, 120, 255), 3);
        using var innerPen = new Pen(Color.FromArgb(132, 206, 255), 1);

        g.FillRectangle(shadowBrush, rect.X + 6, rect.Y + 6, rect.Width, rect.Height);
        g.FillRectangle(background, rect);
        g.DrawRectangle(glowPen, rect);
        g.DrawRectangle(outerPen, rect);
        var innerRect = Rectangle.Inflate(rect, -7, -7);
        g.DrawRectangle(innerPen, innerRect);
    }

    private void DrawOption(Graphics g, bool selected, int x, int y, string text)
    {
        if (selected)
        {
            DrawSelectionMarker(g, x - 28, y + 10);
        }

        DrawText(g, text, x, y);
    }

    private void DrawText(Graphics g, string text, int x, int y, Font? fontOverride = null)
    {
        using var brush = new SolidBrush(Color.White);
        g.DrawString(text, fontOverride ?? uiFont, brush, x, y);
    }

    private void DrawMenuBackdrop(Graphics g)
    {
        using var gradient = new LinearGradientBrush(
            new Rectangle(0, 0, VirtualWidth, VirtualHeight),
            Color.Black,
            Color.FromArgb(0, 10, 22),
            90f);
        using var scanlinePen = new Pen(Color.FromArgb(24, 38, 80));
        using var sideGlowBrush = new SolidBrush(Color.FromArgb(14, 0, 80, 255));

        g.FillRectangle(gradient, 0, 0, VirtualWidth, VirtualHeight);

        for (var y = 0; y < VirtualHeight; y += 4)
        {
            g.DrawLine(scanlinePen, 0, y, VirtualWidth, y);
        }

        g.FillRectangle(sideGlowBrush, 0, 0, 18, VirtualHeight);
        g.FillRectangle(sideGlowBrush, VirtualWidth - 18, 0, 18, VirtualHeight);
    }

    private void DrawTitleText(Graphics g, string text, int x, int y)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(44, 106, 255));
        using var mainBrush = new SolidBrush(Color.FromArgb(238, 244, 255));

        g.DrawString(text, uiFont, shadowBrush, x + 4, y + 4);
        g.DrawString(text, uiFont, mainBrush, x, y);
    }

    private void DrawSelectionMarker(Graphics g, int x, int y)
    {
        if ((frameCounter / 18) % 2 == 1)
        {
            return;
        }

        using var shadowBrush = new SolidBrush(Color.FromArgb(0, 56, 180));
        using var baseBrush = new SolidBrush(Color.FromArgb(0, 120, 255));
        using var shineBrush = new SolidBrush(Color.FromArgb(180, 226, 255));

        g.FillRectangle(shadowBrush, x + 2, y + 2, 12, 12);
        g.FillRectangle(baseBrush, x, y, 12, 12);
        g.FillRectangle(shineBrush, x + 3, y + 3, 4, 4);
    }

    private string GetText(string key)
    {
        var name = playerName.Length == 0 ? "PLAYER" : playerName.ToString();
        return (selectedLanguage, key) switch
        {
            (UiLanguage.Japanese, "fieldHelpLine1") => "やじるし:いどう  ENTER:はなす",
            (UiLanguage.English, "fieldHelpLine1") => "ARROWS: MOVE  ENTER: TALK",
            (UiLanguage.Japanese, "fieldHelpLine2") => "B:バトル  V:ショップ",
            (UiLanguage.English, "fieldHelpLine2") => "B:BATTLE  V:SHOP",
            (UiLanguage.Japanese, "npcLine1") => $"{name}、ようこそ。",
            (UiLanguage.Japanese, "npcLine2") => "アセットがとどくのをまっています。",
            (UiLanguage.English, "npcLine1") => $"Welcome, {name}.",
            (UiLanguage.English, "npcLine2") => "We are waiting for your assets.",
            _ => string.Empty
        };
    }

    private void NormalizeRuntimeState()
    {
        playerHp = Math.Clamp(playerHp, 0, InitialPlayerHp);
        playerMp = Math.Clamp(playerMp, 0, InitialPlayerMp);
        playerGold = Math.Clamp(playerGold, 0, MaxPlayerGold);
        enemyHp = Math.Clamp(enemyHp, 0, EnemyMaxHp);
        movementCooldown = Math.Max(0, movementCooldown);
        battleCursorRow = Math.Clamp(battleCursorRow, 0, 1);
        battleCursorColumn = Math.Clamp(battleCursorColumn, 0, 1);
        shopPromptCursor = Math.Clamp(shopPromptCursor, 0, 1);
        shopItemCursor = Math.Clamp(shopItemCursor, 0, ShopCatalog.Length);

        if (!IsWalkableTile(playerTile) || playerTile == npcTile)
        {
            playerTile = PlayerStartTile;
        }

        if (!Enum.IsDefined(typeof(BattlePhase), battlePhase))
        {
            battlePhase = BattlePhase.CommandSelection;
        }

        if (!Enum.IsDefined(typeof(ShopPhase), shopPhase))
        {
            shopPhase = ShopPhase.Welcome;
        }

        if (!Enum.IsDefined(typeof(UiLanguage), selectedLanguage))
        {
            selectedLanguage = UiLanguage.Japanese;
        }
    }

    private void StartNewGame()
    {
        selectedLanguage = UiLanguage.Japanese;
        languageCursor = 0;
        nameCursorRow = 0;
        nameCursorColumn = 0;
        playerTile = PlayerStartTile;
        playerName.Clear();
        playerHp = InitialPlayerHp;
        playerMp = InitialPlayerMp;
        playerGold = InitialPlayerGold;
        isNpcDialogOpen = false;
        movementCooldown = 0;
        ResetBattleState();
        gameState = GameState.LanguageSelection;
    }

    private bool TryLoadGame()
    {
        menuNotice = string.Empty;
        menuNoticeFrames = 0;

        try
        {
            if (!File.Exists(SaveFilePath))
            {
                return false;
            }

            var json = File.ReadAllText(SaveFilePath);
            var save = JsonSerializer.Deserialize<SaveData>(json);
            if (save is null)
            {
                ShowInvalidSaveNotice();
                return false;
            }

            var isLegacySave = save.Version == 0 && string.IsNullOrWhiteSpace(save.Integrity);
            if (!isLegacySave && !HasValidSaveIntegrity(save))
            {
                ShowInvalidSaveNotice();
                return false;
            }

            if (!IsSafeSavePayload(save))
            {
                ShowInvalidSaveNotice();
                return false;
            }

            selectedLanguage = string.Equals(save.Language, "en", StringComparison.OrdinalIgnoreCase)
                ? UiLanguage.English
                : UiLanguage.Japanese;

            playerName.Clear();
            if (!string.IsNullOrWhiteSpace(save.Name))
            {
                var trimmedName = save.Name.Length > 10 ? save.Name[..10] : save.Name;
                playerName.Append(trimmedName);
            }

            var loadedTile = new Point(save.PlayerX, save.PlayerY);
            playerTile = IsWalkableTile(loadedTile) && loadedTile != npcTile ? loadedTile : PlayerStartTile;
            playerHp = InitialPlayerHp;
            playerMp = InitialPlayerMp;
            playerGold = InitialPlayerGold;
            isNpcDialogOpen = false;
            movementCooldown = 0;
            ResetBattleState();

            if (isLegacySave)
            {
                SaveGame(true);
            }

            return true;
        }
        catch
        {
            ShowInvalidSaveNotice();
            return false;
        }
    }

    private void SaveGame(bool force = false)
    {
        if (!force && gameState != GameState.Field)
        {
            return;
        }

        var save = new SaveData
        {
            Version = CurrentSaveVersion,
            Language = selectedLanguage == UiLanguage.English ? "en" : "ja",
            Name = playerName.ToString(),
            PlayerX = playerTile.X,
            PlayerY = playerTile.Y
        };
        save.Integrity = ComputeSaveIntegrity(save);

        try
        {
            var json = JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFilePath, json);
        }
        catch
        {
        }
    }

    private static string ComputeSaveIntegrity(SaveData save)
    {
        var normalizedLanguage = string.Equals(save.Language, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "ja";
        var normalizedName = Convert.ToBase64String(Encoding.UTF8.GetBytes(save.Name ?? string.Empty));
        var payload = $"{save.Version}|{normalizedLanguage}|{normalizedName}|{save.PlayerX}|{save.PlayerY}|{SaveIntegrityPepper}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }

    private static bool HasValidSaveIntegrity(SaveData save)
    {
        return save.Version == CurrentSaveVersion
            && !string.IsNullOrWhiteSpace(save.Integrity)
            && string.Equals(save.Integrity, ComputeSaveIntegrity(save), StringComparison.Ordinal);
    }

    private bool IsSafeSavePayload(SaveData save)
    {
        if (!string.Equals(save.Language, "ja", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(save.Language, "en", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if ((save.Name?.Length ?? 0) > 10)
        {
            return false;
        }

        var loadedTile = new Point(save.PlayerX, save.PlayerY);
        return IsWalkableTile(loadedTile) && loadedTile != npcTile;
    }

    private void ShowInvalidSaveNotice()
    {
        menuNotice = "SAVE DATA INVALID / セーブデータを検証できません";
        menuNoticeFrames = 240;
    }

    private void MoveNameCursor(int deltaX, int deltaY)
    {
        var table = selectedLanguage == UiLanguage.Japanese ? JapaneseNameTable : EnglishNameTable;
        nameCursorRow = Math.Clamp(nameCursorRow + deltaY, 0, table.Length - 1);
        var maxColumn = table[nameCursorRow].Length - 1;
        nameCursorColumn = Math.Clamp(nameCursorColumn + deltaX, 0, maxColumn);
    }

    private void AddSelectedCharacter()
    {
        var table = selectedLanguage == UiLanguage.Japanese ? JapaneseNameTable : EnglishNameTable;
        var selected = table[nameCursorRow][nameCursorColumn];

        var deleteToken = selectedLanguage == UiLanguage.Japanese ? "けす" : "DEL";
        var endToken = selectedLanguage == UiLanguage.Japanese ? "おわり" : "END";

        if (selected == deleteToken)
        {
            RemoveLastCharacter();
            return;
        }

        if (selected == endToken)
        {
            if (playerName.Length > 0)
            {
                gameState = GameState.Field;
                SaveGame();
            }
            return;
        }

        if (playerName.Length < 10)
        {
            playerName.Append(selected);
        }
    }

    private void RemoveLastCharacter()
    {
        if (playerName.Length > 0)
        {
            playerName.Remove(playerName.Length - 1, 1);
        }
    }

    private bool IsWalkableTile(Point tile)
    {
        if (tile.X < 0 || tile.Y < 0 || tile.X >= map.GetLength(1) || tile.Y >= map.GetLength(0))
        {
            return false;
        }

        return map[tile.Y, tile.X] != 1;
    }

    private bool TryMovePlayer(Point movement)
    {
        var target = new Point(playerTile.X + movement.X, playerTile.Y + movement.Y);
        if (!IsWalkableTile(target) || target == npcTile)
        {
            return false;
        }

        playerTile = target;
        SaveGame();
        return true;
    }

    private static bool IsAdjacent(Point a, Point b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) == 1;
    }

    private bool WasPressed(Keys key) => pressedKeys.Contains(key);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!heldKeys.Contains(e.KeyCode))
        {
            pressedKeys.Add(e.KeyCode);
        }

        heldKeys.Add(e.KeyCode);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        heldKeys.Remove(e.KeyCode);
    }

    private sealed class SaveData
    {
        public int Version { get; set; }
        public string Language { get; set; } = "ja";
        public string Name { get; set; } = string.Empty;
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
        public string Integrity { get; set; } = string.Empty;
    }

    private enum GameState
    {
        ModeSelect,
        LanguageSelection,
        NameInput,
        Field,
        Battle,
        ShopBuy
    }

    private enum ShopPhase
    {
        Welcome,
        BuyList
    }

    private enum BattlePhase
    {
        CommandSelection,
        PlayerActionResult,
        EnemyActionResult,
        Victory,
        Escape,
        Defeat
    }

    private enum UiLanguage
    {
        Japanese,
        English
    }

    private enum BgmTrack
    {
        MainMenu,
        Field,
        Castle
    }

    private enum SoundEffect
    {
        Dialog,
        Collision
    }
}

