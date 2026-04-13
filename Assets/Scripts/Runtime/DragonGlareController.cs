using System.Collections.Generic;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    public sealed partial class DragonGlareController : MonoBehaviour
    {
        private const float VirtualWidth = 640f;
        private const float VirtualHeight = 480f;
        private const float TileSize = 32f;

        private static Texture2D whiteTexture;
        private DragonGlareGameSession session;
        private AudioSource bgmSource;
        private AudioSource seSource;
        private readonly Dictionary<BgmTrack, AudioClip> bgmClips = new Dictionary<BgmTrack, AudioClip>();
        private readonly Dictionary<SoundEffect, AudioClip> seClips = new Dictionary<SoundEffect, AudioClip>();
        private readonly Dictionary<string, Texture2D> npcTextures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> portraitTextures = new Dictionary<string, Texture2D>();
        private Texture2D heroSheet;
        private BgmTrack? activeBgm;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle smallStyle;
        private GUIStyle centeredStyle;
        private GUIStyle rightStyle;
        private GUIStyle wrappedStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            DragonGlareController existing = FindObjectOfType<DragonGlareController>();
            if (existing != null)
            {
                return;
            }

            GameObject root = new GameObject("DragonGlareController");
            DontDestroyOnLoad(root);
            root.AddComponent<DragonGlareController>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            session = new DragonGlareGameSession();
            EnsureWhiteTexture();

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            seSource = gameObject.AddComponent<AudioSource>();
            seSource.loop = false;
            seSource.playOnAwake = false;

            LoadResources();
        }

        private void Update()
        {
            InputSnapshot input = BuildInputSnapshot();
            session.Tick(input);
            SyncAudio();
        }

        private void OnGUI()
        {
            EnsureStyles();

            float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
            float width = VirtualWidth * scale;
            float height = VirtualHeight * scale;
            float offsetX = (Screen.width - width) * 0.5f;
            float offsetY = (Screen.height - height) * 0.5f;

            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            DrawBackdrop(new Rect(0f, 0f, VirtualWidth, VirtualHeight));

            switch (session.State)
            {
                case GameState.ModeSelect:
                    DrawModeSelect();
                    break;
                case GameState.LanguageSelection:
                    DrawLanguageSelection();
                    break;
                case GameState.NameInput:
                    DrawNameInput();
                    break;
                case GameState.SaveSlotSelection:
                    DrawSaveSlotSelection();
                    break;
                case GameState.Field:
                    DrawField();
                    break;
                case GameState.EncounterTransition:
                    DrawField();
                    DrawTransitionOverlay();
                    break;
                case GameState.Battle:
                    DrawBattle();
                    break;
                case GameState.ShopBuy:
                    DrawShop();
                    break;
            }

            if (session.StartupFadeFrames > 0)
            {
                float alpha = Mathf.Clamp01(session.StartupFadeFrames / 18f);
                DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), new Color(0f, 0f, 0f, alpha));
            }

            GUI.matrix = previousMatrix;
        }

        private void LoadResources()
        {
            heroSheet = LoadTexture("DragonGlare/Sprites/Characters/hero_4");

            npcTextures["guide_npc.png"] = LoadTexture("DragonGlare/Sprites/NPC/guide_npc");
            npcTextures["town_child.png"] = LoadTexture("DragonGlare/Sprites/NPC/town_child");
            npcTextures["castle_guard.png"] = LoadTexture("DragonGlare/Sprites/NPC/castle_guard");
            npcTextures["field_scout.png"] = LoadTexture("DragonGlare/Sprites/NPC/field_scout");

            portraitTextures["guide-4.png"] = LoadTexture("DragonGlare/Portraits/NPC/guide-4");
            portraitTextures["young-5.png"] = LoadTexture("DragonGlare/Portraits/NPC/young-5");
            portraitTextures["castle-guard-4.png"] = LoadTexture("DragonGlare/Portraits/NPC/castle-guard-4");
            portraitTextures["mihari-3.png"] = LoadTexture("DragonGlare/Portraits/NPC/mihari-3");

            bgmClips[BgmTrack.MainMenu] = Resources.Load<AudioClip>("DragonGlare/Audio/BGM/SFC_main_menu");
            bgmClips[BgmTrack.Field] = Resources.Load<AudioClip>("DragonGlare/Audio/BGM/SFC_field");
            bgmClips[BgmTrack.Castle] = Resources.Load<AudioClip>("DragonGlare/Audio/BGM/SFC_castle");
            bgmClips[BgmTrack.Battle] = Resources.Load<AudioClip>("DragonGlare/Audio/BGM/SFC_battle");
            bgmClips[BgmTrack.Shop] = Resources.Load<AudioClip>("DragonGlare/Audio/BGM/SFC_shop_buy");

            seClips[SoundEffect.Dialog] = Resources.Load<AudioClip>("DragonGlare/Audio/SE/Serif_SE");
            seClips[SoundEffect.Collision] = Resources.Load<AudioClip>("DragonGlare/Audio/SE/collision");
        }

        private Texture2D LoadTexture(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
            }

            return texture;
        }

        private void SyncAudio()
        {
            if (!activeBgm.HasValue || activeBgm.Value != session.CurrentBgmTrack)
            {
                activeBgm = session.CurrentBgmTrack;
                AudioClip clip;
                if (bgmClips.TryGetValue(session.CurrentBgmTrack, out clip) && clip != null)
                {
                    if (bgmSource.clip != clip)
                    {
                        bgmSource.clip = clip;
                        bgmSource.Play();
                    }
                }
                else
                {
                    bgmSource.Stop();
                }
            }

            List<SoundEffect> sounds = session.DrainPendingSoundEffects();
            for (int index = 0; index < sounds.Count; index++)
            {
                AudioClip clip;
                if (seClips.TryGetValue(sounds[index], out clip) && clip != null)
                {
                    seSource.PlayOneShot(clip);
                }
            }
        }

        private InputSnapshot BuildInputSnapshot()
        {
            InputSnapshot input = new InputSnapshot();
            input.UpPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            input.DownPressed = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            input.LeftPressed = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            input.RightPressed = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
            input.UpHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            input.DownHeld = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            input.LeftHeld = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            input.RightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            input.ConfirmPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space);
            input.CancelPressed = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X);
            input.ToggleStatusPressed = Input.GetKeyDown(KeyCode.X);
            input.OpenBattlePressed = Input.GetKeyDown(KeyCode.B);
            input.OpenShopPressed = Input.GetKeyDown(KeyCode.V);
            input.BackspacePressed = Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete);
            return input;
        }

        private void EnsureWhiteTexture()
        {
            if (whiteTexture != null)
            {
                return;
            }

            whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            whiteTexture.filterMode = FilterMode.Point;
        }

        private void EnsureStyles()
        {
            if (bodyStyle != null)
            {
                return;
            }

            bodyStyle = new GUIStyle(GUI.skin.label);
            bodyStyle.fontSize = 18;
            bodyStyle.normal.textColor = Color.white;

            smallStyle = new GUIStyle(bodyStyle);
            smallStyle.fontSize = 15;

            titleStyle = new GUIStyle(bodyStyle);
            titleStyle.fontSize = 30;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            centeredStyle = new GUIStyle(bodyStyle);
            centeredStyle.alignment = TextAnchor.MiddleCenter;

            rightStyle = new GUIStyle(bodyStyle);
            rightStyle.alignment = TextAnchor.MiddleRight;

            wrappedStyle = new GUIStyle(bodyStyle);
            wrappedStyle.wordWrap = true;
        }

        private void DrawBackdrop(Rect rect)
        {
            DrawFilledRect(rect, new Color(0.02f, 0.03f, 0.08f, 1f));

            for (int index = 0; index < 20; index++)
            {
                float y = rect.y + (index * 24f);
                DrawFilledRect(new Rect(rect.x, y, rect.width, 1f), new Color(0.05f, 0.10f, 0.20f, 0.45f));
            }

            DrawFilledRect(new Rect(rect.x + 18f, rect.y + 18f, rect.width - 36f, rect.height - 36f), new Color(0f, 0.20f, 0.45f, 0.06f));
        }

        private void DrawWindow(Rect rect)
        {
            DrawFilledRect(new Rect(rect.x + 6f, rect.y + 6f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.35f));
            DrawFilledRect(rect, new Color(0.06f, 0.10f, 0.18f, 0.95f));
            DrawFrame(rect, new Color(0.10f, 0.48f, 1f, 1f), 2f);
            DrawFrame(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), new Color(0.52f, 0.85f, 1f, 1f), 1f);
        }

        private void DrawFrame(Rect rect, Color color, float thickness)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private void DrawLabel(Rect rect, string text, GUIStyle style)
        {
            GUI.Label(rect, text, style);
        }

        private void DrawCentered(Rect rect, string text)
        {
            GUI.Label(rect, text, centeredStyle);
        }

        private void DrawWrapped(Rect rect, string text)
        {
            GUI.Label(rect, text, wrappedStyle);
        }

        private void DrawSelectionRect(Rect rect)
        {
            DrawFilledRect(rect, new Color(0.15f, 0.55f, 1f, 0.22f));
            DrawFrame(rect, new Color(0.72f, 0.92f, 1f, 1f), 2f);
        }

        private void DrawModeSelect()
        {
            DrawLabel(new Rect(80f, 30f, 480f, 48f), "DRAGONGLARE ALPHA", titleStyle);
            DrawWindow(new Rect(118f, 120f, 404f, 200f));
            DrawCentered(new Rect(130f, 146f, 380f, 24f), "Unity Port");

            Rect newGameRect = new Rect(150f, 198f, 340f, 40f);
            Rect loadRect = new Rect(150f, 252f, 340f, 40f);
            if (session.ModeCursor == 0)
            {
                DrawSelectionRect(newGameRect);
            }
            else
            {
                DrawSelectionRect(loadRect);
            }

            DrawCentered(newGameRect, "NEW GAME");
            DrawCentered(loadRect, "LOAD GAME");

            DrawWindow(new Rect(118f, 352f, 404f, 76f));
            DrawCentered(new Rect(132f, 378f, 376f, 20f), "↑↓: SELECT   Z/ENTER: OK");
        }

        private void DrawLanguageSelection()
        {
            DrawLabel(new Rect(80f, 30f, 480f, 48f), "LANGUAGE", titleStyle);
            DrawWindow(new Rect(140f, 126f, 360f, 180f));
            Rect jpRect = new Rect(170f, 180f, 300f, 36f);
            Rect enRect = new Rect(170f, 228f, 300f, 36f);

            if (session.LanguageCursor == 0)
            {
                DrawSelectionRect(jpRect);
            }
            else
            {
                DrawSelectionRect(enRect);
            }

            DrawCentered(jpRect, "日本語");
            DrawCentered(enRect, "ENGLISH");

            DrawWindow(new Rect(118f, 352f, 404f, 76f));
            DrawCentered(new Rect(132f, 378f, 376f, 20f), "↑↓: SELECT   Z/ENTER: OK   ESC/X: BACK");
        }

        private void DrawNameInput()
        {
            DrawLabel(new Rect(80f, 20f, 480f, 42f), "NAME ENTRY", titleStyle);
            DrawWindow(new Rect(96f, 72f, 448f, 60f));
            DrawLabel(new Rect(118f, 92f, 388f, 24f), session.PlayerNameBuffer, bodyStyle);

            string[][] table = GameContent.GetNameTable(session.SelectedLanguage);
            float startX = 78f;
            float startY = 156f;
            float cellWidth = 48f;
            float cellHeight = 42f;

            for (int row = 0; row < table.Length; row++)
            {
                for (int column = 0; column < table[row].Length; column++)
                {
                    Rect cellRect = new Rect(startX + (column * cellWidth), startY + (row * cellHeight), cellWidth - 4f, cellHeight - 4f);
                    DrawWindow(cellRect);
                    if (session.NameCursorRow == row && session.NameCursorColumn == column)
                    {
                        DrawSelectionRect(cellRect);
                    }

                    DrawCentered(cellRect, table[row][column]);
                }
            }

            DrawWindow(new Rect(82f, 404f, 476f, 52f));
            DrawCentered(new Rect(98f, 422f, 444f, 20f), "ARROWS/WASD: MOVE   Z/ENTER: OK   ESC/X: BACK");
        }

        private void DrawSaveSlotSelection()
        {
            DrawLabel(new Rect(60f, 24f, 520f, 44f), session.SaveSlotSelectionMode == SaveSlotSelectionMode.Save ? "SAVE SLOT" : "LOAD SLOT", titleStyle);

            for (int index = 0; index < session.SaveSlotSummaries.Count; index++)
            {
                SaveSlotSummary summary = session.SaveSlotSummaries[index];
                Rect slotRect = new Rect(88f, 96f + (index * 104f), 464f, 88f);
                DrawWindow(slotRect);
                if (session.SaveSlotCursor == index)
                {
                    DrawSelectionRect(slotRect);
                }

                string title = "SLOT " + (index + 1);
                string line1 = summary.State == SaveSlotState.Occupied
                    ? summary.Name + "  Lv." + summary.Level + "  G " + summary.Gold
                    : summary.State == SaveSlotState.Corrupted ? "BROKEN DATA / セーブ破損" : "EMPTY";
                string line2 = summary.State == SaveSlotState.Occupied
                    ? "AREA " + summary.CurrentFieldMap.ToString().ToUpperInvariant() + "   " + summary.SavedAtLocal.ToString("yyyy-MM-dd HH:mm")
                    : "ENTER: SELECT";

                DrawLabel(new Rect(slotRect.x + 18f, slotRect.y + 12f, 160f, 24f), title, bodyStyle);
                DrawLabel(new Rect(slotRect.x + 18f, slotRect.y + 40f, 420f, 22f), line1, smallStyle);
                DrawLabel(new Rect(slotRect.x + 18f, slotRect.y + 62f, 420f, 20f), line2, smallStyle);
            }

            if (!string.IsNullOrEmpty(session.MenuNotice))
            {
                DrawWindow(new Rect(102f, 408f, 436f, 38f));
                DrawCentered(new Rect(118f, 418f, 404f, 20f), session.MenuNotice);
            }
        }

        private void DrawField()
        {
            DrawFieldScene();

            Rect helpRect = session.IsFieldStatusVisible ? new Rect(8f, 8f, 430f, 96f) : new Rect(8f, 8f, 624f, 96f);
            DrawWindow(helpRect);
            DrawLabel(new Rect(helpRect.x + 18f, helpRect.y + 14f, helpRect.width - 36f, 18f), session.GetFieldHelpLine1(), smallStyle);
            DrawLabel(new Rect(helpRect.x + 18f, helpRect.y + 40f, helpRect.width - 36f, 18f), session.GetFieldHelpLine2(), smallStyle);
            DrawLabel(new Rect(helpRect.x + 18f, helpRect.y + 66f, helpRect.width - 36f, 18f), session.GetFieldHelpLine3(), smallStyle);

            if (session.IsFieldStatusVisible)
            {
                DrawWindow(new Rect(446f, 8f, 186f, 116f));
                DrawLabel(new Rect(458f, 22f, 160f, 22f), session.GetDisplayPlayerName() + "  Lv." + session.Player.Level, smallStyle);
                DrawLabel(new Rect(458f, 48f, 160f, 22f), "HP " + session.Player.CurrentHp + "/" + session.Player.MaxHp, smallStyle);
                DrawLabel(new Rect(458f, 72f, 160f, 22f), "MP " + session.Player.CurrentMp + "/" + session.Player.MaxMp, smallStyle);
                DrawLabel(new Rect(458f, 96f, 160f, 22f), "G " + session.Player.Gold, smallStyle);

                DrawWindow(new Rect(446f, 132f, 186f, 148f));
                DrawLabel(new Rect(458f, 146f, 160f, 22f), "ATK " + session.GetTotalAttack() + "  DEF " + session.GetTotalDefense(), smallStyle);
                DrawLabel(new Rect(458f, 176f, 160f, 22f), "EXP " + session.GetExperienceSummary(), smallStyle);
                DrawLabel(new Rect(458f, 206f, 160f, 22f), "ぶき " + session.GetEquippedWeaponName(), smallStyle);
                DrawLabel(new Rect(458f, 234f, 160f, 22f), "ぼうぐ " + session.GetEquippedArmorName(), smallStyle);
            }

            if (session.IsFieldDialogOpen)
            {
                Rect dialogRect = new Rect(46f, 320f, 548f, 138f);
                DrawWindow(dialogRect);

                Texture2D portrait;
                if (portraitTextures.TryGetValue(session.ActiveFieldDialogPortraitAssetName, out portrait) && portrait != null)
                {
                    Rect portraitRect = new Rect(dialogRect.x + 16f, dialogRect.y + 16f, 96f, 96f);
                    DrawWindow(portraitRect);
                    DrawTextureCover(portraitRect, portrait, 6f);
                    DrawWrapped(new Rect(portraitRect.xMax + 18f, dialogRect.y + 20f, 404f, 72f), session.GetCurrentFieldDialogPage());
                    DrawLabel(new Rect(portraitRect.xMax + 18f, dialogRect.y + 98f, 404f, 20f), "Z/ENTER: NEXT   ESC/X: CLOSE", smallStyle);
                }
                else
                {
                    DrawWrapped(new Rect(dialogRect.x + 22f, dialogRect.y + 18f, 504f, 76f), session.GetCurrentFieldDialogPage());
                    DrawLabel(new Rect(dialogRect.x + 22f, dialogRect.y + 102f, 504f, 20f), "Z/ENTER: NEXT   ESC/X: CLOSE", smallStyle);
                }
            }
        }

        private void DrawFieldScene()
        {
            Rect viewport = session.IsFieldStatusVisible ? new Rect(16f, 112f, 13f * TileSize, 9f * TileSize) : new Rect(48f, 110f, 17f * TileSize, 11f * TileSize);
            int visibleWidthTiles = Mathf.RoundToInt(viewport.width / TileSize);
            int visibleHeightTiles = Mathf.RoundToInt(viewport.height / TileSize);
            int maxCameraX = Mathf.Max(0, session.Map.GetLength(1) - visibleWidthTiles);
            int maxCameraY = Mathf.Max(0, session.Map.GetLength(0) - visibleHeightTiles);
            Vector2Int cameraOrigin = new Vector2Int(
                Mathf.Clamp(session.Player.TilePosition.x - (visibleWidthTiles / 2), 0, maxCameraX),
                Mathf.Clamp(session.Player.TilePosition.y - (visibleHeightTiles / 2), 0, maxCameraY));

            DrawWindow(new Rect(viewport.x - 8f, viewport.y - 8f, viewport.width + 16f, viewport.height + 16f));
            DrawFilledRect(viewport, new Color(0.03f, 0.04f, 0.06f, 1f));

            Vector2 movementOffset = Vector2.zero;
            if (session.FieldMovementAnimationFramesRemaining > 0)
            {
                float progress = session.FieldMovementAnimationFramesRemaining / (float)session.FieldMovementAnimationFrameDuration;
                movementOffset = new Vector2(session.FieldMovementAnimationDirection.x * TileSize * progress, session.FieldMovementAnimationDirection.y * TileSize * progress);
            }

            for (int y = 0; y < visibleHeightTiles; y++)
            {
                for (int x = 0; x < visibleWidthTiles; x++)
                {
                    int worldX = cameraOrigin.x + x;
                    int worldY = cameraOrigin.y + y;
                    Rect tileRect = new Rect(viewport.x + (x * TileSize), viewport.y + (y * TileSize), TileSize, TileSize);
                    DrawFilledRect(tileRect, GetTileColor(worldX, worldY));
                }
            }

            foreach (FieldEventDefinition fieldEvent in session.GetCurrentFieldEvents())
            {
                Rect eventRect = new Rect(
                    viewport.x + ((fieldEvent.TilePosition.x - cameraOrigin.x) * TileSize) + 4f,
                    viewport.y + ((fieldEvent.TilePosition.y - cameraOrigin.y) * TileSize) + 4f,
                    TileSize - 8f,
                    TileSize - 8f);

                if (!viewport.Overlaps(eventRect))
                {
                    continue;
                }

                Texture2D sprite;
                if (!string.IsNullOrEmpty(fieldEvent.SpriteAssetName) && npcTextures.TryGetValue(fieldEvent.SpriteAssetName, out sprite) && sprite != null)
                {
                    GUI.DrawTexture(eventRect, sprite, ScaleMode.ScaleToFit, true);
                }
                else
                {
                    DrawFilledRect(eventRect, fieldEvent.DisplayColor);
                }
            }

            Rect playerRect = new Rect(
                viewport.x + ((session.Player.TilePosition.x - cameraOrigin.x) * TileSize) - movementOffset.x + 4f,
                viewport.y + ((session.Player.TilePosition.y - cameraOrigin.y) * TileSize) - movementOffset.y + 4f,
                TileSize - 8f,
                TileSize - 8f);

            if (heroSheet != null)
            {
                GUI.DrawTextureWithTexCoords(playerRect, heroSheet, GetHeroUv(session.PlayerFacingDirection), true);
            }
            else
            {
                DrawFilledRect(playerRect, Color.white);
            }
        }

        private Color GetTileColor(int worldX, int worldY)
        {
            if (worldX < 0 || worldY < 0 || worldX >= session.Map.GetLength(1) || worldY >= session.Map.GetLength(0))
            {
                return new Color(0.02f, 0.02f, 0.03f, 1f);
            }

            int tileId = session.Map[worldY, worldX];
            switch (tileId)
            {
                case MapFactory.WallTile:
                    return session.CurrentFieldMap == FieldMapId.Castle ? new Color(0.23f, 0.05f, 0.09f, 1f) : new Color(0.03f, 0.12f, 0.35f, 1f);
                case MapFactory.CastleBlockTile:
                    return new Color(0.47f, 0.11f, 0.15f, 1f);
                case MapFactory.CastleGateTile:
                    return new Color(0.45f, 0.22f, 0.12f, 1f);
                case MapFactory.FieldGateTile:
                    return new Color(0.09f, 0.22f, 0.16f, 1f);
                case MapFactory.CastleFloorTile:
                    return new Color(0.42f, 0.16f, 0.21f, 1f);
                case MapFactory.GrassTile:
                    return new Color(0.10f, 0.32f, 0.14f, 1f);
                case MapFactory.DecorationBlueTile:
                    return session.CurrentFieldMap == FieldMapId.Castle ? new Color(0.29f, 0.08f, 0.13f, 1f) : new Color(0.03f, 0.12f, 0.35f, 1f);
                default:
                    return new Color(0.10f, 0.10f, 0.12f, 1f);
            }
        }

        private Rect GetHeroUv(FacingDirection facingDirection)
        {
            switch (facingDirection)
            {
                case FacingDirection.Left:
                    return new Rect(0f, 0.5f, 0.5f, 0.5f);
                case FacingDirection.Right:
                    return new Rect(0.5f, 0.5f, 0.5f, 0.5f);
                case FacingDirection.Up:
                    return new Rect(0f, 0f, 0.5f, 0.5f);
                default:
                    return new Rect(0.5f, 0f, 0.5f, 0.5f);
            }
        }

        private void DrawTextureCover(Rect rect, Texture2D texture, float inset)
        {
            Rect inner = new Rect(rect.x + inset, rect.y + inset, rect.width - (inset * 2f), rect.height - (inset * 2f));
            GUI.DrawTexture(inner, texture, ScaleMode.ScaleAndCrop, true);
        }

        private void DrawTransitionOverlay()
        {
            float alpha = Mathf.Clamp01(session.EncounterTransitionFrames / 24f);
            DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), new Color(1f, 1f, 1f, alpha * 0.75f));
        }

        private void DrawBattle()
        {
            DrawFilledRect(new Rect(0f, 0f, VirtualWidth, 220f), new Color(0.10f, 0.10f, 0.18f, 1f));
            DrawFilledRect(new Rect(0f, 220f, VirtualWidth, 260f), new Color(0.18f, 0.05f, 0.12f, 1f));

            DrawWindow(new Rect(36f, 24f, 240f, 74f));
            DrawLabel(new Rect(52f, 40f, 200f, 24f), session.GetDisplayPlayerName() + "  Lv." + session.Player.Level, bodyStyle);
            DrawLabel(new Rect(52f, 64f, 200f, 18f), "HP " + session.Player.CurrentHp + "/" + session.Player.MaxHp + "   MP " + session.Player.CurrentMp + "/" + session.Player.MaxMp, smallStyle);

            DrawWindow(new Rect(332f, 36f, 268f, 160f));
            if (session.CurrentEncounter != null)
            {
                Color enemyColor = session.EnemyHitFlashFramesRemaining > 0 ? new Color(1f, 0.8f, 0.8f, 1f) : new Color(0.85f, 0.90f, 1f, 1f);
                DrawFilledRect(new Rect(398f, 76f, 136f, 72f), enemyColor);
                DrawFrame(new Rect(398f, 76f, 136f, 72f), new Color(0.18f, 0.32f, 0.62f, 1f), 2f);
                DrawCentered(new Rect(350f, 152f, 232f, 22f), session.CurrentEncounter.Enemy.Name);
                DrawCentered(new Rect(350f, 174f, 232f, 18f), "HP " + session.CurrentEncounter.CurrentHp + "/" + session.CurrentEncounter.Enemy.MaxHp);
            }

            DrawWindow(new Rect(22f, 264f, 284f, 182f));
            DrawLabel(new Rect(40f, 282f, 240f, 24f), session.GetBattleSelectionTitle(), bodyStyle);

            if (session.BattleFlowState == BattleFlowState.ItemSelection || session.BattleFlowState == BattleFlowState.EquipmentSelection)
            {
                IReadOnlyList<BattleSelectionEntry> entries = session.GetActiveBattleSelectionEntries();
                for (int index = 0; index < Mathf.Min(4, entries.Count - session.BattleListScroll); index++)
                {
                    BattleSelectionEntry entry = entries[session.BattleListScroll + index];
                    Rect rowRect = new Rect(34f, 318f + (index * 28f), 260f, 24f);
                    if (session.BattleListScroll + index == session.BattleListCursor)
                    {
                        DrawSelectionRect(rowRect);
                    }

                    DrawLabel(new Rect(rowRect.x + 8f, rowRect.y + 1f, 120f, 20f), entry.Label, smallStyle);
                    DrawCentered(new Rect(rowRect.x + 118f, rowRect.y + 1f, 88f, 20f), entry.Detail);
                    DrawLabel(new Rect(rowRect.x + 196f, rowRect.y + 1f, 56f, 20f), entry.Badge, rightStyle);
                }

                DrawLabel(new Rect(34f, 430f, 250f, 18f), session.GetBattleSelectionCounterText(), rightStyle);
            }
            else
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 2; column++)
                    {
                        Rect commandRect = new Rect(34f + (column * 126f), 318f + (row * 34f), 118f, 28f);
                        if (session.BattleCursorRow == row && session.BattleCursorColumn == column && session.BattleFlowState == BattleFlowState.CommandSelection)
                        {
                            DrawSelectionRect(commandRect);
                        }

                        DrawCentered(commandRect, session.GetBattleCommandLabel(row, column));
                    }
                }
            }

            DrawWindow(new Rect(322f, 264f, 292f, 182f));
            DrawWrapped(new Rect(340f, 286f, 256f, 128f), session.BattleMessage);
        }

        private void DrawShop()
        {
            DrawLabel(new Rect(110f, 20f, 420f, 40f), "SHOP", titleStyle);
            DrawWindow(new Rect(32f, 70f, 242f, 112f));
            DrawWindow(new Rect(304f, 70f, 316f, 274f));
            DrawWindow(new Rect(32f, 202f, 242f, 112f));
            DrawWindow(new Rect(70f, 356f, 498f, 96f));

            if (session.ShopPhase == ShopPhase.Welcome)
            {
                Rect buyRect = new Rect(52f, 104f, 204f, 28f);
                Rect quitRect = new Rect(52f, 142f, 204f, 28f);
                if (session.ShopPromptCursor == 0)
                {
                    DrawSelectionRect(buyRect);
                }
                else
                {
                    DrawSelectionRect(quitRect);
                }

                DrawCentered(buyRect, "こうにゅう");
                DrawCentered(quitRect, "やめる");
            }
            else
            {
                IReadOnlyList<ShopMenuEntry> entries = session.GetShopVisibleEntries();
                DrawLabel(new Rect(324f, 88f, 120f, 20f), "いちらん", smallStyle);
                for (int index = 0; index < entries.Count; index++)
                {
                    ShopMenuEntry entry = entries[index];
                    Rect rowRect = new Rect(320f, 118f + (index * 30f), 284f, 24f);
                    if (session.ShopItemCursor == index)
                    {
                        DrawSelectionRect(rowRect);
                    }

                    DrawLabel(new Rect(rowRect.x + 10f, rowRect.y + 2f, 126f, 20f), entry.Label, smallStyle);
                    if (entry.Item != null)
                    {
                        DrawCentered(new Rect(rowRect.x + 132f, rowRect.y + 2f, 40f, 20f), entry.Item.AttackBonus > 0 ? "+" + entry.Item.AttackBonus : "-");
                        DrawCentered(new Rect(rowRect.x + 174f, rowRect.y + 2f, 40f, 20f), entry.Item.DefenseBonus > 0 ? "+" + entry.Item.DefenseBonus : "-");
                        DrawCentered(new Rect(rowRect.x + 216f, rowRect.y + 2f, 42f, 20f), entry.Item.Price.ToString());
                        DrawLabel(new Rect(rowRect.x + 246f, rowRect.y + 2f, 28f, 20f), session.Player.GetItemCount(entry.Item.Id).ToString(), rightStyle);
                    }
                }
            }

            DrawLabel(new Rect(52f, 220f, 200f, 20f), "ぶき " + session.GetEquippedWeaponName(), smallStyle);
            DrawLabel(new Rect(52f, 246f, 200f, 20f), "ぼうぐ " + session.GetEquippedArmorName(), smallStyle);
            DrawLabel(new Rect(52f, 272f, 200f, 20f), "ATK " + session.GetTotalAttack() + "  DEF " + session.GetTotalDefense(), smallStyle);

            DrawWrapped(new Rect(94f, 376f, 450f, 60f), session.ShopMessage);
        }
    }
}
