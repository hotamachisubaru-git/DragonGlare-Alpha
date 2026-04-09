using System.Reflection;
using DragonGlareAlpha.Security;

namespace DragonGlareAlpha.Tests;

public sealed class AntiCheatTests
{
    [Fact]
    public void ProtectedInt_WhenInternalStateWasMutated_ThrowsTamperDetectedException()
    {
        var protectedInt = new ProtectedInt(220);
        var encodedValueField = typeof(ProtectedInt).GetField("encodedValue", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(encodedValueField);
        encodedValueField!.SetValue(protectedInt, 1);

        Assert.Throws<TamperDetectedException>(() => _ = protectedInt.Value);
    }
}
