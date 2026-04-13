using System;
using System.Linq;
using UnityEngine;

namespace DragonGlareAlpha.Unity
{
    public static class GameContent
    {
        public static readonly string[][] JapaneseNameTable = new string[][]
        {
            new string[] { "あ", "い", "う", "え", "お", "か", "き", "く", "け", "こ" },
            new string[] { "さ", "し", "す", "せ", "そ", "た", "ち", "つ", "て", "と" },
            new string[] { "な", "に", "ぬ", "ね", "の", "は", "ひ", "ふ", "へ", "ほ" },
            new string[] { "ま", "み", "む", "め", "も", "や", "ゆ", "よ", "わ", "を" },
            new string[] { "ら", "り", "る", "れ", "ろ", "ん", "ー", "゛", "けす", "おわり" }
        };

        public static readonly string[][] EnglishNameTable = new string[][]
        {
            new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" },
            new string[] { "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T" },
            new string[] { "U", "V", "W", "X", "Y", "Z", "-", "'", "DEL", "END" }
        };

        public static readonly string[,] JapaneseBattleCommandLabels = new string[,]
        {
            { "こうげき", "じゅもん" },
            { "ぼうぎょ", "どうぐ" },
            { "そうび", "にげる" }
        };

        public static readonly string[,] EnglishBattleCommandLabels = new string[,]
        {
            { "ATTACK", "SPELL" },
            { "GUARD", "ITEM" },
            { "EQUIP", "RUN" }
        };

        public static readonly BattleActionType[,] BattleCommandGrid = new BattleActionType[,]
        {
            { BattleActionType.Attack, BattleActionType.Spell },
            { BattleActionType.Defend, BattleActionType.Item },
            { BattleActionType.Equip, BattleActionType.Run }
        };

        public static readonly WeaponDefinition[] WeaponCatalog = new WeaponDefinition[]
        {
            new WeaponDefinition("stick", "ぼう", 16, 2),
            new WeaponDefinition("club", "こんぼう", 32, 4),
            new WeaponDefinition("bamboo_spear", "たけやり", 52, 6),
            new WeaponDefinition("thorn_club", "とげのぼう", 64, 7),
            new WeaponDefinition("wood_blade", "ぼくとう", 82, 8),
            new WeaponDefinition("stone_axe", "いしのおの", 128, 11),
            new WeaponDefinition("bronze_sword", "どうのつるぎ", 196, 14),
            new WeaponDefinition("iron_sword", "てつのけん", 288, 17),
            new WeaponDefinition("steel_blade", "はがねけん", 416, 20),
            new WeaponDefinition("dragon_lance", "りゅうのやり", 580, 24)
        };

        public static readonly ArmorDefinition[] ArmorCatalog = new ArmorDefinition[]
        {
            new ArmorDefinition("cloth_tunic", "ぬののふく", 18, 1),
            new ArmorDefinition("leather_armor", "かわのよろい", 48, 3),
            new ArmorDefinition("scale_vest", "うろこふく", 72, 4),
            new ArmorDefinition("bronze_mail", "どうよろい", 108, 6),
            new ArmorDefinition("iron_armor", "てつよろい", 152, 8),
            new ArmorDefinition("steel_armor", "はがねよろい", 224, 10),
            new ArmorDefinition("silver_mail", "ぎんむねあて", 336, 13),
            new ArmorDefinition("dragon_mail", "りゅうよろい", 492, 16)
        };

        public static readonly IEquipmentDefinition[] ShopCatalog = new IEquipmentDefinition[]
        {
            WeaponCatalog[0],
            ArmorCatalog[0],
            WeaponCatalog[1],
            ArmorCatalog[1],
            WeaponCatalog[2],
            WeaponCatalog[3],
            ArmorCatalog[2],
            WeaponCatalog[4],
            ArmorCatalog[3],
            WeaponCatalog[5],
            ArmorCatalog[4],
            WeaponCatalog[6],
            ArmorCatalog[5],
            WeaponCatalog[7],
            ArmorCatalog[6],
            WeaponCatalog[8],
            ArmorCatalog[7],
            WeaponCatalog[9]
        };

        public static readonly EnemyDefinition[] EnemyCatalog = new EnemyDefinition[]
        {
            new EnemyDefinition("horn_slime", "ホーンスライム", FieldMapId.Hub, 1, 2, 6, 18, 5, 1, 8, 12, new EnemyDropDefinition("healing_herb", 24, 1)),
            new EnemyDefinition("moss_toad", "モストード", FieldMapId.Hub, 1, 4, 4, 24, 7, 2, 12, 18, new EnemyDropDefinition("healing_herb", 18, 1)),
            new EnemyDefinition("ember_bat", "エンバーバット", FieldMapId.Hub, 3, 6, 2, 30, 9, 3, 16, 24, new EnemyDropDefinition("mana_seed", 14, 1)),
            new EnemyDefinition("iron_mite", "アイアンマイト", FieldMapId.Castle, 1, 4, 5, 26, 8, 3, 13, 20, new EnemyDropDefinition("healing_herb", 18, 1)),
            new EnemyDefinition("night_shade", "ナイトシェイド", FieldMapId.Castle, 3, 7, 3, 38, 11, 5, 24, 34, new EnemyDropDefinition("mana_seed", 15, 1)),
            new EnemyDefinition("bell_armor", "ベルアーマー", FieldMapId.Castle, 5, 10, 2, 50, 14, 7, 38, 54, new EnemyDropDefinition("fire_orb", 12, 1)),
            new EnemyDefinition("bog_lizard", "ボグリザード", FieldMapId.Field, 2, 5, 5, 34, 10, 4, 20, 28, new EnemyDropDefinition("healing_herb", 18, 1)),
            new EnemyDefinition("stone_wolf", "ストーンウルフ", FieldMapId.Field, 4, 8, 4, 46, 14, 7, 34, 46, new EnemyDropDefinition("mana_seed", 15, 1)),
            new EnemyDefinition("dragon_pup", "ドラゴンパピー", FieldMapId.Field, 6, 11, 3, 58, 18, 9, 48, 68, new EnemyDropDefinition("fire_orb", 12, 1)),
            new EnemyDefinition("wyvern_scout", "ワイバーンスカウト", FieldMapId.Field, 9, 15, 3, 72, 21, 11, 66, 96, new EnemyDropDefinition("mana_seed", 10, 1)),
            new EnemyDefinition("lava_drake", "ラヴァドレイク", FieldMapId.Field, 13, 99, 2, 90, 25, 13, 88, 132, new EnemyDropDefinition("fire_orb", 15, 1)),
            new EnemyDefinition("ancient_wyrm", "エンシェントワーム", FieldMapId.Field, 18, 99, 1, 112, 29, 15, 120, 180, new EnemyDropDefinition("mana_seed", 12, 1))
        };

        public static readonly ConsumableDefinition[] ConsumableCatalog = new ConsumableDefinition[]
        {
            new ConsumableDefinition("healing_herb", "やくそう", "HPを 12かいふく", ConsumableEffectType.HealHp, 12),
            new ConsumableDefinition("mana_seed", "まりょくのたね", "MPを 3かいふく", ConsumableEffectType.HealMp, 3),
            new ConsumableDefinition("fire_orb", "ひのたま", "てきに 18ダメージ", ConsumableEffectType.DamageEnemy, 18)
        };

        public static readonly FieldTransitionDefinition[] FieldTransitions = new FieldTransitionDefinition[]
        {
            new FieldTransitionDefinition(FieldMapId.Hub, new RectInt(9, 0, 2, 1), FieldMapId.Castle, new Vector2Int(9, 12)),
            new FieldTransitionDefinition(FieldMapId.Hub, new RectInt(19, 7, 1, 2), FieldMapId.Field, new Vector2Int(2, 7)),
            new FieldTransitionDefinition(FieldMapId.Castle, new RectInt(9, 14, 2, 1), FieldMapId.Hub, new Vector2Int(9, 2)),
            new FieldTransitionDefinition(FieldMapId.Field, new RectInt(0, 7, 1, 2), FieldMapId.Hub, new Vector2Int(15, 7))
        };

        public static readonly FieldEventDefinition[] FieldEvents = new FieldEventDefinition[]
        {
            new FieldEventDefinition(
                "guide_npc",
                FieldMapId.Hub,
                new Vector2Int(12, 7),
                new Color32(0, 255, 255, 255),
                true,
                FieldEventActionType.Dialogue,
                new string[]
                {
                    "{player}、ようこそ。\nけんをみがき たびのしたくをしよう。",
                    "やくそうは HPを なおし、\nひのたまは どうぐで なげられるぞ。"
                },
                new string[]
                {
                    "Welcome, {player}.\nSharpen your blade and prepare.",
                    "Herbs heal you.\nFire orbs can be thrown from ITEMS."
                },
                "guide_npc.png",
                "guide-4.png",
                0,
                0),
            new FieldEventDefinition(
                "town_child",
                FieldMapId.Hub,
                new Vector2Int(4, 4),
                new Color32(120, 255, 180, 255),
                true,
                FieldEventActionType.Dialogue,
                new string[]
                {
                    "まちの こどもだ。\n「おしろの へいしって すごく まじめだよ！」",
                    "「フィールドの くさむらは\n　まものが でやすいから きをつけてね。」"
                },
                new string[]
                {
                    "A village child grins.\n\"The castle guard is super serious!\"",
                    "\"Watch the tall grass out in the field.\nMonsters jump out fast there.\""
                },
                "town_child.png",
                "young-5.png",
                0,
                0),
            new FieldEventDefinition(
                "castle_guard",
                FieldMapId.Castle,
                new Vector2Int(12, 11),
                new Color32(255, 180, 120, 255),
                true,
                FieldEventActionType.Dialogue,
                new string[]
                {
                    "おしろの へいしだ。\n「りゅうの ひかりを おうものよ、あわてるな。」",
                    "「レベルが あがったら そうびも みなおせ。\n　ちからだけでは かてぬぞ。」"
                },
                new string[]
                {
                    "A castle guard stands firm.\n\"Hunter of dragonlight, do not rush.\"",
                    "\"When you grow stronger, review your gear.\nPower alone will not carry you.\""
                },
                "castle_guard.png",
                "castle-guard-4.png",
                0,
                0),
            new FieldEventDefinition(
                "field_scout",
                FieldMapId.Field,
                new Vector2Int(11, 11),
                new Color32(255, 228, 120, 255),
                true,
                FieldEventActionType.Dialogue,
                new string[]
                {
                    "みはりの ぼうけんしゃだ。\n「このさきは ぬかるみが おおい。」",
                    "「HPが へったら いったん もどれ。\n　むりやり すすむと いたいめを みるぞ。」"
                },
                new string[]
                {
                    "A field scout watches the road.\n\"The ground ahead gets rough.\"",
                    "\"If your HP drops, fall back first.\nPushing through carelessly will cost you.\""
                },
                "field_scout.png",
                "mihari-3.png",
                0,
                0),
            new FieldEventDefinition(
                "field_sign",
                FieldMapId.Hub,
                new Vector2Int(2, 12),
                new Color32(255, 215, 0, 255),
                true,
                FieldEventActionType.Dialogue,
                new string[]
                {
                    "たてふだだ。\nXで ステータスをひらける。",
                    "Bで バトル、Vで ショップ。\nZで イベントを よめる。"
                },
                new string[]
                {
                    "A sign reads:\nPress X to open STATUS.",
                    "Press B for battle, V for shop,\nand Z to inspect events."
                },
                string.Empty,
                string.Empty,
                0,
                0),
            new FieldEventDefinition(
                "healing_spring",
                FieldMapId.Hub,
                new Vector2Int(16, 12),
                new Color32(0, 250, 154, 255),
                true,
                FieldEventActionType.Recover,
                new string[]
                {
                    "きらめく いずみだ。",
                    "みずの ちからが からだに しみこんだ。"
                },
                new string[]
                {
                    "A shining spring bubbles here.",
                    "The water restores your strength."
                },
                string.Empty,
                string.Empty,
                999,
                999)
        };

        public static string[][] GetNameTable(UiLanguage language)
        {
            return language == UiLanguage.Japanese ? JapaneseNameTable : EnglishNameTable;
        }

        public static string GetBattleCommandLabel(UiLanguage language, int row, int column)
        {
            string[,] labels = language == UiLanguage.English ? EnglishBattleCommandLabels : JapaneseBattleCommandLabels;
            return labels[row, column];
        }

        public static WeaponDefinition GetWeaponById(string itemId)
        {
            return WeaponCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
        }

        public static ArmorDefinition GetArmorById(string itemId)
        {
            return ArmorCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
        }

        public static ConsumableDefinition GetConsumableById(string itemId)
        {
            return ConsumableCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
        }
    }

    public static class MapFactory
    {
        public const int FloorTile = 0;
        public const int WallTile = 1;
        public const int CastleBlockTile = 2;
        public const int CastleGateTile = 3;
        public const int FieldGateTile = 4;
        public const int CastleFloorTile = 5;
        public const int GrassTile = 6;
        public const int DecorationBlueTile = 7;

        public static int[,] CreateDefaultMap()
        {
            return CreateMap(FieldMapId.Hub);
        }

        public static int[,] CreateMap(FieldMapId mapId)
        {
            switch (mapId)
            {
                case FieldMapId.Castle:
                    return CreateCastleMap();
                case FieldMapId.Field:
                    return CreateFieldMap();
                default:
                    return CreateHubMap();
            }
        }

        private static int[,] CreateHubMap()
        {
            int[,] map = CreateBoundedMap();
            PaintArea(map, 9, 0, 10, 0, CastleGateTile);
            PaintArea(map, 19, 7, 19, 8, FieldGateTile);

            for (int x = 4; x <= 15; x++)
            {
                map[10, x] = WallTile;
            }

            map[10, 9] = FloorTile;
            map[10, 10] = FloorTile;
            map[6, 6] = WallTile;
            map[6, 7] = WallTile;
            map[7, 6] = WallTile;
            return map;
        }

        private static int[,] CreateCastleMap()
        {
            int[,] map = CreateBoundedMap();
            PaintArea(map, 1, 1, 18, 13, CastleFloorTile);
            PaintArea(map, 7, 2, 12, 3, CastleBlockTile);
            PaintArea(map, 4, 4, 5, 11, WallTile);
            PaintArea(map, 14, 4, 15, 11, WallTile);
            PaintArea(map, 9, 14, 10, 14, CastleGateTile);
            return map;
        }

        private static int[,] CreateFieldMap()
        {
            int[,] map = CreateBoundedMap();
            PaintArea(map, 0, 7, 0, 8, FieldGateTile);
            PaintArea(map, 2, 2, 6, 5, GrassTile);
            PaintArea(map, 10, 3, 16, 6, GrassTile);
            PaintArea(map, 5, 9, 12, 12, GrassTile);
            PaintArea(map, 8, 7, 9, 8, DecorationBlueTile);
            PaintArea(map, 14, 10, 15, 11, DecorationBlueTile);
            return map;
        }

        private static int[,] CreateBoundedMap()
        {
            int[,] map = new int[15, 20];

            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    map[y, x] = x == 0 || y == 0 || x == map.GetLength(1) - 1 || y == map.GetLength(0) - 1
                        ? WallTile
                        : FloorTile;
                }
            }

            return map;
        }

        private static void PaintArea(int[,] map, int left, int top, int right, int bottom, int tile)
        {
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    map[y, x] = tile;
                }
            }
        }
    }
}
