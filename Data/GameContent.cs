using DragonGlareAlpha.Domain;
using DragonGlareAlpha.Domain.Battle;
using DragonGlareAlpha.Domain.Field;
using DragonGlareAlpha.Domain.Items;
using DragonGlareAlpha.Domain.Player;


namespace DragonGlareAlpha.Data;

public static class GameContent
{
    public static readonly string[][] JapaneseNameTable =
    [
        ["あ", "い", "う", "え", "お", "か", "き", "く", "け", "こ"],
        ["さ", "し", "す", "せ", "そ", "た", "ち", "つ", "て", "と"],
        ["な", "に", "ぬ", "ね", "の", "は", "ひ", "ふ", "へ", "ほ"],
        ["ま", "み", "む", "め", "も", "や", "ゆ", "よ", "わ", "を"],
        ["ら", "り", "る", "れ", "ろ", "ん", "ー", "゛", "けす", "おわり"]
    ];

    public static readonly string[][] EnglishNameTable =
    [
        ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J"],
        ["K", "L", "M", "N", "O", "P", "Q", "R", "S", "T"],
        ["U", "V", "W", "X", "Y", "Z", "-", "'", "DEL", "END"]
    ];

    public static readonly string[,] BattleCommandLabels =
    {
        { "こうげき", "じゅもん" },
        { "どうぐ", "にげる" }
    };

    public static readonly BattleActionType[,] BattleCommandGrid =
    {
        { BattleActionType.Attack, BattleActionType.Spell },
        { BattleActionType.Item, BattleActionType.Run }
    };
    public static readonly WeaponDefinition[] WeaponCatalog =
    [
        new("stick", "ぼう", 16, 2),
        new("club", "こんぼう", 32, 4),
        new("bamboo_spear", "たけやり", 52, 6),
        new("thorn_club", "とげのぼう", 64, 7),
        new("wood_blade", "ぼくとう", 82, 8),
        new("stone_axe", "いしのおの", 128, 11),
        new("bronze_sword", "どうのつるぎ", 196, 14),
        new("iron_sword", "てつのけん", 288, 17),
        new("steel_blade", "はがねけん", 416, 20),
        new("dragon_lance", "りゅうのやり", 580, 24)
    ];

    public static readonly ArmorDefinition[] ArmorCatalog =
    [
        new("cloth_tunic", "ぬののふく", 18, 1),
        new("leather_armor", "かわのよろい", 48, 3),
        new("scale_vest", "うろこふく", 72, 4),
        new("bronze_mail", "どうよろい", 108, 6),
        new("iron_armor", "てつよろい", 152, 8),
        new("steel_armor", "はがねよろい", 224, 10),
        new("silver_mail", "ぎんむねあて", 336, 13),
        new("dragon_mail", "りゅうよろい", 492, 16)
    ];

    public static readonly IEquipmentDefinition[] ShopCatalog =
    [
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
    ];

    public static readonly EnemyDefinition[] EnemyCatalog =
    [
        new("horn_slime", "ホーンスライム", FieldMapId.Hub, 1, 2, 6, 18, 5, 1, 8, 12, new EnemyDropDefinition("healing_herb", 24)),
        new("moss_toad", "モストード", FieldMapId.Hub, 1, 4, 4, 24, 7, 2, 12, 18, new EnemyDropDefinition("healing_herb", 18)),
        new("ember_bat", "エンバーバット", FieldMapId.Hub, 3, 6, 2, 30, 9, 3, 16, 24, new EnemyDropDefinition("mana_seed", 14)),
        new("iron_mite", "アイアンマイト", FieldMapId.Castle, 1, 4, 5, 26, 8, 3, 13, 20, new EnemyDropDefinition("healing_herb", 18)),
        new("night_shade", "ナイトシェイド", FieldMapId.Castle, 3, 7, 3, 38, 11, 5, 24, 34, new EnemyDropDefinition("mana_seed", 15)),
        new("bell_armor", "ベルアーマー", FieldMapId.Castle, 5, 10, 2, 50, 14, 7, 38, 54, new EnemyDropDefinition("fire_orb", 12)),
        new("bog_lizard", "ボグリザード", FieldMapId.Field, 2, 5, 5, 34, 10, 4, 20, 28, new EnemyDropDefinition("healing_herb", 18)),
        new("stone_wolf", "ストーンウルフ", FieldMapId.Field, 4, 8, 4, 46, 14, 7, 34, 46, new EnemyDropDefinition("mana_seed", 15)),
        new("dragon_pup", "ドラゴンパピー", FieldMapId.Field, 6, 11, 3, 58, 18, 9, 48, 68, new EnemyDropDefinition("fire_orb", 12)),
        new("wyvern_scout", "ワイバーンスカウト", FieldMapId.Field, 9, 15, 3, 72, 21, 11, 66, 96, new EnemyDropDefinition("mana_seed", 10)),
        new("lava_drake", "ラヴァドレイク", FieldMapId.Field, 13, 99, 2, 90, 25, 13, 88, 132, new EnemyDropDefinition("fire_orb", 15)),
        new("ancient_wyrm", "エンシェントワーム", FieldMapId.Field, 18, 99, 1, 112, 29, 15, 120, 180, new EnemyDropDefinition("mana_seed", 12))
    ];

    public static readonly ConsumableDefinition[] ConsumableCatalog =
    [
        new("healing_herb", "やくそう", "HPを 12かいふく", ConsumableEffectType.HealHp, 12),
        new("mana_seed", "まりょくのたね", "MPを 3かいふく", ConsumableEffectType.HealMp, 3),
        new("fire_orb", "ひのたま", "てきに 18ダメージ", ConsumableEffectType.DamageEnemy, 18)
    ];

    public static readonly FieldTransitionDefinition[] FieldTransitions =
    [
        new(FieldMapId.Hub, new Rectangle(9, 0, 2, 1), FieldMapId.Castle, new Point(9, 12)),
        new(FieldMapId.Hub, new Rectangle(19, 7, 1, 2), FieldMapId.Field, new Point(2, 7)),
        new(FieldMapId.Castle, new Rectangle(9, 14, 2, 1), FieldMapId.Hub, new Point(9, 2)),
        new(FieldMapId.Field, new Rectangle(0, 7, 1, 2), FieldMapId.Hub, new Point(15, 7))
    ];

    public static readonly FieldEventDefinition[] FieldEvents =
    [
        new(
            "guide_npc",
            FieldMapId.Hub,
            new Point(12, 7),
            Color.Cyan,
            true,
            FieldEventActionType.Dialogue,
            [
                "{player}、ようこそ。\nけんをみがき たびのしたくをしよう。".Replace("、", "、"),
                "やくそうは HPを なおし、\nひのたまは どうぐで なげられるぞ。"
            ],
            [
                "Welcome, {player}.\nSharpen your blade and prepare.",
                "Herbs heal you.\nFire orbs can be thrown from ITEMS."
            ],
            SpriteAssetName: "guide_npc.png"),
        new(
            "town_child",
            FieldMapId.Hub,
            new Point(4, 4),
            Color.FromArgb(120, 255, 180),
            true,
            FieldEventActionType.Dialogue,
            [
                "まちの こどもだ。\n「おしろの へいしって すごく まじめだよ！」",
                "「フィールドの くさむらは\n　まものが でやすいから きをつけてね。」"
            ],
            [
                "A village child grins.\n\"The castle guard is super serious!\"",
                "\"Watch the tall grass out in the field.\nMonsters jump out fast there.\""
            ],
            SpriteAssetName: "town_child.png"),
        new(
            "castle_guard",
            FieldMapId.Castle,
            new Point(12, 11),
            Color.FromArgb(255, 180, 120),
            true,
            FieldEventActionType.Dialogue,
            [
                "おしろの へいしだ。\n「りゅうの ひかりを おうものよ、あわてるな。」",
                "「レベルが あがったら そうびも みなおせ。\n　ちからだけでは かてぬぞ。」"
            ],
            [
                "A castle guard stands firm.\n\"Hunter of dragonlight, do not rush.\"",
                "\"When you grow stronger, review your gear.\nPower alone will not carry you.\""
            ],
            SpriteAssetName: "castle_guard.png"),
        new(
            "field_scout",
            FieldMapId.Field,
            new Point(11, 11),
            Color.FromArgb(255, 228, 120),
            true,
            FieldEventActionType.Dialogue,
            [
                "みはりの ぼうけんしゃだ。\n「このさきは ぬかるみが おおい。」",
                "「HPが へったら いったん もどれ。\n　むりやり すすむと いたいめを みるぞ。」"
            ],
            [
                "A field scout watches the road.\n\"The ground ahead gets rough.\"",
                "\"If your HP drops, fall back first.\nPushing through carelessly will cost you.\""
            ],
            SpriteAssetName: "field_scout.png"),
        new(
            "field_sign",
            FieldMapId.Hub,
            new Point(2, 12),
            Color.Gold,
            true,
            FieldEventActionType.Dialogue,
            [
                "たてふだだ。\nXで ステータスをひらける。",
                "Bで バトル、Vで ショップ。\nZで イベントを よめる。"
            ],
            [
                "A sign reads:\nPress X to open STATUS.",
                "Press B for battle, V for shop,\nand Z to inspect events."
            ]),
        new(
            "healing_spring",
            FieldMapId.Hub,
            new Point(16, 12),
            Color.MediumSpringGreen,
            true,
            FieldEventActionType.Recover,
            [
                "きらめく いずみだ。",
                "みずの ちからが からだに しみこんだ。"
            ],
            [
                "A shining spring bubbles here.",
                "The water restores your strength."
            ],
            RecoverHp: 999,
            RecoverMp: 999)
    ];

    public static string[][] GetNameTable(UiLanguage language)
    {
        return language == UiLanguage.Japanese ? JapaneseNameTable : EnglishNameTable;
    }

    public static WeaponDefinition? GetWeaponById(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        return WeaponCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
    }

    public static ArmorDefinition? GetArmorById(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        return ArmorCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
    }

    public static ConsumableDefinition? GetConsumableById(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        return ConsumableCatalog.FirstOrDefault(item => string.Equals(item.Id, itemId, StringComparison.Ordinal));
    }

    public static FieldEventDefinition? GetFieldEventById(string? eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        return FieldEvents.FirstOrDefault(fieldEvent => string.Equals(fieldEvent.Id, eventId, StringComparison.Ordinal));
    }
}
