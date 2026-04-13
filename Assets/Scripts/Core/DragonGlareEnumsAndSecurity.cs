using System;
using System.Security.Cryptography;

namespace DragonGlareAlpha.Unity
{
    public enum GameState
    {
        ModeSelect,
        LanguageSelection,
        NameInput,
        SaveSlotSelection,
        Field,
        EncounterTransition,
        Battle,
        ShopBuy
    }

    public enum SaveSlotSelectionMode
    {
        Save,
        Load
    }

    public enum ShopPhase
    {
        Welcome,
        BuyList
    }

    public enum EquipmentSlot
    {
        Weapon,
        Armor
    }

    public enum UiLanguage
    {
        Japanese,
        English
    }

    public enum FieldMapId
    {
        Hub,
        Castle,
        Field
    }

    public enum BgmTrack
    {
        MainMenu,
        Field,
        Castle,
        Battle,
        Shop
    }

    public enum SoundEffect
    {
        Dialog,
        Collision
    }

    public enum BattleFlowState
    {
        Intro,
        CommandSelection,
        ItemSelection,
        EquipmentSelection,
        Resolving,
        Victory,
        Defeat,
        Escaped
    }

    public enum BattleActionType
    {
        Attack,
        Spell,
        Defend,
        Item,
        Equip,
        Run
    }

    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat,
        Escaped,
        Invalid
    }

    public enum BattleVisualCue
    {
        None,
        EnemyHit,
        PlayerHit,
        SpellCast,
        PlayerHeal,
        MpRecover,
        EnemyDefeat,
        ItemUse
    }

    public enum ConsumableEffectType
    {
        HealHp,
        HealMp,
        DamageEnemy
    }

    public enum FieldEventActionType
    {
        Dialogue,
        Recover
    }

    public enum SaveSlotState
    {
        Empty,
        Occupied,
        Corrupted
    }

    public enum SaveLoadFailureReason
    {
        None,
        NotFound,
        InvalidSignature,
        InvalidFormat
    }

    public sealed class TamperDetectedException : InvalidOperationException
    {
        public TamperDetectedException(string message) : base(message)
        {
        }
    }

    public sealed class ProtectedInt
    {
        private readonly int salt = NextNonZeroInt32();
        private int encodedValue;
        private int mask;
        private int mirror;
        private int tag;

        public ProtectedInt()
            : this(0)
        {
        }

        public ProtectedInt(int value)
        {
            Write(value);
        }

        public int Value
        {
            get
            {
                int value = ReadAndValidate();
                Write(value);
                return value;
            }
            set { Write(value); }
        }

        public void Validate()
        {
            ReadAndValidate();
        }

        public void Rekey()
        {
            int value = ReadAndValidate();
            Write(value);
        }

        private int ReadAndValidate()
        {
            int value = encodedValue ^ mask;
            int expectedMirror = RotateLeft((~value) ^ salt, 11);
            int expectedTag = ComputeTag(encodedValue, mask, expectedMirror, salt);
            if (mirror != expectedMirror || tag != expectedTag)
            {
                throw new TamperDetectedException("メモリ改ざんを検知しました。");
            }

            return value;
        }

        private void Write(int value)
        {
            mask = NextNonZeroInt32();
            encodedValue = value ^ mask;
            mirror = RotateLeft((~value) ^ salt, 11);
            tag = ComputeTag(encodedValue, mask, mirror, salt);
        }

        private static int ComputeTag(int encoded, int currentMask, int currentMirror, int currentSalt)
        {
            unchecked
            {
                int hash = encoded ^ 0x5F356495;
                hash = (hash * 397) ^ RotateLeft(currentMask, 5);
                hash = (hash * 397) ^ RotateLeft(currentMirror, 13);
                hash = (hash * 397) ^ currentSalt;
                return hash;
            }
        }

        private static int NextNonZeroInt32()
        {
            byte[] buffer = new byte[sizeof(int)];
            int value = 0;

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                while (value == 0)
                {
                    rng.GetBytes(buffer);
                    value = BitConverter.ToInt32(buffer, 0);
                }
            }

            return value;
        }

        private static int RotateLeft(int value, int offset)
        {
            uint unsignedValue = (uint)value;
            return unchecked((int)((unsignedValue << offset) | (unsignedValue >> (32 - offset))));
        }
    }
}
