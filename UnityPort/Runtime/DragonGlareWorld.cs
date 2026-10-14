using System;
using System.Collections.Generic;
using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Services;
using UnityEngine;
using UnityEngine.Rendering;
using Color = UnityEngine.Color;
using Session = global::DragonGlareAlpha.DragonGlareAlpha;
using Object = UnityEngine.Object;

namespace DragonGlareUnity;

/// <summary>Presentation only. The shared RPG session owns movement, encounters and rewards.</summary>
public sealed class DragonGlareWorld : IDisposable
{
    private const int WorldLayer = 29;
    private readonly GameObject root;
    private readonly Camera camera;
    private readonly Light sun;
    private readonly Dictionary<string, Material> materials = new();
    private readonly List<Mesh> mapMeshes = new();
    private readonly List<Effect> effects = new();
    private readonly List<Transform> portalRings = new();
    private readonly List<Chest> chests = new();
    private readonly List<Transform> npcHeads = new();
    private readonly MaterialPropertyBlock flashProperties = new();
    private readonly Texture2D particleTexture;
    private readonly Mesh ringMesh;
    private GameObject? fieldRoot;
    private GameObject? battleRoot;
    private Transform? fieldHero;
    private Transform? battleHero;
    private Transform? enemy;
    private Renderer[] enemyRenderers = Array.Empty<Renderer>();
    private Renderer[] heroRenderers = Array.Empty<Renderer>();
    private FieldMapId? builtMap;
    private BattleEncounter? builtEncounter;
    private float elapsed;
    private float enemyFlash;
    private float heroFlash;
    private float enemyLunge;
    private float heroLunge;
    private float defeatRemaining;
    private bool defeatPlayed;
    private float battleLookHeight = 1.18f;
    private Vector3 enemyBaseScale = Vector3.one;
    private bool disposed;
    private Vector3 cameraTarget;
    private Vector3 cameraPosition;

    public DragonGlareWorld(Transform parent)
    {
        root = Child("DragonGlare 3D World", parent);
        var cameraObject = Child("RPG camera", root.transform);
        camera = cameraObject.AddComponent<Camera>();
        Texture = new RenderTexture(640, 480, 24, RenderTextureFormat.ARGB32)
        {
            name = "DragonGlare 3D View",
            antiAliasing = 2,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        Texture.Create();
        camera.targetTexture = Texture;
        camera.cullingMask = 1 << WorldLayer;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.07f, .11f, .18f);
        camera.nearClipPlane = .1f;
        camera.farClipPlane = 90f;
        camera.fieldOfView = 48f;
        camera.allowHDR = false;
        camera.allowMSAA = true;
        camera.depth = -20;

        sun = Child("Warm directional light", root.transform).AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, .89f, .71f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .68f;
        sun.shadowBias = .04f;
        sun.cullingMask = 1 << WorldLayer;
        sun.transform.rotation = Quaternion.Euler(48, -36, 0);
        var fill = Child("Cool directional fill", root.transform).AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(.46f, .64f, 1f);
        fill.intensity = .38f;
        fill.cullingMask = 1 << WorldLayer;
        fill.transform.rotation = Quaternion.Euler(30, 150, 0);

        particleTexture = MakeParticleTexture();
        ringMesh = MakeRingMesh();
    }

    public RenderTexture Texture { get; }

    public void Update(Session session, float dt)
    {
        if (disposed) return;
        dt = Mathf.Max(0, dt);
        elapsed += dt;
        if (builtMap != session.MapId) RebuildField(session);
        var inBattle = session.State == GameState.Battle;
        fieldRoot?.SetActive(!inBattle);
        battleRoot?.SetActive(inBattle);
        if (inBattle && session.Encounter != null && !ReferenceEquals(builtEncounter, session.Encounter))
            RebuildBattle(session);

        if (inBattle)
        {
            UpdateBattle(dt);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 38f, 1f - Mathf.Exp(-dt * 9f));
            // Frame each creature's actual silhouette at the original enemy
            // center (about virtual Y=266), between the retained UI windows.
            cameraTarget = new Vector3(0, battleLookHeight, 0);
            cameraPosition = cameraTarget + new Vector3(0, 2.12f, 7.9f);
        }
        else
        {
            if (fieldHero != null)
            {
                fieldHero.position = session.WorldPlayerPosition;
                fieldHero.rotation = Quaternion.Euler(0, session.FacingDegrees, 0);
                var torso = fieldHero.Find("Body");
                if (torso != null) torso.localPosition = new Vector3(0, .55f + Mathf.Sin(elapsed * 4f) * .015f, 0);
            }
            var destination = session.WorldPlayerPosition;
            // Keep the camera within the map but never alter the session's tile position.
            destination.x = Mathf.Clamp(destination.x, 4f, Mathf.Max(4f, session.Map.GetLength(1) - 5f));
            destination.z = -Mathf.Clamp(-destination.z, 4f, Mathf.Max(4f, session.Map.GetLength(0) - 5f));
            var follow = 1f - Mathf.Exp(-dt * 12f);
            cameraTarget = Vector3.Lerp(cameraTarget, destination + Vector3.up * .15f, follow);
            cameraPosition = cameraTarget + new Vector3(5.8f, 10.4f, -9.5f);
            camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, 48f, 1f - Mathf.Exp(-dt * 9f));
            if (session.State == GameState.EncounterTransition)
                cameraPosition += new Vector3(Mathf.Sin(elapsed * 91f), Mathf.Cos(elapsed * 77f), 0) * .10f;
            foreach (var chest in chests)
            {
                var opened = session.Player.HasCompletedFieldEvent(chest.Id);
                chest.Lid.localRotation = Quaternion.Euler(opened ? -65f : 0, 0, 0);
                chest.Glow.SetActive(!opened);
            }
            foreach (var head in npcHeads)
                if (head != null) head.localRotation = Quaternion.Euler(0, Mathf.Sin(elapsed * .7f + head.position.x) * 8f, 0);
        }

        foreach (var ring in portalRings)
            if (ring != null) ring.localRotation = Quaternion.Euler(0, elapsed * 32f, 0);
        camera.transform.position = cameraPosition;
        camera.transform.LookAt(cameraTarget);
        UpdateEffects(dt);
    }

    public void PlayCue(BattleVisualCue cue)
    {
        if (disposed) return;
        var enemyPosition = enemy != null ? enemy.position + Vector3.up * .8f : new Vector3(0, .8f, 0);
        var heroPosition = battleHero != null ? battleHero.position + Vector3.up * .8f : new Vector3(-1.9f, .8f, 2.4f);
        switch (cue)
        {
            case BattleVisualCue.PlayerAction:
                heroLunge = .34f;
                Slash(enemyPosition, new Color(.58f, .85f, 1f));
                break;
            case BattleVisualCue.EnemyAction:
                enemyLunge = .38f;
                Slash(heroPosition, new Color(1f, .34f, .25f));
                break;
            case BattleVisualCue.EnemyHit:
                enemyFlash = .25f;
                Burst(enemyPosition, new Color(1f, .79f, .22f), 28, .8f);
                break;
            case BattleVisualCue.PlayerHit:
                heroFlash = .25f;
                Burst(heroPosition, new Color(1f, .29f, .18f), 28, .7f);
                break;
            case BattleVisualCue.SpellCast:
                Burst(enemyPosition, new Color(.28f, .68f, 1f), 72, 1.6f);
                Ring(enemyPosition - Vector3.up * .62f, new Color(.33f, .7f, 1f), 1.35f);
                AddPointLight(enemyPosition, new Color(.32f, .62f, 1f), 3.2f, .6f);
                break;
            case BattleVisualCue.PlayerGuard:
                Ring(heroPosition - Vector3.up * .65f, new Color(.35f, .92f, 1f), 1.0f);
                Burst(heroPosition, new Color(.59f, .94f, 1f), 20, .4f);
                break;
            case BattleVisualCue.PlayerHeal:
            case BattleVisualCue.MpRecover:
                Burst(heroPosition, new Color(.31f, 1f, .60f), 48, .8f, true);
                Ring(heroPosition - Vector3.up * .65f, new Color(.35f, 1f, .70f), 1.05f);
                AddPointLight(heroPosition, new Color(.32f, 1f, .52f), 2.8f, .7f);
                break;
            case BattleVisualCue.EnemyDefeat:
                defeatPlayed = true;
                defeatRemaining = .55f;
                Burst(enemyPosition, new Color(.87f, .67f, 1f), 70, 1.8f, true);
                Ring(enemyPosition - Vector3.up * .7f, new Color(.8f, .56f, 1f), 1.5f);
                break;
            case BattleVisualCue.ItemUse:
                Burst(heroPosition, new Color(1f, .89f, .36f), 34, .7f, true);
                break;
            case BattleVisualCue.EnemyStatus:
                Burst(enemyPosition, new Color(.6f, .36f, .97f), 34, .4f, true);
                break;
            case BattleVisualCue.PlayerStatus:
            case BattleVisualCue.PoisonTick:
                Burst(heroPosition, new Color(.48f, .92f, .26f), 26, .4f, true);
                break;
        }
    }

    private void RebuildField(Session session)
    {
        ClearEffects();
        Destroy(fieldRoot);
        foreach (var mesh in mapMeshes) Destroy(mesh);
        mapMeshes.Clear();
        portalRings.Clear();
        npcHeads.Clear();
        chests.Clear();
        builtMap = session.MapId;
        fieldRoot = Child(session.MapId + " landscape", root.transform);
        var castle = session.MapId == FieldMapId.Castle;
        var dungeon = session.MapId == FieldMapId.Dungeon;
        var indoors = castle || dungeon;
        camera.backgroundColor = indoors ? new Color(.028f, .024f, .052f) : new Color(.085f, .16f, .22f);
        sun.color = dungeon ? new Color(.66f, .65f, .97f) : new Color(1f, .89f, .71f);
        sun.intensity = dungeon ? .52f : 1.15f;

        var stone = new MeshBuilder();
        var paving = new MeshBuilder();
        var walls = new MeshBuilder();
        var water = new MeshBuilder();
        var grass = new MeshBuilder();
        var wood = new MeshBuilder();
        var leaves = new MeshBuilder();
        var gold = new MeshBuilder();
        var carpet = new MeshBuilder();
        var width = session.Map.GetLength(1);
        var height = session.Map.GetLength(0);
        var cutaway = GetInteriorCutawayMidpoint(session.Map);
        stone.Box(new Vector3((width - 1) * .5f, -.35f, -(height - 1) * .5f), new Vector3(width + .5f, .6f, height + .5f));
        for (var z = 0; z < height; z++)
        for (var x = 0; x < width; x++)
        {
            var tile = session.Map[z, x];
            // Logical map rows increase downward; Unity's +Z points upward
            // on screen with the field camera looking from the -Z side.
            var at = new Vector3(x, 0, -z);
            var blocked = !MapFactory.IsWalkableTileId(tile);
            var hash = (x * 733 + z * 199) % 17;
            if (!indoors && (tile == MapFactory.WallTile || tile == MapFactory.DecorationBlueTile))
            {
                water.Box(at + Vector3.up * -.06f, new Vector3(.99f, .08f, .99f));
                if ((x == 0 || z == 0 || x == width - 1 || z == height - 1) && hash % 3 == 0)
                    stone.Box(at + new Vector3(.17f, .13f, -.14f), new Vector3(.44f, .4f + hash * .015f, .45f));
                continue;
            }
            if (indoors && blocked)
            {
                if (tile == MapFactory.CastleTextPillarTile || tile == MapFactory.CastleTextOrnamentTile)
                {
                    walls.Cylinder(at + Vector3.up * .85f, .17f, 1.7f, 8);
                    gold.Box(at + Vector3.up * 1.7f, new Vector3(.45f, .16f, .45f));
                    paving.Box(at + Vector3.up * .09f, new Vector3(.48f, .18f, .48f));
                }
                else
                {
                    // The original castle has a one-tile entrance corridor.
                    // Cut away foreground masonry so the oblique camera can
                    // show the whole hero there. This is static presentation
                    // geometry; original tile collision remains authoritative.
                    var wallHeight = z >= cutaway.y || x >= cutaway.x ? .32f :
                        tile == MapFactory.CastleTextColumnBaseTile ? .75f : 1.65f;
                    walls.Box(at + Vector3.up * (wallHeight * .5f), new Vector3(.98f, wallHeight, .98f));
                    paving.Box(at + Vector3.up * (wallHeight + .06f), new Vector3(1f, .12f, 1f));
                    if (hash == 1 && HasWalkableNeighbor(session.Map, x, z))
                        AddTorch(at + new Vector3(0, Mathf.Min(1.18f, wallHeight + .10f), -.53f), fieldRoot.transform, dungeon);
                }
                continue;
            }
            (tile == MapFactory.CastleTextCarpetTile ? carpet : paving).Box(at + Vector3.up * -.01f, new Vector3(.98f, .1f, .98f));
            if (tile == MapFactory.GrassTile)
            {
                for (var n = 0; n < 4; n++)
                {
                    var px = ((hash * 3 + n * 7) % 13) / 13f - .5f;
                    var pz = ((hash * 5 + n * 3) % 11) / 11f - .5f;
                    grass.Cone(at + new Vector3(px, .13f, pz), .08f, .24f, 4);
                }
                if (hash == 0 || hash == 7)
                {
                    var corner = at + new Vector3(.38f, 0, -.38f);
                    wood.Cylinder(corner + Vector3.up * .38f, .07f, .76f, 5);
                    leaves.Cone(corner + Vector3.up * .95f, .43f, 1f, 6);
                    leaves.Cone(corner + Vector3.up * 1.32f, .3f, .7f, 6);
                }
            }
            if (tile == MapFactory.CastleGateTile || tile == MapFactory.FieldGateTile || tile == MapFactory.CastleTextExitTile)
                AddPortal(at, fieldRoot.transform, tile == MapFactory.CastleGateTile || tile == MapFactory.CastleTextExitTile);
        }
        BuildBatch(stone, "Terrain foundation", indoors ? new Color(.19f, .17f, .24f) : new Color(.22f, .31f, .25f));
        BuildBatch(paving, "Stone paths", indoors ? new Color(.48f, .40f, .53f) : new Color(.49f, .53f, .40f));
        BuildBatch(walls, "Castle walls", dungeon ? new Color(.16f, .16f, .25f) : new Color(.36f, .34f, .43f));
        BuildBatch(water, "Water", new Color(.11f, .43f, .62f), .12f);
        BuildBatch(grass, "Tall grass", new Color(.31f, .58f, .24f));
        BuildBatch(wood, "Tree trunks", new Color(.36f, .22f, .14f));
        BuildBatch(leaves, "Tree canopies", new Color(.13f, .36f, .24f));
        BuildBatch(gold, "Column capitals", new Color(.80f, .62f, .22f));
        BuildBatch(carpet, "Castle carpet", dungeon ? new Color(.24f, .13f, .34f) : new Color(.59f, .14f, .19f));

        foreach (var definition in session.VisibleEvents) AddFieldEvent(definition, fieldRoot.transform);
        fieldHero = MakeHero(fieldRoot.transform, "Hero", new Color(.16f, .43f, .72f));
        cameraTarget = session.WorldPlayerPosition + Vector3.up * .15f;
    }

    private void AddFieldEvent(FieldEventDefinition definition, Transform parent)
    {
        var at = new Vector3(definition.TilePosition.X, 0, -definition.TilePosition.Y);
        if (definition.ActionType == FieldEventActionType.Treasure)
        {
            var chest = Child("Treasure " + definition.Id, parent).transform;
            chest.position = at;
            chest.rotation = Quaternion.Euler(0, 180f, 0);
            Shape(PrimitiveType.Cube, "Chest", chest, new Vector3(0, .23f, 0), new Vector3(.58f, .40f, .42f), "Chest wood", new Color(.41f, .20f, .10f));
            var hinge = Child("Chest lid hinge", chest).transform;
            hinge.localPosition = new Vector3(0, .43f, -.21f);
            Shape(PrimitiveType.Cube, "Lid", hinge, new Vector3(0, .065f, .21f), new Vector3(.61f, .15f, .46f), "Chest trim", new Color(.84f, .65f, .20f));
            Shape(PrimitiveType.Cube, "Lock", chest, new Vector3(0, .35f, .225f), new Vector3(.1f, .14f, .04f), "Gold", new Color(.96f, .78f, .28f));
            var glow = Shape(PrimitiveType.Sphere, "Treasure glimmer", chest, new Vector3(0, .60f, 0), Vector3.one * .1f, "Treasure glow", new Color(1f, .83f, .25f), 1.2f);
            chests.Add(new Chest(definition.Id, hinge, glow.gameObject));
            return;
        }
        if (definition.ActionType == FieldEventActionType.Recover)
        {
            Shape(PrimitiveType.Cylinder, "Healing spring stone", parent, at + Vector3.up * .07f, new Vector3(.86f, .10f, .86f), "Spring stone", new Color(.58f, .61f, .64f));
            Shape(PrimitiveType.Cylinder, "Healing water", parent, at + Vector3.up * .18f, new Vector3(.67f, .025f, .67f), "Spring water", new Color(.12f, .86f, .69f), .8f);
            AddPermanentRing(at + Vector3.up * .21f, parent, new Color(.34f, 1f, .78f), .38f);
            return;
        }
        if (definition.Id.Contains("sign", StringComparison.Ordinal))
        {
            Shape(PrimitiveType.Cube, "Sign post", parent, at + Vector3.up * .36f, new Vector3(.1f, .72f, .1f), "Sign wood", new Color(.48f, .31f, .17f));
            Shape(PrimitiveType.Cube, "Notice board", parent, at + Vector3.up * .72f, new Vector3(.66f, .36f, .10f), "Sign face", new Color(.75f, .58f, .31f));
            return;
        }
        var tint = new Color(definition.DisplayColor.R / 255f, definition.DisplayColor.G / 255f, definition.DisplayColor.B / 255f);
        var npc = MakeHero(parent, definition.Id, Color.Lerp(tint, new Color(.30f, .37f, .48f), .35f));
        npc.position = at;
        npc.localScale = definition.Id == "town_child" ? Vector3.one * .70f : Vector3.one * .94f;
        npc.rotation = Quaternion.Euler(0, 150f, 0);
        var head = npc.Find("Head");
        if (head != null) npcHeads.Add(head);
        if (definition.ActionType == FieldEventActionType.Bank)
        {
            Shape(PrimitiveType.Cube, "Bank counter", npc, new Vector3(0, .38f, .42f), new Vector3(.92f, .7f, .36f), "Counter wood", new Color(.28f, .21f, .18f));
            Shape(PrimitiveType.Cylinder, "Coin stack", npc, new Vector3(.22f, .80f, .44f), new Vector3(.15f, .04f, .15f), "Gold", new Color(.96f, .78f, .28f));
        }
    }

    private void RebuildBattle(Session session)
    {
        ClearEffects();
        Destroy(battleRoot);
        builtEncounter = session.Encounter;
        battleRoot = Child("Battle arena", root.transform);
        battleRoot.SetActive(true);
        Shape(PrimitiveType.Cylinder, "Arena plinth", battleRoot.transform, new Vector3(0, -.14f, 0), new Vector3(9f, .16f, 9f), "Arena stone", new Color(.25f, .27f, .34f));
        Shape(PrimitiveType.Cylinder, "Arena inset", battleRoot.transform, new Vector3(0, .018f, 0), new Vector3(7.8f, .015f, 7.8f), "Arena inset", new Color(.34f, .34f, .40f));
        for (var i = 0; i < 10; i++)
        {
            var angle = i * Mathf.PI * .2f;
            var at = new Vector3(Mathf.Cos(angle) * 4.0f, 0, Mathf.Sin(angle) * 4.0f);
            Shape(PrimitiveType.Cube, "Ancient stone", battleRoot.transform, at + Vector3.up * .35f, new Vector3(.34f, .7f + (i % 3) * .15f, .45f), "Arena ruin", new Color(.29f, .30f, .39f));
            if (i % 2 == 0) AddTorch(at + Vector3.up * .88f, battleRoot.transform, session.MapId == FieldMapId.Dungeon);
        }
        battleHero = MakeHero(battleRoot.transform, "Battle hero", new Color(.16f, .43f, .72f));
        battleHero.position = new Vector3(-1.85f, 0, 2.40f);
        battleHero.rotation = Quaternion.Euler(0, 150f, 0);
        enemy = MakeEnemy(session.Encounter!.Enemy.Id, battleRoot.transform);
        enemyRenderers = enemy.GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length > 0)
        {
            var bounds = enemyRenderers[0].bounds;
            foreach (var renderer in enemyRenderers) bounds.Encapsulate(renderer.bounds);
            var height = Mathf.Max(.01f, bounds.size.y);
            var silhouetteHeight = Mathf.Clamp(height, 1.2f, 1.7f);
            enemyBaseScale = Vector3.one * (silhouetteHeight / height);
            enemy.localScale = enemyBaseScale;
            battleLookHeight = bounds.center.y * enemyBaseScale.y + .30f;
        }
        heroRenderers = battleHero.GetComponentsInChildren<Renderer>();
        enemyFlash = heroFlash = enemyLunge = heroLunge = defeatRemaining = 0;
        defeatPlayed = false;
    }

    private void UpdateBattle(float dt)
    {
        enemyFlash = Mathf.Max(0, enemyFlash - dt);
        heroFlash = Mathf.Max(0, heroFlash - dt);
        enemyLunge = Mathf.Max(0, enemyLunge - dt);
        heroLunge = Mathf.Max(0, heroLunge - dt);
        if (enemy != null)
        {
            var flying = builtEncounter?.Enemy.Id is "ember_bat" or "wyvern_scout";
            var bob = Mathf.Sin(elapsed * (flying ? 5f : 3f)) * (flying ? .14f : .025f);
            enemy.localPosition = new Vector3(0, bob, Mathf.Sin(enemyLunge / .38f * Mathf.PI) * .48f);
            enemy.localRotation = Quaternion.Euler(0, Mathf.Sin(elapsed * 1.6f) * 4f, 0);
            if (defeatPlayed)
            {
                defeatRemaining = Mathf.Max(0, defeatRemaining - dt);
                enemy.localScale = enemyBaseScale * Mathf.Max(.01f, defeatRemaining / .55f);
                enemy.localRotation *= Quaternion.Euler(0, (1f - defeatRemaining / .55f) * 180f, 0);
                enemy.gameObject.SetActive(defeatRemaining > 0);
            }
            Flash(enemyRenderers, enemyFlash);
        }
        if (battleHero != null)
        {
            var lunge = Mathf.Sin(heroLunge / .34f * Mathf.PI) * .40f;
            battleHero.localPosition = new Vector3(-1.85f + lunge * .4f, 0, 2.40f - lunge);
            Flash(heroRenderers, heroFlash);
        }
    }

    private Transform MakeHero(Transform parent, string name, Color tunic)
    {
        var hero = Child(name, parent).transform;
        var skin = new Color(.94f, .73f, .53f);
        Shape(PrimitiveType.Cube, "Left boot", hero, new Vector3(-.13f, .12f, .045f), new Vector3(.17f, .23f, .28f), "Boot leather", new Color(.20f, .16f, .19f));
        Shape(PrimitiveType.Cube, "Right boot", hero, new Vector3(.13f, .12f, .045f), new Vector3(.17f, .23f, .28f), "Boot leather", new Color(.20f, .16f, .19f));
        Shape(PrimitiveType.Capsule, "Body", hero, new Vector3(0, .55f, 0), new Vector3(.43f, .31f, .30f), "Tunic " + ColorUtility.ToHtmlStringRGB(tunic), tunic);
        Shape(PrimitiveType.Cube, "Cape", hero, new Vector3(0, .54f, -.19f), new Vector3(.42f, .58f, .055f), "Crimson cape", new Color(.59f, .13f, .21f));
        var head = Shape(PrimitiveType.Sphere, "Head", hero, new Vector3(0, .96f, 0), new Vector3(.36f, .39f, .34f), "Skin", skin);
        Shape(PrimitiveType.Sphere, "Hair", head, new Vector3(0, .35f, -.06f), new Vector3(1.04f, .60f, 1.04f), "Dark hair", new Color(.17f, .12f, .15f));
        Shape(PrimitiveType.Cube, "Left eye", head, new Vector3(-.19f, .045f, .46f), new Vector3(.08f, .10f, .07f), "Eyes", new Color(.05f, .075f, .10f));
        Shape(PrimitiveType.Cube, "Right eye", head, new Vector3(.19f, .045f, .46f), new Vector3(.08f, .10f, .07f), "Eyes", new Color(.05f, .075f, .10f));
        Shape(PrimitiveType.Capsule, "Left arm", hero, new Vector3(-.29f, .56f, .02f), new Vector3(.14f, .22f, .14f), "Skin", skin);
        Shape(PrimitiveType.Capsule, "Right arm", hero, new Vector3(.29f, .56f, .02f), new Vector3(.14f, .22f, .14f), "Skin", skin);
        Shape(PrimitiveType.Cube, "Sword blade", hero, new Vector3(.34f, .76f, .22f), new Vector3(.055f, .72f, .09f), "Steel", new Color(.72f, .84f, .90f));
        Shape(PrimitiveType.Cube, "Sword guard", hero, new Vector3(.34f, .44f, .22f), new Vector3(.24f, .045f, .09f), "Gold", new Color(.96f, .78f, .28f));
        return hero;
    }

    private Transform MakeEnemy(string id, Transform parent)
    {
        var model = Child(id, parent).transform;
        Color tint;
        if (id == "horn_slime")
        {
            tint = new Color(.31f, .72f, .72f);
            Shape(PrimitiveType.Sphere, "Slime body", model, new Vector3(0, .45f, 0), new Vector3(1.25f, .86f, 1.05f), "Slime", tint, .12f);
            Shape(PrimitiveType.Capsule, "Horn", model, new Vector3(0, 1.02f, -.08f), new Vector3(.14f, .21f, .14f), "Ivory", new Color(.94f, .88f, .65f));
            Eyes(model, .42f, .42f, .17f);
        }
        else if (id is "moss_toad" or "bog_lizard" or "iron_mite")
        {
            tint = id == "iron_mite" ? new Color(.42f, .46f, .55f) : new Color(.29f, .52f, .25f);
            Shape(PrimitiveType.Sphere, "Creature body", model, new Vector3(0, .40f, 0), new Vector3(1.2f, .72f, 1.0f), id + " body", tint);
            for (var side = -1; side <= 1; side += 2)
            {
                Shape(PrimitiveType.Sphere, "Haunch", model, new Vector3(side * .48f, .19f, -.2f), new Vector3(.47f, .36f, .60f), id + " body", tint);
                Shape(PrimitiveType.Capsule, "Front foot", model, new Vector3(side * .42f, .12f, .37f), new Vector3(.14f, .12f, .4f), id + " body", tint);
                Shape(PrimitiveType.Sphere, "Eye mound", model, new Vector3(side * .27f, .72f, .29f), new Vector3(.28f, .27f, .26f), id + " body", tint);
            }
            Eyes(model, .76f, .4f, .26f);
        }
        else if (id == "night_shade" || id == "bell_armor")
        {
            tint = id == "night_shade" ? new Color(.22f, .17f, .40f) : new Color(.56f, .55f, .63f);
            Shape(PrimitiveType.Capsule, "Armored body", model, new Vector3(0, .80f, 0), new Vector3(.9f, .65f, .65f), id + " body", tint);
            Shape(PrimitiveType.Sphere, "Helmet", model, new Vector3(0, 1.6f, 0), new Vector3(.70f, .65f, .60f), id + " body", tint);
            Shape(PrimitiveType.Cube, "Visor", model, new Vector3(0, 1.63f, .29f), new Vector3(.44f, .10f, .06f), "Ember eyes", new Color(.96f, .23f, .18f), 1f);
            Shape(PrimitiveType.Sphere, "Left shoulder", model, new Vector3(-.55f, 1.16f, 0), new Vector3(.48f, .44f, .47f), id + " body", tint);
            Shape(PrimitiveType.Sphere, "Right shoulder", model, new Vector3(.55f, 1.16f, 0), new Vector3(.48f, .44f, .47f), id + " body", tint);
        }
        else if (id == "stone_wolf")
        {
            tint = new Color(.48f, .53f, .60f);
            Shape(PrimitiveType.Capsule, "Wolf torso", model, new Vector3(0, .68f, -.1f), new Vector3(.66f, .48f, 1.05f), "Wolf", tint);
            Shape(PrimitiveType.Sphere, "Wolf head", model, new Vector3(0, 1.0f, .48f), new Vector3(.61f, .64f, .7f), "Wolf", tint);
            Shape(PrimitiveType.Cube, "Muzzle", model, new Vector3(0, .86f, .83f), new Vector3(.36f, .28f, .36f), "Wolf", tint);
            for (var side = -1; side <= 1; side += 2)
            {
                Shape(PrimitiveType.Cube, "Ear", model, new Vector3(side * .22f, 1.41f, .41f), new Vector3(.18f, .31f, .16f), "Wolf", tint);
                for (var leg = -1; leg <= 1; leg += 2)
                    Shape(PrimitiveType.Capsule, "Leg", model, new Vector3(side * .27f, .29f, leg * .36f), new Vector3(.17f, .29f, .18f), "Wolf", tint);
            }
            Eyes(model, 1.10f, .78f, .18f);
        }
        else
        {
            var bat = id == "ember_bat";
            tint = bat ? new Color(.68f, .24f, .22f) : id == "lava_drake" ? new Color(.65f, .24f, .14f) : new Color(.35f, .39f, .64f);
            var bodyHeight = bat ? 1.0f : .75f;
            Shape(PrimitiveType.Sphere, "Dragon body", model, new Vector3(0, bodyHeight, -.05f), new Vector3(.98f, .90f, .84f), id + " body", tint);
            Shape(PrimitiveType.Sphere, "Dragon head", model, new Vector3(0, bodyHeight + .42f, .46f), new Vector3(.76f, .66f, .81f), id + " body", tint);
            Shape(PrimitiveType.Cube, "Dragon muzzle", model, new Vector3(0, bodyHeight + .27f, .83f), new Vector3(.47f, .31f, .40f), id + " body", tint);
            for (var side = -1; side <= 1; side += 2)
            {
                var wing = Shape(PrimitiveType.Cube, "Wing", model, new Vector3(side * .87f, bodyHeight + .22f, -.12f), new Vector3(1.2f, .08f, .7f), id + " wings", Color.Lerp(tint, new Color(.20f, .13f, .26f), .40f));
                wing.localRotation = Quaternion.Euler(15, side * -22f, side * -18f);
                Shape(PrimitiveType.Capsule, "Horn", model, new Vector3(side * .24f, bodyHeight + .84f, .35f), new Vector3(.12f, .22f, .12f), "Ivory", new Color(.94f, .88f, .65f));
                if (!bat) Shape(PrimitiveType.Capsule, "Dragon foot", model, new Vector3(side * .37f, .23f, .10f), new Vector3(.29f, .23f, .51f), id + " body", tint);
            }
            var tail = Shape(PrimitiveType.Capsule, "Tail", model, new Vector3(0, bodyHeight - .2f, -.67f), new Vector3(.18f, .55f, .18f), id + " body", tint);
            tail.localRotation = Quaternion.Euler(65, 0, 0);
            Eyes(model, bodyHeight + .50f, .80f, .23f);
        }
        return model;
    }

    private void Eyes(Transform parent, float height, float front, float spacing)
    {
        for (var side = -1; side <= 1; side += 2)
            Shape(PrimitiveType.Sphere, "Creature eye", parent, new Vector3(side * spacing, height, front), new Vector3(.12f, .15f, .08f), "Creature eyes", new Color(1f, .91f, .39f), .3f);
    }

    private void AddPortal(Vector3 position, Transform parent, bool castle)
    {
        var tint = castle ? new Color(.76f, .59f, .25f) : new Color(.22f, .69f, .66f);
        for (var side = -1; side <= 1; side += 2)
            Shape(PrimitiveType.Cube, "Gate column", parent, position + new Vector3(side * .47f, .60f, 0), new Vector3(.18f, 1.2f, .24f), "Portal stone", new Color(.40f, .45f, .52f));
        Shape(PrimitiveType.Cube, "Gate lintel", parent, position + Vector3.up * 1.26f, new Vector3(1.12f, .18f, .25f), "Portal trim " + castle, tint);
        AddPermanentRing(position + Vector3.up * .08f, parent, tint, .46f);
    }

    private void AddPermanentRing(Vector3 position, Transform parent, Color tint, float radius)
    {
        var ring = Child("Runic light ring", parent).transform;
        ring.position = position;
        ring.localScale = Vector3.one * radius;
        ring.gameObject.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        var renderer = ring.gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Material("Rune " + ColorUtility.ToHtmlStringRGB(tint), tint, .9f);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        portalRings.Add(ring);
    }

    private void AddTorch(Vector3 position, Transform parent, bool dungeon)
    {
        var tint = dungeon ? new Color(.34f, .64f, 1f) : new Color(1f, .50f, .18f);
        Shape(PrimitiveType.Cube, "Torch holder", parent, position, new Vector3(.10f, .26f, .13f), "Torch iron", new Color(.18f, .17f, .21f));
        Shape(PrimitiveType.Sphere, "Torch flame", parent, position + Vector3.up * .2f, new Vector3(.16f, .3f, .16f), "Flame " + dungeon, tint, 1.8f);
        var lamp = Child("Torch light", parent).AddComponent<Light>();
        lamp.transform.position = position + Vector3.up * .24f;
        lamp.type = LightType.Point;
        lamp.color = tint;
        lamp.intensity = .85f;
        lamp.range = 3.1f;
        lamp.cullingMask = 1 << WorldLayer;
    }

    private void BuildBatch(MeshBuilder builder, string name, Color color, float emission = 0)
    {
        if (builder.Count == 0 || fieldRoot == null) return;
        var mesh = builder.Build(name);
        mapMeshes.Add(mesh);
        var batch = Child(name, fieldRoot.transform);
        batch.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = batch.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Material(name + " " + ColorUtility.ToHtmlStringRGB(color), color, emission);
        renderer.shadowCastingMode = name == "Water" ? ShadowCastingMode.Off : ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private Transform Shape(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string materialName, Color color, float emission = 0)
    {
        var shape = GameObject.CreatePrimitive(type);
        shape.name = name;
        shape.layer = WorldLayer;
        shape.transform.SetParent(parent, false);
        shape.transform.localPosition = position;
        shape.transform.localScale = scale;
        var collider = shape.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
            Destroy(collider);
        }
        shape.GetComponent<Renderer>().sharedMaterial = Material(materialName, color, emission);
        return shape.transform;
    }

    private Material Material(string key, Color color, float emission = 0)
    {
        if (materials.TryGetValue(key, out var material)) return material;
        var shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
        if (shader == null) throw new InvalidOperationException("A built-in Unity color shader is required for the RPG world.");
        material = new Material(shader) { name = "DragonGlare " + key, color = color, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", .12f);
        if (emission > 0 && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        materials.Add(key, material);
        return material;
    }

    private void Flash(Renderer[] renderers, float duration)
    {
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (duration <= 0) renderer.SetPropertyBlock(null);
            else
            {
                flashProperties.Clear();
                flashProperties.SetColor("_Color", Color.Lerp(renderer.sharedMaterial.color, Color.white, duration / .25f));
                renderer.SetPropertyBlock(flashProperties);
            }
        }
    }

    private void Slash(Vector3 position, Color color)
    {
        var slash = Shape(PrimitiveType.Cube, "Sword light streak", root.transform, position, new Vector3(.075f, 1.75f, .08f), "Slash " + ColorUtility.ToHtmlStringRGB(color), color, 1.4f);
        slash.rotation = Quaternion.Euler(0, 0, -42);
        effects.Add(new Effect(slash.gameObject, .30f, EffectKind.Slash, slash.position, slash.localScale));
        Burst(position, color, 18, .7f);
    }

    private void Ring(Vector3 position, Color color, float size)
    {
        var go = Child("Spell shockwave", root.transform);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * .1f;
        go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = Material("Effect ring " + ColorUtility.ToHtmlStringRGB(color), color, 1f);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        effects.Add(new Effect(go, .70f, EffectKind.Ring, position, Vector3.one * size));
    }

    private void Burst(Vector3 position, Color tint, int count, float speed, bool rising = false)
    {
        var go = Child("RPG particles", root.transform);
        go.transform.position = position;
        var particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(.35f, .85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * .35f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(.045f, .14f);
        main.startColor = tint;
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = rising ? -.17f : .12f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = .27f;
        var colorOverLife = particles.colorOverLifetime;
        colorOverLife.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(tint, .35f), new GradientColorKey(tint, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.75f, .5f), new GradientAlphaKey(0, 1) });
        colorOverLife.color = gradient;
        var sizeOverLife = particles.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        particles.Play();
        particles.Emit(count);
        effects.Add(new Effect(go, 1.2f, EffectKind.Particles, position, Vector3.one));
    }

    private Material ParticleMaterial()
    {
        const string key = "Particles";
        if (materials.TryGetValue(key, out var cached)) return cached;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Transparent");
        if (shader == null) return Material("Particle fallback", Color.white, 1f);
        var material = new Material(shader) { name = "DragonGlare particle glow", mainTexture = particleTexture, hideFlags = HideFlags.DontSave };
        materials.Add(key, material);
        return material;
    }

    private void AddPointLight(Vector3 position, Color color, float intensity, float duration)
    {
        var go = Child("Magic flash light", root.transform);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4;
        light.color = color;
        light.intensity = intensity;
        light.cullingMask = 1 << WorldLayer;
        effects.Add(new Effect(go, duration, EffectKind.Light, position, Vector3.one * intensity));
    }

    private void UpdateEffects(float dt)
    {
        for (var i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            effect.Remaining -= dt;
            if (effect.Remaining <= 0 || effect.Object == null)
            {
                Destroy(effect.Object);
                effects.RemoveAt(i);
                continue;
            }
            var progress = 1f - effect.Remaining / effect.Duration;
            switch (effect.Kind)
            {
                case EffectKind.Slash:
                    effect.Object.transform.position = effect.Origin + new Vector3(progress * .75f - .2f, 0, 0);
                    effect.Object.transform.localScale = effect.Scale * (1f - progress);
                    break;
                case EffectKind.Ring:
                    effect.Object.transform.localScale = effect.Scale * Mathf.Sin(progress * Mathf.PI * .5f);
                    effect.Object.transform.position = effect.Origin + Vector3.up * progress * .14f;
                    if (progress > .8f) effect.Object.transform.localScale *= (1 - progress) * 5f;
                    break;
                case EffectKind.Light:
                    effect.Object.GetComponent<Light>().intensity = effect.Scale.x * (1 - progress);
                    break;
            }
        }
    }

    private static bool HasWalkableNeighbor(int[,] map, int x, int y)
    {
        return (x > 0 && MapFactory.IsWalkableTileId(map[y, x - 1])) ||
               (y > 0 && MapFactory.IsWalkableTileId(map[y - 1, x])) ||
               (x + 1 < map.GetLength(1) && MapFactory.IsWalkableTileId(map[y, x + 1])) ||
               (y + 1 < map.GetLength(0) && MapFactory.IsWalkableTileId(map[y + 1, x]));
    }

    private static Vector2Int GetInteriorCutawayMidpoint(int[,] map)
    {
        var firstX = map.GetLength(1);
        var firstZ = map.GetLength(0);
        var lastX = -1;
        var lastZ = -1;
        for (var z = 0; z < map.GetLength(0); z++)
        for (var x = 0; x < map.GetLength(1); x++)
        {
            if (!MapFactory.IsWalkableTileId(map[z, x])) continue;
            firstX = Math.Min(firstX, x);
            firstZ = Math.Min(firstZ, z);
            lastX = Math.Max(lastX, x);
            lastZ = Math.Max(lastZ, z);
        }
        return lastZ < firstZ ? new Vector2Int(map.GetLength(1), map.GetLength(0)) :
            new Vector2Int((firstX + lastX) / 2, (firstZ + lastZ) / 2);
    }

    private static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name) { layer = WorldLayer };
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Texture2D MakeParticleTexture()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RPG radial glow", hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear };
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var distance = new Vector2((x + .5f) / size * 2 - 1, (y + .5f) / size * 2 - 1).magnitude;
            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1f - distance), 1.4f));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static Mesh MakeRingMesh()
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (var i = 0; i < 48; i++)
        {
            var a = i * Mathf.PI * 2 / 48;
            var b = (i + 1) * Mathf.PI * 2 / 48;
            var start = vertices.Count;
            vertices.Add(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
            vertices.Add(new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)));
            vertices.Add(new Vector3(Mathf.Cos(b) * .84f, 0, Mathf.Sin(b) * .84f));
            vertices.Add(new Vector3(Mathf.Cos(a) * .84f, 0, Mathf.Sin(a) * .84f));
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }
        var mesh = new Mesh { name = "RPG rune ring", hideFlags = HideFlags.DontSave };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ClearEffects()
    {
        foreach (var effect in effects) Destroy(effect.Object);
        effects.Clear();
    }

    private static void Destroy(Object? value)
    {
        if (value == null) return;
        if (value is GameObject go) go.SetActive(false);
        if (Application.isPlaying) Object.Destroy(value);
        else Object.DestroyImmediate(value);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        camera.enabled = false;
        camera.targetTexture = null;
        Texture.Release();
        Destroy(Texture);
        ClearEffects();
        Destroy(root);
        foreach (var material in materials.Values) Destroy(material);
        foreach (var mesh in mapMeshes) Destroy(mesh);
        Destroy(ringMesh);
        Destroy(particleTexture);
        materials.Clear();
        mapMeshes.Clear();
    }

    private sealed record Chest(string Id, Transform Lid, GameObject Glow);
    private enum EffectKind { Particles, Slash, Ring, Light }
    private sealed class Effect(GameObject value, float duration, EffectKind kind, Vector3 origin, Vector3 scale)
    {
        public GameObject Object { get; } = value;
        public float Duration { get; } = duration;
        public float Remaining { get; set; } = duration;
        public EffectKind Kind { get; } = kind;
        public Vector3 Origin { get; } = origin;
        public Vector3 Scale { get; } = scale;
    }

    /// <summary>Static terrain is batched by material, avoiding one GameObject/material per tile.</summary>
    private sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<int> indices = new();
        public int Count => vertices.Count;

        public void Box(Vector3 center, Vector3 size)
        {
            var h = size * .5f;
            Quad(center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(-h.x, h.y, h.z), center + new Vector3(h.x, h.y, h.z), center + new Vector3(h.x, h.y, -h.z));
            Quad(center + new Vector3(-h.x, -h.y, h.z), center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3(h.x, -h.y, -h.z), center + new Vector3(h.x, -h.y, h.z));
            Quad(center + new Vector3(-h.x, -h.y, h.z), center + new Vector3(h.x, -h.y, h.z), center + new Vector3(h.x, h.y, h.z), center + new Vector3(-h.x, h.y, h.z));
            Quad(center + new Vector3(h.x, -h.y, -h.z), center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(h.x, h.y, -h.z));
            Quad(center + new Vector3(h.x, -h.y, h.z), center + new Vector3(h.x, -h.y, -h.z), center + new Vector3(h.x, h.y, -h.z), center + new Vector3(h.x, h.y, h.z));
            Quad(center + new Vector3(-h.x, -h.y, -h.z), center + new Vector3(-h.x, -h.y, h.z), center + new Vector3(-h.x, h.y, h.z), center + new Vector3(-h.x, h.y, -h.z));
        }

        public void Cone(Vector3 center, float radius, float height, int sides) => Cylinder(center, radius, height, sides, true);
        public void Cylinder(Vector3 center, float radius, float height, int sides, bool cone = false)
        {
            for (var i = 0; i < sides; i++)
            {
                var a = i * Mathf.PI * 2 / sides;
                var b = (i + 1) * Mathf.PI * 2 / sides;
                var bottomA = center + new Vector3(Mathf.Cos(a) * radius, -height * .5f, Mathf.Sin(a) * radius);
                var bottomB = center + new Vector3(Mathf.Cos(b) * radius, -height * .5f, Mathf.Sin(b) * radius);
                var topA = cone ? center + Vector3.up * height * .5f : bottomA + Vector3.up * height;
                var topB = cone ? topA : bottomB + Vector3.up * height;
                Quad(bottomA, topA, topB, bottomB);
                Triangle(center - Vector3.up * height * .5f, bottomA, bottomB);
                if (!cone) Triangle(center + Vector3.up * height * .5f, topB, topA);
            }
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var start = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        private void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var start = vertices.Count;
            vertices.AddRange(new[] { a, b, c });
            indices.AddRange(new[] { start, start + 1, start + 2 });
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
