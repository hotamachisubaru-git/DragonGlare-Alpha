using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Player;
using DragonGlareAlpha.Domain.Startup;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Security;
using DragonGlareAlpha.Services;

namespace DragonGlareAlpha;

public partial class DragonGlareAlpha
{
    public GameState State => gameState;
    public bool IsSecurityBlocked => skipSaveOnClose;
    public bool IsTransitioning => startupFadeFrames > 0 || pendingGameState is not null;
    public FieldMapId MapId => currentFieldMap;
    public int[,] Map => map;
    public PlayerProgress Player => player;
    public BattleEncounter? Encounter => currentEncounter ?? pendingEncounter;
    public IEnumerable<FieldEventDefinition> VisibleEvents => GetCurrentFieldEvents();
    public UnityEngine.Texture? WorldTexture { get; set; }
    public event Action<BattleVisualCue>? VisualCue;
    public event Action<SoundEffect>? SoundRequested;
    public event Action<BgmTrack?>? MusicRequested;
    public UnityEngine.Vector3 WorldPlayerPosition
    {
        get
        {
            var offset = fieldMovementAnimationFramesRemaining / (float)FieldMovementAnimationDuration;
            return new UnityEngine.Vector3(player.TilePosition.X - fieldMovementAnimationDirection.X * offset,
                0, -(player.TilePosition.Y - fieldMovementAnimationDirection.Y * offset));
        }
    }
    public float FacingDegrees => playerFacingDirection switch
    {
        PlayerFacingDirection.Right => 90, PlayerFacingDirection.Up => 0,
        PlayerFacingDirection.Left => 270, _ => 180
    };

    public DragonGlareAlpha(string saveDirectory, string settingsPath)
    {
        saveService = new SaveService(saveDirectory);
        launchSettingsService = new LaunchSettingsService(settingsPath);
        launchSettings = launchSettingsService.Load();
        activeDisplayMode = launchSettings.DisplayMode;
        lastWindowedDisplayMode = activeDisplayMode == LaunchDisplayMode.Fullscreen ? LaunchDisplayMode.Window720p : activeDisplayMode;
        optionsCursor = (int)activeDisplayMode;
        ApplyDisplayMode();
        fontLoaded = DragonGlareUnity.Drawing.Font.NativeFont is not null;
        gameState = launchSettings.PromptOnStartup ? GameState.StartupOptions : GameState.ModeSelect;
        RefreshSaveSlotSummaries();
    }

    public void Tick(IEnumerable<XnaKeys> held, IEnumerable<XnaKeys> pressed)
    {
        if (skipSaveOnClose) return;
        heldKeys.Clear(); heldKeys.UnionWith(held);
        pressedKeys.Clear(); pressedKeys.UnionWith(pressed);
        if (pressedKeys.Contains(XnaKeys.F11)) ToggleFullscreen();
        try
        {
            UpdateGame();
            UpdateQueuedProgressSave();
        }
        catch (TamperDetectedException ex) { HandleSecurityViolation(ex.Message); }
        finally { pressedKeys.Clear(); }
    }

    public void Render() { using var g = new Graphics(); RenderVirtualFrame(g); }
    public void Shutdown()
    {
        SaveLaunchSettings();
        if (!skipSaveOnClose)
        {
            if (progressSavePending) FlushQueuedProgressSave(refreshSlotSummaries: false);
            else SaveGame(refreshSlotSummaries: false);
        }
    }
    private void ApplyDisplayMode()
    {
        if (UnityEngine.Application.isEditor) return;
        var full = activeDisplayMode == LaunchDisplayMode.Fullscreen;
        var size = GetWindowedClientSize(activeDisplayMode);
        UnityEngine.Screen.SetResolution(full ? UnityEngine.Screen.currentResolution.width : size.Width,
            full ? UnityEngine.Screen.currentResolution.height : size.Height,
            full ? UnityEngine.FullScreenMode.FullScreenWindow : UnityEngine.FullScreenMode.Windowed);
    }
    private void HandleSecurityViolation(string message)
    {
        if (skipSaveOnClose) return;
        skipSaveOnClose = true;
        UnityEngine.Debug.LogError(message);
        Exit();
    }
    private void Exit() { UnityEngine.Application.Quit(); }
    private float? GetApplicationVolume() => UnityEngine.AudioListener.volume;
    private void PlaySe(SoundEffect sound) => SoundRequested?.Invoke(sound);
    private void UpdateBgm()
    {
        BgmTrack? next = gameState switch
        {
            GameState.LanguageSelection when !languageOpeningFinished => BgmTrack.Prologue,
            GameState.Battle => BgmTrack.Battle,
            GameState.Field or GameState.EncounterTransition => currentFieldMap is FieldMapId.Castle or FieldMapId.Dungeon ? BgmTrack.Castle : BgmTrack.Field,
            GameState.ShopBuy or GameState.Bank => BgmTrack.Shop,
            _ => BgmTrack.MainMenu
        };
        if (currentBgmTrack == next) return;
        currentBgmTrack = next;
        MusicRequested?.Invoke(next);
    }

    private Image? LoadResourceImage(string group, string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = group + "/" + Path.GetFileNameWithoutExtension(name);
        if (uiImages.TryGetValue(key, out var cached)) return cached;
        var texture = UnityEngine.Resources.Load<UnityEngine.Texture2D>("DragonGlare/" + key);
        if (texture is null) return null;
        var image = new Image(texture); uiImages[key] = image; return image;
    }
    private Image? GetUiImage(string? name) => LoadResourceImage("UI", name);
    private Image? GetNpcPortrait(string? name) => LoadResourceImage("Portraits", name);
    private Image? GetNpcSprite(string? name) => null; // Field characters are 3D objects.
    private Image? GetEnemySprite(string? name) => null; // Battle enemies are 3D objects.
    private Image? GetHeroSprite() => null;
    private bool TryDrawFieldTileSprite(Graphics g, Point point, Rectangle rect) => false;
    private bool TryDrawFieldMapImage(Graphics g, Rectangle viewport, Point camera) => false;
}
