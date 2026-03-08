using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Text;
using System.Text.Json;
using Timer = System.Windows.Forms.Timer;

namespace DragonGlareAlpha;

public partial class Form1 : Form
{
    private const int VirtualWidth = 640;
    private const int VirtualHeight = 480;
    private const int TileSize = 32;
    private static readonly Point PlayerStartTile = new(3, 12);

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
    private bool fontLoaded;
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

    public Form1()
    {
        InitializeComponent();
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
        }

        if (!fontLoaded)
        {
            DrawWindow(e.Graphics, new Rectangle(8, 8, 624, 44));
            DrawText(e.Graphics, "TTF NOT FOUND: USING FALLBACK FONT", 20, 20);
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

    private void RegisterBgm(BgmTrack track, params string[] fileNames)
    {
        var path = ResolveAssetPath(fileNames);
        if (path is not null)
        {
            bgmUris[track] = new Uri(path, UriKind.Absolute);
        }
    }

    private void RegisterSe(SoundEffect effect, params string[] fileNames)
    {
        var path = ResolveAssetPath(fileNames);
        if (path is not null)
        {
            seUris[effect] = new Uri(path, UriKind.Absolute);
        }
    }

    private static string? ResolveAssetPath(params string[] fileNames)
    {
        foreach (var name in fileNames)
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "アセット", name),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "アセット", name),
                Path.Combine(Directory.GetCurrentDirectory(), "アセット", name),
                Path.Combine(AppContext.BaseDirectory, "Assets", "Audio", name),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "Audio", name),
                Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Audio", name)
            };

            foreach (var candidate in candidates)
            {
                var normalized = Path.GetFullPath(candidate);
                if (File.Exists(normalized))
                {
                    return normalized;
                }
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

        menuNotice = "NO SAVE DATA / セーブデータがありません";
        menuNoticeFrames = 180;
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

    private void UpdateBgm()
    {
        var desiredTrack = gameState switch
        {
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
        DrawText(g, "メインメニュー", 34, 58);

        DrawWindow(g, new Rectangle(116, 236, 410, 198));
        DrawText(g, "モードをせんたくしてください", 152, 268, smallFont);
        DrawOption(g, modeCursor == 0, 152, 305, "さいしょから  NEW GAME");
        DrawOption(g, modeCursor == 1, 152, 344, "つづきから  LOAD GAME");
        DrawText(g, "MODE SELECT", 152, 382);

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
        for (var y = 0; y < map.GetLength(0); y++)
        {
            for (var x = 0; x < map.GetLength(1); x++)
            {
                var tileRect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
                if (map[y, x] == 1)
                {
                    using var wallBrush = new SolidBrush(Color.FromArgb(8, 30, 90));
                    g.FillRectangle(wallBrush, tileRect);
                }
                else if (map[y, x] == 2)
                {
                    using var castleBrush = new SolidBrush(Color.FromArgb(80, 20, 20));
                    g.FillRectangle(castleBrush, tileRect);
                }
                else
                {
                    using var floorBrush = new SolidBrush(Color.FromArgb(5, 5, 5));
                    g.FillRectangle(floorBrush, tileRect);
                }
            }
        }

        DrawTileEntity(g, npcTile, Color.Cyan);
        DrawTileEntity(g, playerTile, Color.White);

        DrawWindow(g, new Rectangle(8, 8, 430, 84));
        DrawText(g, GetText("fieldHelp"), 20, 26, smallFont);
        DrawText(g, IsInsideCastleZone(playerTile) ? GetText("areaCastle") : GetText("areaField"), 20, 56, smallFont);

        if (isNpcDialogOpen)
        {
            DrawWindow(g, new Rectangle(46, 320, 548, 138));
            DrawText(g, GetText("npcLine1"), 72, 354, smallFont);
            DrawText(g, GetText("npcLine2"), 72, 392, smallFont);
        }
    }

    private void DrawTileEntity(Graphics g, Point tile, Color color)
    {
        var rect = new Rectangle(tile.X * TileSize + 4, tile.Y * TileSize + 4, TileSize - 8, TileSize - 8);
        using var brush = new SolidBrush(color);
        g.FillRectangle(brush, rect);
    }

    private void DrawWindow(Graphics g, Rectangle rect)
    {
        using var background = new SolidBrush(Color.Black);
        using var outerPen = new Pen(Color.FromArgb(0, 120, 255), 4);
        using var innerPen = new Pen(Color.FromArgb(80, 180, 255), 2);

        g.FillRectangle(background, rect);
        g.DrawRectangle(outerPen, rect);
        var innerRect = Rectangle.Inflate(rect, -5, -5);
        g.DrawRectangle(innerPen, innerRect);
    }

    private void DrawOption(Graphics g, bool selected, int x, int y, string text)
    {
        if (selected)
        {
            DrawText(g, "▶", x - 24, y);
        }

        DrawText(g, text, x, y);
    }

    private void DrawText(Graphics g, string text, int x, int y, Font? fontOverride = null)
    {
        using var brush = new SolidBrush(Color.White);
        g.DrawString(text, fontOverride ?? uiFont, brush, x, y);
    }

    private string GetText(string key)
    {
        var name = playerName.Length == 0 ? "PLAYER" : playerName.ToString();
        return (selectedLanguage, key) switch
        {
            (UiLanguage.Japanese, "fieldHelp") => "やじるし:いどう  ENTER:はなす",
            (UiLanguage.English, "fieldHelp") => "ARROWS: MOVE  ENTER: TALK",
            (UiLanguage.Japanese, "areaField") => "フィールドBGM: SFC_field",
            (UiLanguage.English, "areaField") => "FIELD BGM: SFC_field",
            (UiLanguage.Japanese, "areaCastle") => "おしろBGM: SFC_castle",
            (UiLanguage.English, "areaCastle") => "CASTLE BGM: SFC_castle",
            (UiLanguage.Japanese, "npcLine1") => $"{name}、ようこそ。",
            (UiLanguage.Japanese, "npcLine2") => "アセットがとどくのをまっています。",
            (UiLanguage.English, "npcLine1") => $"Welcome, {name}.",
            (UiLanguage.English, "npcLine2") => "We are waiting for your assets.",
            _ => string.Empty
        };
    }

    private void StartNewGame()
    {
        selectedLanguage = UiLanguage.Japanese;
        languageCursor = 0;
        nameCursorRow = 0;
        nameCursorColumn = 0;
        playerTile = PlayerStartTile;
        playerName.Clear();
        isNpcDialogOpen = false;
        movementCooldown = 0;
        gameState = GameState.LanguageSelection;
    }

    private bool TryLoadGame()
    {
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
            isNpcDialogOpen = false;
            movementCooldown = 0;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SaveGame()
    {
        if (gameState != GameState.Field)
        {
            return;
        }

        var save = new SaveData
        {
            Language = selectedLanguage == UiLanguage.English ? "en" : "ja",
            Name = playerName.ToString(),
            PlayerX = playerTile.X,
            PlayerY = playerTile.Y
        };

        try
        {
            var json = JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SaveFilePath, json);
        }
        catch
        {
        }
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
        public string Language { get; set; } = "ja";
        public string Name { get; set; } = string.Empty;
        public int PlayerX { get; set; }
        public int PlayerY { get; set; }
    }

    private enum GameState
    {
        ModeSelect,
        LanguageSelection,
        NameInput,
        Field
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
