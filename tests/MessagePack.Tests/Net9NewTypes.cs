#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.Tests;

public class Net9NewTypes
{
    // NET7

    [Theory]
    [Xunit.InlineData(0, 0)]
    [Xunit.InlineData(100, 200)]
    [Xunit.InlineData(ulong.MaxValue, 9999)]
    [Xunit.InlineData(9999, ulong.MaxValue)]
    [Xunit.InlineData(ulong.MaxValue, ulong.MaxValue)]
    public void Int128Test(ulong upper, ulong lower)
    {
        var i = new Int128(upper, lower);
        var bin = MessagePackSerializer.Serialize(i);
        var i2 = MessagePackSerializer.Deserialize<Int128>(bin);
        i.Should().Be(i2);
    }

    [Theory]
    [Xunit.InlineData(0, 0)]
    [Xunit.InlineData(100, 200)]
    [Xunit.InlineData(ulong.MaxValue, 9999)]
    [Xunit.InlineData(9999, ulong.MaxValue)]
    [Xunit.InlineData(ulong.MaxValue, ulong.MaxValue)]
    public void UInt128Test(ulong upper, ulong lower)
    {
        var i = new UInt128(upper, lower);
        var bin = MessagePackSerializer.Serialize(i);
        var i2 = MessagePackSerializer.Deserialize<UInt128>(bin);
        i.Should().Be(i2);
    }
}
#endif
