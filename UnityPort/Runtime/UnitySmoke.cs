using System.Collections;
using System.Reflection;
using System.Security.Cryptography;
using DragonGlareAlpha.Data;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Persistence;
using DragonGlareAlpha.Services;
using UnityEngine;
using Session = global::DragonGlareAlpha.DragonGlareAlpha;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace DragonGlareUnity;

/// <summary>Opt-in Windows player verification; all files stay in the supplied smoke directory.</summary>
public static class UnitySmoke
{
    private static readonly Keys[] NoKeys = Array.Empty<Keys>();
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

    public static IEnumerator Run(DragonGlareBehaviour host, string output)
    {
        var result = new Results();
        var root = Path.GetFullPath(output);
        Directory.CreateDirectory(root);
        var logErrors = new List<string>();
        void ObserveLog(string message, string stack, LogType type)
        {
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
                logErrors.Add(type + ": " + message + "\n" + stack);
        }
        Application.logMessageReceived += ObserveLog;
        var steps = Steps(host, root, result);
        while (true)
        {
            bool advanced;
            object? next = null;
            try
            {
                advanced = steps.MoveNext();
                if (advanced) next = steps.Current;
            }
            catch (Exception exception)
            {
                result.failures.Add(exception.ToString());
                break;
            }
            if (!advanced) break;
            yield return next;
        }

        try { host.Session.Shutdown(); }
        catch (Exception exception) { result.failures.Add("Shutdown: " + exception); }
        Application.logMessageReceived -= ObserveLog;
        result.failures.AddRange(logErrors);
        var jsonAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(assembly => assembly.GetName().Name == "System.Text.Json");
        var report = new Report
        {
            passed = result.failures.Count == 0,
            unityVersion = Application.unityVersion,
            runtime = Environment.Version.ToString(),
            jsonAssembly = jsonAssembly?.GetName().FullName ?? "Not loaded",
            outputDirectory = root,
            checks = result.checks.ToArray(),
            screenshots = result.screenshots.ToArray(),
            screenshotSamples = result.samples.ToArray(),
            fieldDirectionSamples = result.fieldDirections.ToArray(),
            failures = result.failures.ToArray(),
            errorCount = logErrors.Count
        };
        File.WriteAllText(Path.Combine(root, "smoke-report.json"), JsonUtility.ToJson(report, true), Encoding.UTF8);
        Debug.Log("DragonGlare Unity smoke " + (report.passed ? "PASSED" : "FAILED") + ": " + root);
        Application.Quit(report.passed ? 0 : 1);
    }

    private static IEnumerator Steps(DragonGlareBehaviour host, string output, Results result)
    {
        var session = host.Session;
        var saveService = Field<SaveService>(session, "saveService");
        var allowedRoot = output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Check(Path.GetFullPath(saveService.SaveRootDirectory).StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase),
            "Session saves are isolated beneath the smoke output directory", result);
        Check(session.State == GameState.StartupOptions, "Fresh startup options", result);
        var readyDeadline = Time.realtimeSinceStartup + 10f;
        while (host.RenderedFrames < 3 || session.IsTransitioning)
        {
            if (Time.realtimeSinceStartup > readyDeadline) throw new InvalidOperationException("Player did not render the startup screen after its fade.");
            yield return new WaitForSecondsRealtime(.05f);
        }
        // Allow the resized native player surface and font atlas to present
        // after the first UI repaints, before reading the startup backbuffer.
        yield return new WaitForSecondsRealtime(1.25f);
        Capture(output, "01-startup", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "01-startup", result);

        for (var i = 0; i < 5; i++) Press(session, Keys.Down);
        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.ModeSelect, "Startup confirmation opens mode menu", result);
        Capture(output, "02-mode-menu", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "02-mode-menu", result);

        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.LanguageSelection, "New game opens prologue and language selection", result);
        Press(session, Keys.Enter);
        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.NameInput, "Prologue skip and Japanese selection open name entry", result);
        Capture(output, "03-name-entry", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "03-name-entry", result);
        Press(session, Keys.Enter);
        var names = GameContent.GetNameTable(UiLanguage.Japanese);
        for (var i = 1; i < names.Length; i++) Press(session, Keys.Down);
        for (var i = 1; i < names[^1].Length; i++) Press(session, Keys.Right);
        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.SaveSlotSelection, "Name entry END opens save slots", result);
        Check(session.Player.Name == names[0][0], "Name entry preserves selected kana", result);
        Capture(output, "04-save-slots", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "04-save-slots", result);
        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.Field, "New game saves and enters 3D field", result);
        Check(saveService.TryLoadSlot(1, out var firstSave) && firstSave!.Name == session.Player.Name,
            "Session save is readable under Unity Mono", result);
        Check(host.World.Texture.IsCreated(), "3D render texture is created", result);
        Capture(output, "05-field", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "05-field", result);

        CheckSaves(session, output, result);
        var start = session.Player.TilePosition;
        Press(session, Keys.Up);
        yield return new WaitForSecondsRealtime(.3f);
        Check(session.Player.TilePosition == new Point(start.X, start.Y - 1), "Field key moves one walkable tile", result);
        CheckFieldDirections(host, output, result);
        Press(session, Keys.X);
        Check(Field<bool>(session, "isFieldStatusVisible"), "X opens original field status HUD", result);
        Capture(output, "06-field-hud", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "06-field-hud", result);
        Press(session, Keys.X);

        // Position fixtures avoid waiting for random encounters; interactions still use original input handlers.
        session.Player.TilePosition = new Point(12, 8);
        Press(session, Keys.Enter);
        Check(Field<bool>(session, "isFieldDialogOpen"), "Adjacent guide NPC interaction opens dialog", result);
        yield return new WaitForSecondsRealtime(.3f);
        Capture(output, "07-guide-dialog", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "07-guide-dialog", result);
        Press(session, Keys.Escape);

        Press(session, Keys.V);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.ShopBuy, "V opens original shop", result);
        Press(session, Keys.Enter);
        var cash = session.Player.Gold;
        var quantity = session.Player.Inventory.Sum(item => item.Quantity);
        Press(session, Keys.Enter);
        Check(session.Player.Gold < cash && session.Player.Inventory.Sum(item => item.Quantity) == quantity + 1,
            "Shop purchase changes gold and inventory through original UI", result);
        Capture(output, "08-shop-purchase", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "08-shop-purchase", result);
        Press(session, Keys.Escape);
        Press(session, Keys.Escape);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.Field, "Shop back returns to field", result);

        var banker = FieldContent.FieldEvents.First(fieldEvent => fieldEvent.MapId == FieldMapId.Hub &&
            fieldEvent.ActionType == FieldEventActionType.Bank);
        session.Player.TilePosition = new Point(banker.TilePosition.X, banker.TilePosition.Y + 1);
        Press(session, Keys.Enter);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.Bank, "Bank NPC opens original bank", result);
        Press(session, Keys.Enter);
        var beforeDeposit = session.Player.BankGold;
        cash = session.Player.Gold;
        Press(session, Keys.Enter);
        Check(session.Player.BankGold == beforeDeposit + 10 && session.Player.Gold == cash - 10,
            "Bank 10G deposit changes cash and balance through original UI", result);
        Capture(output, "09-bank-deposit", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "09-bank-deposit", result);
        Press(session, Keys.Escape);
        Press(session, Keys.Escape);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.Field, "Bank back returns to field", result);

        session.Player.TilePosition = new Point(3, 12);
        var visualCues = new List<BattleVisualCue>();
        void ObserveCue(BattleVisualCue cue) => visualCues.Add(cue);
        session.VisualCue += ObserveCue;
        Press(session, Keys.B);
        yield return new WaitForSecondsRealtime(2.2f);
        Check(session.State == GameState.Battle && session.Encounter is not null, "Battle transition creates 3D encounter", result);
        if (Field<BattleFlowState>(session, "battleFlowState") == BattleFlowState.Intro) Press(session, Keys.Enter);
        Check(Field<BattleFlowState>(session, "battleFlowState") == BattleFlowState.CommandSelection,
            "Battle intro opens original command grid", result);
        Capture(output, "10-battle", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "10-battle", result);
        var enemyHp = session.Encounter!.CurrentHp;
        Press(session, Keys.Enter);
        Check(session.Encounter.CurrentHp < enemyHp, "Attack command applies battle damage", result);
        yield return new WaitForSecondsRealtime(3f);
        Check(visualCues.Count > 0, "Battle resolution dispatches 3D visual cues", result);
        session.VisualCue -= ObserveCue;
        host.World.PlayCue(BattleVisualCue.SpellCast);
        host.World.PlayCue(BattleVisualCue.PlayerHeal);
        host.World.PlayCue(BattleVisualCue.PlayerGuard);
        yield return new WaitForSecondsRealtime(.12f);
        Capture(output, "11-battle-effects", result);
        yield return new WaitForSecondsRealtime(.45f);
        VerifyCapture(output, "11-battle-effects", result);
        foreach (BattleVisualCue cue in Enum.GetValues(typeof(BattleVisualCue))) host.World.PlayCue(cue);
        yield return new WaitForSecondsRealtime(.25f);
        Check(true, "Every BattleVisualCue executes under Unity player", result);

        Invoke(session, "ChangeGameState", GameState.Field);
        yield return new WaitForSecondsRealtime(.9f);
        Check(session.State == GameState.Field, "Battle fixture exits to field", result);
        foreach (var mapId in new[] { FieldMapId.Castle, FieldMapId.Field, FieldMapId.Dungeon })
        {
            Invoke(session, "SetFieldMap", mapId);
            session.Player.TilePosition = mapId == FieldMapId.Field ? new Point(2, 6) : new Point(14, 20);
            yield return new WaitForSecondsRealtime(.65f);
            Check(session.MapId == mapId && host.World.Texture.IsCreated(), "3D map fixture renders " + mapId, result);
            var screenshot = "12-map-" + mapId.ToString().ToLowerInvariant();
            Capture(output, screenshot, result);
            yield return new WaitForSecondsRealtime(.45f);
            VerifyCapture(output, screenshot, result);
        }
        Invoke(session, "SetFieldMap", FieldMapId.Hub);
        session.Player.TilePosition = new Point(3, 12);
        Check(true, "Gameplay smoke completed", result);
    }

    private static void CheckSaves(Session session, string output, Results result)
    {
        var fixture = new SaveService(Path.Combine(output, "core-save-fixture"));
        var saved = SaveDataMapper.Create(session.Player, UiLanguage.Japanese, session.MapId, 1);
        saved.Name = "竜<&>\"\\😀";
        saved.SavedAtUtc = new DateTime(2026, 10, 14, 1, 2, 3, DateTimeKind.Utc).AddTicks(1234567);
        saved.BankGold = 801;
        saved.LoanBalance = 209;
        saved.CompletedFieldEventIds.Add("field_treasure_chest");
        fixture.SaveSlot(1, saved);
        Check(fixture.TryLoadSlot(1, out var loaded) && loaded!.Name == saved.Name &&
            loaded.SavedAtUtc == saved.SavedAtUtc && loaded.BankGold == 801 && loaded.LoanBalance == 209 &&
            loaded.CompletedFieldEventIds.Contains("field_treasure_chest") && loaded.Signature == saved.Signature,
            "Unity Mono JSON Unicode, precise timestamp, bank and event save roundtrip", result);
        var restored = SaveDataMapper.Restore(loaded!, new Point(3, 12));
        Check(restored.Player.Inventory.Sum(item => item.Quantity) == session.Player.Inventory.Sum(item => item.Quantity),
            "SaveDataMapper restores original inventory under Unity Mono", result);
        saved.Gold = 909;
        fixture.SaveSlot(1, saved);
        Check(fixture.TryLoadSlot(1, out loaded) && loaded!.Gold == 909 && !File.Exists(fixture.GetSlotPath(1) + ".tmp"),
            "Unity Mono atomically overwrites existing save", result);
        Check(fixture.CopySlot(1, 2) && fixture.TryLoadSlot(2, out var copied) && copied!.SlotNumber == 2 &&
            copied.Gold == 909 && fixture.GetSlotSummaries()[1].State == SaveSlotState.Occupied,
            "Unity Mono copy re-signs and re-encrypts destination slot", result);

        var protection = typeof(SaveService).Assembly.GetType("DragonGlare.UnityPort.WindowsDataProtection", throwOnError: true)!;
        var decrypt = protection.GetMethod("Unprotect", BindingFlags.Static | BindingFlags.NonPublic)!;
        var encrypt = protection.GetMethod("Protect", BindingFlags.Static | BindingFlags.NonPublic)!;
        byte[] entropy;
        using (var sha = SHA256.Create())
            entropy = sha.ComputeHash(Encoding.UTF8.GetBytes("DragonGlareAlpha::Dpapi::2026-04-09::1"));
        var plaintext = (byte[])decrypt.Invoke(null, new object[] { File.ReadAllBytes(fixture.GetSlotPath(1)), entropy, 0u })!;
        var json = Encoding.UTF8.GetString(plaintext);
        var changed = json.Replace("\"Gold\": 909", "\"Gold\": 910");
        Check(changed != json, "Tamper fixture modifies the decrypted gold field", result);
        var ciphertext = (byte[])encrypt.Invoke(null, new object[] { Encoding.UTF8.GetBytes(changed), entropy, 0u })!;
        File.WriteAllBytes(fixture.GetSlotPath(1), ciphertext);
        Check(!fixture.TryLoadSlot(1, out _) && fixture.LastFailureReason == SaveLoadFailureReason.InvalidSignature,
            "Unity Mono rejects changed payload with valid DPAPI encryption", result);
        Check(fixture.DeleteSlot(2) && !fixture.TryLoadSlot(2, out _) && fixture.LastFailureReason == SaveLoadFailureReason.NotFound,
            "Unity Mono deletes a slot and reports missing data", result);
    }

    private static void CheckFieldDirections(DragonGlareBehaviour host, string output, Results result)
    {
        var session = host.Session;
        var originalMap = session.MapId;
        var originalTile = session.Player.TilePosition;
        var originalEncounterSteps = Field<int>(session, "fieldEncounterStepsRemaining");
        var originalFacing = Field<object>(session, "playerFacingDirection");
        var camera = Field<Camera>(host.World, "camera");
        var inputs = new[]
        {
            (Keys.Right, new Point(1, 0)), (Keys.Left, new Point(-1, 0)),
            (Keys.Up, new Point(0, -1)), (Keys.Down, new Point(0, 1)),
            (Keys.D, new Point(1, 0)), (Keys.A, new Point(-1, 0)),
            (Keys.W, new Point(0, -1)), (Keys.S, new Point(0, 1))
        };
        try
        {
            foreach (var mapId in new[] { FieldMapId.Hub, FieldMapId.Castle, FieldMapId.Field, FieldMapId.Dungeon })
            {
                Invoke(session, "SetFieldMap", mapId);
                SetField(session, "fieldEncounterStepsRemaining", 10000);
                var origin = FindMovementFixture(session);
                session.Player.TilePosition = origin;
                SettleFieldMovement(session);
                host.World.Update(session, 1f);
                foreach (var (key, tileDelta) in inputs)
                {
                    session.Player.TilePosition = origin;
                    SettleFieldMovement(session);
                    host.World.Update(session, 0f);
                    var before = session.WorldPlayerPosition;
                    // Both endpoints use this same native camera. Camera follow
                    // would otherwise hide how the actor actually moved on screen.
                    var projectedBefore = camera.WorldToViewportPoint(before);
                    var cameraPosition = camera.transform.position;
                    var cameraRotation = camera.transform.rotation;
                    if (mapId == FieldMapId.Hub && key == Keys.Right)
                        CaptureFieldFrame(camera, host.World.Texture, output, "14-field-right-before", result);
                    Press(session, key);
                    SettleFieldMovement(session);
                    host.World.Update(session, 0f);
                    var after = session.WorldPlayerPosition;
                    var projectedAfter = camera.WorldToViewportPoint(after);
                    var hero = Field<Transform>(host.World, "fieldHero");
                    var worldDelta = after - before;
                    var screenDelta = projectedAfter - projectedBefore;
                    var facingDot = Vector3.Dot(hero.forward, worldDelta.normalized);
                    if (mapId == FieldMapId.Hub && key == Keys.Right)
                        CaptureFieldFrame(camera, host.World.Texture, output, "14-field-right-after", result);
                    var label = mapId + " " + key;
                    result.fieldDirections.Add(new FieldDirectionSample
                    {
                        map = mapId.ToString(), key = key.ToString(),
                        tileDeltaX = session.Player.TilePosition.X - origin.X,
                        tileDeltaY = session.Player.TilePosition.Y - origin.Y,
                        worldDeltaX = worldDelta.x, worldDeltaZ = worldDelta.z,
                        viewportDeltaX = screenDelta.x, viewportDeltaY = screenDelta.y,
                        facingDot = facingDot
                    });
                    Check(session.State == GameState.Field && session.MapId == mapId &&
                        session.Player.TilePosition == new Point(origin.X + tileDelta.X, origin.Y + tileDelta.Y),
                        "Field input preserves original logical tile direction: " + label, result);
                    Check(Vector3.Distance(cameraPosition, camera.transform.position) < .00001f &&
                        Quaternion.Angle(cameraRotation, camera.transform.rotation) < .001f,
                        "Field projection uses the same camera for both movement endpoints: " + label, result);
                    Check(projectedBefore.z > 0 && projectedAfter.z > 0 &&
                        (tileDelta.X != 0 ? screenDelta.x * tileDelta.X > .0001f :
                            screenDelta.y * -tileDelta.Y > .0001f),
                        "Field input moves the actor in the expected screen direction: " + label, result);
                    Check(Vector3.Distance(hero.position, after) < .00001f && facingDot > .999f,
                        "Field actor position and facing agree with actual movement: " + label, result);
                }
            }
        }
        finally
        {
            Invoke(session, "SetFieldMap", originalMap);
            session.Player.TilePosition = originalTile;
            SetField(session, "fieldEncounterStepsRemaining", originalEncounterSteps);
            SetField(session, "playerFacingDirection", originalFacing);
            SettleFieldMovement(session);
            host.World.Update(session, 1f);
        }
    }

    private static Point FindMovementFixture(Session session)
    {
        var map = session.Map;
        var middle = new Point(map.GetLength(1) / 2, map.GetLength(0) / 2);
        Point? best = null;
        var bestDistance = int.MaxValue;
        var cross = new[] { Point.Empty, new Point(1, 0), new Point(-1, 0), new Point(0, -1), new Point(0, 1) };
        for (var y = 1; y < map.GetLength(0) - 1; y++)
        for (var x = 1; x < map.GetLength(1) - 1; x++)
        {
            var candidate = new Point(x, y);
            if (cross.Any(delta =>
            {
                var tile = new Point(x + delta.X, y + delta.Y);
                var tileId = map[tile.Y, tile.X];
                return !MapFactory.IsWalkableTileId(tileId) ||
                    tileId is MapFactory.CastleGateTile or MapFactory.FieldGateTile or MapFactory.CastleTextExitTile ||
                    FieldContent.FieldEvents.Any(definition => definition.MapId == session.MapId &&
                        definition.BlocksMovement && definition.TilePosition == tile) ||
                    FieldContent.FieldTransitions.Any(definition => definition.FromMapId == session.MapId &&
                        definition.IsTriggeredBy(tile));
            })) continue;
            var distance = (x - middle.X) * (x - middle.X) + (y - middle.Y) * (y - middle.Y);
            if (distance >= bestDistance) continue;
            best = candidate;
            bestDistance = distance;
        }
        return best ?? throw new InvalidOperationException("No unobstructed movement fixture for " + session.MapId);
    }

    private static void SettleFieldMovement(Session session)
    {
        for (var tick = 0; tick < 12; tick++) session.Tick(NoKeys, NoKeys);
    }

    private static void CaptureFieldFrame(Camera camera, RenderTexture world, string output, string name, Results result)
    {
        // Render and read the actual 3D field immediately, before the host's
        // next Update can follow the player and change the shared camera.
        camera.Render();
        var previous = RenderTexture.active;
        var resolved = RenderTexture.GetTemporary(world.width, world.height, 0, RenderTextureFormat.ARGB32);
        var image = new Texture2D(world.width, world.height, TextureFormat.RGB24, false);
        try
        {
            UnityEngine.Graphics.Blit(world, resolved);
            RenderTexture.active = resolved;
            image.ReadPixels(new Rect(0, 0, world.width, world.height), 0, 0);
            image.Apply();
            var filename = name + ".png";
            File.WriteAllBytes(Path.Combine(output, filename), image.EncodeToPNG());
            result.screenshots.Add(filename);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(resolved);
            UnityEngine.Object.Destroy(image);
        }
        VerifyCapture(output, name, result);
    }

    private static void Press(Session session, Keys key)
    {
        var keys = new[] { key };
        session.Tick(keys, keys);
        session.Tick(NoKeys, NoKeys);
    }

    private static T Field<T>(object target, string name)
        => (T)(target.GetType().GetField(name, PrivateInstance)?.GetValue(target)
            ?? throw new MissingFieldException(target.GetType().FullName, name));

    private static void SetField(object target, string name, object value)
    {
        var field = target.GetType().GetField(name, PrivateInstance)
            ?? throw new MissingFieldException(target.GetType().FullName, name);
        field.SetValue(target, value);
    }

    private static void Invoke(Session session, string name, params object[] arguments)
    {
        var method = typeof(Session).GetMethod(name, PrivateInstance)
            ?? throw new MissingMethodException(typeof(Session).FullName, name);
        method.Invoke(session, arguments);
    }

    private static void Check(bool passed, string name, Results result)
    {
        if (!passed) throw new InvalidOperationException("Smoke check failed: " + name);
        result.checks.Add(name);
    }

    private static void Capture(string output, string name, Results result)
    {
        var filename = name + ".png";
        ScreenCapture.CaptureScreenshot(Path.Combine(output, filename));
        result.screenshots.Add(filename);
    }

    private static void VerifyCapture(string output, string name, Results result)
    {
        var path = Path.Combine(output, name + ".png");
        Check(File.Exists(path) && new FileInfo(path).Length > 1024, "Screenshot written: " + name, result);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            Check(texture.LoadImage(File.ReadAllBytes(path)), "Screenshot PNG decodes: " + name, result);
            var pixels = texture.GetPixels32();
            var colors = new HashSet<int>();
            var visible = 0;
            var colored = 0;
            var darkest = 255;
            var brightest = 0;
            double brightnessSum = 0, squaredBrightnessSum = 0;
            foreach (var pixel in pixels)
            {
                var brightness = (299 * pixel.r + 587 * pixel.g + 114 * pixel.b) / 1000;
                darkest = Math.Min(darkest, brightness);
                brightest = Math.Max(brightest, brightness);
                brightnessSum += brightness;
                squaredBrightnessSum += brightness * brightness;
                if (brightness >= 24) visible++;
                var maximum = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
                var minimum = Math.Min(pixel.r, Math.Min(pixel.g, pixel.b));
                if (maximum - minimum >= 24 && maximum >= 40) colored++;
                colors.Add(((pixel.r >> 4) << 8) | ((pixel.g >> 4) << 4) | (pixel.b >> 4));
            }
            var average = pixels.Length == 0 ? 0 : brightnessSum / pixels.Length;
            var variance = pixels.Length == 0 ? 0 : squaredBrightnessSum / pixels.Length - average * average;
            var sample = new ScreenshotSample
            {
                filename = name + ".png", width = texture.width, height = texture.height,
                pixelCount = pixels.Length, visiblePixels = visible, coloredPixels = colored,
                quantizedColors = colors.Count, darkest = darkest, brightest = brightest,
                averageBrightness = average, brightnessVariance = variance
            };
            result.samples.Add(sample);
            Check(pixels.Length > 0 && visible >= Math.Max(128, pixels.Length / 100) &&
                brightest - darkest >= 32 && colors.Count >= 2 && variance >= 25,
                "Screenshot contains visible, varied image content: " + name +
                " (visible=" + visible + ", colors=" + colors.Count + ", variance=" + variance.ToString("F2") + ")", result);
            if (name.StartsWith("05-") || name.StartsWith("06-") || name.StartsWith("07-") ||
                name.StartsWith("10-") || name.StartsWith("11-") || name.StartsWith("12-") || name.StartsWith("14-"))
                Check(colors.Count >= 8 && colored >= 128, "3D screenshot contains color variation: " + name, result);
        }
        finally { UnityEngine.Object.Destroy(texture); }
    }

    private sealed class Results
    {
        internal readonly List<string> checks = new();
        internal readonly List<string> screenshots = new();
        internal readonly List<ScreenshotSample> samples = new();
        internal readonly List<FieldDirectionSample> fieldDirections = new();
        internal readonly List<string> failures = new();
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string unityVersion = string.Empty;
        public string runtime = string.Empty;
        public string jsonAssembly = string.Empty;
        public string outputDirectory = string.Empty;
        public string[] checks = Array.Empty<string>();
        public string[] screenshots = Array.Empty<string>();
        public ScreenshotSample[] screenshotSamples = Array.Empty<ScreenshotSample>();
        public FieldDirectionSample[] fieldDirectionSamples = Array.Empty<FieldDirectionSample>();
        public string[] failures = Array.Empty<string>();
        public int errorCount;
    }

    [Serializable]
    private sealed class FieldDirectionSample
    {
        public string map = string.Empty, key = string.Empty;
        public int tileDeltaX, tileDeltaY;
        public float worldDeltaX, worldDeltaZ, viewportDeltaX, viewportDeltaY, facingDot;
    }

    [Serializable]
    private sealed class ScreenshotSample
    {
        public string filename = string.Empty;
        public int width, height, pixelCount, visiblePixels, coloredPixels, quantizedColors, darkest, brightest;
        public double averageBrightness, brightnessVariance;
    }
}
