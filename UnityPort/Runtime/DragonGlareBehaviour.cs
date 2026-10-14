using UnityEngine;
using DragonGlareAlpha.Domain;
using GameSession = DragonGlareAlpha.DragonGlareAlpha;
using GameSound = DragonGlareAlpha.Domain.SoundEffect;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace DragonGlareUnity;

public sealed class DragonGlareBehaviour : MonoBehaviour
{
    public GameSession Session { get; private set; } = null!;
    public DragonGlareWorld World { get; private set; } = null!;
    public int RenderedFrames { get; private set; }
    private readonly HashSet<Keys> previousHeld = new();
    private readonly HashSet<Keys> pendingPressed = new();
    private readonly HashSet<Keys> held = new();
    private AudioSource music = null!, sound = null!;
    private AudioClip? pendingMusic;
    private float musicFade;
    private double clock;
    private bool ready;
    private bool closed;
    private bool smoke;
    private bool pendingMusicLoop = true;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Application.runInBackground = true;
        Drawing.Font.NativeFont = Resources.Load<UnityEngine.Font>("DragonGlare/Fonts/JF-Dot-ShinonomeMin14");
        var args = Environment.GetCommandLineArgs();
        smoke = args.Contains("-dragonGlareSmoke");
        var index = Array.IndexOf(args, "-dragonGlareSmokeOutput");
        var dataRoot = smoke ? (index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : Path.Combine(Application.persistentDataPath, "Smoke", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"))) : Path.Combine(Application.persistentDataPath, "LumisValidation-20261014");
        Session = new GameSession(Path.Combine(dataRoot, "saves"), Path.Combine(dataRoot, "launch_settings.json"));
        World = new DragonGlareWorld(transform);
        Session.WorldTexture = World.Texture;
        Session.VisualCue += World.PlayCue;
        music = gameObject.AddComponent<AudioSource>(); music.loop = true; music.spatialBlend = 0;
        sound = gameObject.AddComponent<AudioSource>(); sound.spatialBlend = 0;
        Session.MusicRequested += SetMusic;
        Session.SoundRequested += PlaySound;
        var settings = new DragonGlareAlpha.Services.LaunchSettingsService(Path.Combine(dataRoot, "launch_settings.json")).Load();
        AudioListener.volume = settings.Volume ?? 0.85f;
        if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        var outputObject = new GameObject("UI Output Camera"); outputObject.transform.SetParent(transform, false);
        var outputCamera = outputObject.AddComponent<Camera>();
        outputCamera.cullingMask = 0; outputCamera.clearFlags = CameraClearFlags.SolidColor;
        outputCamera.backgroundColor = UnityEngine.Color.black; outputCamera.depth = -100;
        ready = true;
        if (smoke) StartCoroutine(UnitySmoke.Run(this, dataRoot));
    }

    private void Update()
    {
        if (!ready || Session.IsSecurityBlocked) return;
        if (!smoke) ReadInput();
        clock += Math.Min(Time.unscaledDeltaTime, 0.25f);
        while (clock >= 1d / 60d)
        {
            Session.Tick(held, pendingPressed); pendingPressed.Clear(); clock -= 1d / 60d;
        }
        World.Update(Session, Time.unscaledDeltaTime);
        if (pendingMusic is not null && music.clip != pendingMusic)
        {
            music.volume = Mathf.MoveTowards(music.volume, 0, Time.unscaledDeltaTime * 3);
            if (music.volume <= 0) { music.clip = pendingMusic; music.loop = pendingMusicLoop; music.Play(); }
        }
        else music.volume = Mathf.MoveTowards(music.volume, musicFade, Time.unscaledDeltaTime * 2);
    }

    private void ReadInput()
    {
        held.Clear();
        Add(Keys.Up, KeyCode.UpArrow, KeyCode.W); Add(Keys.Down, KeyCode.DownArrow, KeyCode.S);
        Add(Keys.Left, KeyCode.LeftArrow, KeyCode.A); Add(Keys.Right, KeyCode.RightArrow, KeyCode.D);
        Add(Keys.Enter, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.JoystickButton0, KeyCode.JoystickButton7);
        Add(Keys.Escape, KeyCode.Escape, KeyCode.JoystickButton1, KeyCode.JoystickButton6);
        Add(Keys.Z, KeyCode.Z, KeyCode.JoystickButton3);
        Add(Keys.X, KeyCode.X, KeyCode.JoystickButton2); Add(Keys.Back, KeyCode.Backspace, KeyCode.JoystickButton2);
        Add(Keys.B, KeyCode.B, KeyCode.JoystickButton4); Add(Keys.V, KeyCode.V, KeyCode.JoystickButton5);
        Add(Keys.F11, KeyCode.F11);
        if (Input.GetJoystickNames().Any(name => !string.IsNullOrEmpty(name)))
        {
            var x = Input.GetAxisRaw("DragonGlarePadX"); var y = Input.GetAxisRaw("DragonGlarePadY");
            var dx = Input.GetAxisRaw("DragonGlareDpadX"); var dy = Input.GetAxisRaw("DragonGlareDpadY");
            if (x > 0.5 || dx > 0.5) held.Add(Keys.Right);
            if (x < -0.5 || dx < -0.5) held.Add(Keys.Left);
            if (y > 0.5 || dy > 0.5) held.Add(Keys.Up);
            if (y < -0.5 || dy < -0.5) held.Add(Keys.Down);
        }
        foreach (var key in held) if (!previousHeld.Contains(key)) pendingPressed.Add(key);
        previousHeld.Clear(); previousHeld.UnionWith(held);
    }
    private void Add(Keys key, params KeyCode[] inputs) { if (inputs.Any(Input.GetKey)) held.Add(key); }
    private void OnApplicationFocus(bool focused)
    {
        if (!focused) { previousHeld.Clear(); pendingPressed.Clear(); held.Clear(); clock = 0; }
    }

    private void OnGUI()
    {
        if (!ready || Event.current.type != EventType.Repaint) return;
        var old = GUI.matrix;
        var scale = Mathf.Min(Screen.width / 640f, Screen.height / 480f);
        GUI.color = UnityEngine.Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = UnityEngine.Color.white;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 640 * scale) / 2, (Screen.height - 480 * scale) / 2, 0), Quaternion.identity, new Vector3(scale, scale, 1));
        try
        {
            if (Session.IsSecurityBlocked)
            {
                using var g = new Drawing.Graphics();
                using var ink = new Drawing.SolidBrush(System.Drawing.Color.White);
                g.DrawString("データの整合性を確認できないため、ゲームを停止しました。", new Drawing.Font(), ink, 48, 220, Drawing.StringFormat.GenericTypographic);
            }
            else Session.Render();
            RenderedFrames++;
        }
        finally { GUI.matrix = old; }
    }

    private void SetMusic(BgmTrack? track)
    {
        var name = track switch
        {
            BgmTrack.MainMenu => "SFC_main_menu", BgmTrack.Prologue => "SFC_prologue02",
            BgmTrack.Field => "SFC_field", BgmTrack.Castle => "SFC_castle",
            BgmTrack.Battle => "SFC_battle", BgmTrack.Shop => "SFC_shop_buy", _ => null
        };
        pendingMusic = name is null ? null : Resources.Load<AudioClip>("DragonGlare/BGM/" + name);
        pendingMusicLoop = track != BgmTrack.Prologue;
        musicFade = pendingMusic is null ? 0 : 0.85f;
    }
    private void PlaySound(GameSound effect)
    {
        var name = effect switch
        {
            GameSound.Dialog => "Serif_SE", GameSound.Collision => "当たり判定SFC",
            GameSound.Attack => "SFC_atk1", GameSound.Defend => "SFC_def",
            GameSound.Magic => "SFC_magic", GameSound.Cure => "SFC_cure",
            GameSound.Poison => "SFC_poison", GameSound.Raiden => "SFC_raiden",
            GameSound.Fire => "SFC_fire", GameSound.Equip => "SFC_equip",
            GameSound.Cursor => "SFC_cursor", GameSound.Cancel => "SFC_cancel", GameSound.Escape => "SFC_escape", _ => null
        };
        var clip = name is null ? null : Resources.Load<AudioClip>("DragonGlare/SE/" + name);
        if (clip is not null) sound.PlayOneShot(clip, 0.9f);
    }
    /// <summary>
    /// Called by the Unity-side Lumis host before quitting. The existing save
    /// guard then prevents untrusted progress from overwriting a valid save.
    /// </summary>
    public void BlockSavesForSecurityViolation(string message)
    {
        if (Session == null || closed) return;
        Session.BlockForExternalSecurityViolation(message);
    }

    private void OnApplicationQuit() => Close();
    private void OnDestroy() => Close();
    private void Close()
    {
        if (!ready || closed) return;
        closed = true;
        try { Session.Shutdown(); } finally { World.Dispose(); }
    }
}
