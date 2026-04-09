using System.Numerics;
using System.Security.Cryptography;

namespace DragonGlareAlpha.Security;

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
            var value = ReadAndValidate();
            Write(value);
            return value;
        }
        set => Write(value);
    }

    public void Validate()
    {
        _ = ReadAndValidate();
    }

    public void Rekey()
    {
        var value = ReadAndValidate();
        Write(value);
    }

    private int ReadAndValidate()
    {
        var value = encodedValue ^ mask;
        var expectedMirror = RotateLeft((~value) ^ salt, 11);
        var expectedTag = ComputeTag(encodedValue, mask, expectedMirror, salt);
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
            var hash = encoded ^ 0x5F356495;
            hash = (hash * 397) ^ RotateLeft(currentMask, 5);
            hash = (hash * 397) ^ RotateLeft(currentMirror, 13);
            hash = (hash * 397) ^ currentSalt;
            return hash;
        }
    }

    private static int NextNonZeroInt32()
    {
        var value = 0;
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        while (value == 0)
        {
            RandomNumberGenerator.Fill(buffer);
            value = BitConverter.ToInt32(buffer);
        }

        return value;
    }

    private static int RotateLeft(int value, int offset)
    {
        return unchecked((int)BitOperations.RotateLeft((uint)value, offset));
    }
}
