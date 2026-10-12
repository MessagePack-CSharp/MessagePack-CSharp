using System.Threading;
using MessagePack;

namespace MessagePack.Tests.NetFx;

// .NET Framework x64 is where a level of object / object[] / Dictionary<object, object> cost about 2.4 KB of stack:
// about 210 levels overflowed a 512 KB thread (IIS x64 w3wp's default), well within MaxDepth 500, and the process died.
// The netstandard2.0 build must now either read such a payload or refuse it with the data error. A regression here
// crashes the test host instead of failing a test.
public class DownlevelStackTests
{
    const int IisStack = 512 * 1024;

    static Exception? RunOn(int stackSize, Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        }, stackSize);
        thread.Start();
        thread.Join();
        return caught;
    }

    static byte[] NestedArrays(int levels)
    {
        var bytes = new byte[levels + 1];
        for (int i = 0; i < levels; i++)
        {
            bytes[i] = 0x91; // fixarray(1) per level
        }
        bytes[levels] = 0xC0;
        return bytes;
    }

    static byte[] NestedMaps(int levels)
    {
        var bytes = new byte[levels * 3 + 1];
        for (int i = 0; i < levels; i++)
        {
            bytes[i * 3] = 0x81;
            bytes[i * 3 + 1] = 0xA1;
            bytes[i * 3 + 2] = (byte)'a';
        }
        bytes[bytes.Length - 1] = 0xC0;
        return bytes;
    }

    [Fact]
    public void ObjectPayloadWithinMaxDepth_OnA512KBThread_ReadsOrRefuses()
    {
        foreach (var payload in new[] { NestedArrays(499), NestedMaps(499) })
        {
            var ex = RunOn(IisStack, () => MessagePackSerializer.Deserialize<object>(payload));
            Assert.True(ex is null or MessagePackSerializationException, ex?.ToString());
        }
    }

    [Fact]
    public void UnlimitedDepth_OnA512KBThread_Refuses()
    {
        var unlimited = MessagePackSerializerOptions.Default with { MaxDepth = 0 };
        var ex = RunOn(IisStack, () => MessagePackSerializer.Deserialize<object>(NestedArrays(100_000), unlimited));
        Assert.IsType<MessagePackSerializationException>(ex);
    }

    [Fact]
    public void ConvertToJson_OnA512KBThread_ReadsOrRefuses()
    {
        var ex = RunOn(IisStack, () => MessagePackSerializer.ConvertToJson(NestedArrays(499)));
        Assert.True(ex is null or MessagePackSerializationException, ex?.ToString());
    }
}

// CultureInfoFormatter (opt-in) reads predefined names only: .NET Framework would otherwise synthesize and cache a
// custom culture for any BCP-47-shaped payload name
public class DownlevelCultureTests
{
    static readonly MessagePackSerializerOptions Globalization = new(new MessagePackFormatterResolver(
        MessagePackFormatterFactory.Combine(new CultureInfoFormatterFactory(), MessagePackFormatterFactory.Default)));

    [Fact]
    public void PredefinedCultures_Roundtrip_AndSynthesizableNamesAreRefused()
    {
        var ja = System.Globalization.CultureInfo.GetCultureInfo("ja-JP");
        Assert.Equal(ja, MessagePackSerializer.Deserialize<System.Globalization.CultureInfo>(MessagePackSerializer.Serialize(ja, Globalization), Globalization));
        Assert.Equal(System.Globalization.CultureInfo.InvariantCulture, MessagePackSerializer.Deserialize<System.Globalization.CultureInfo>(MessagePackSerializer.Serialize(System.Globalization.CultureInfo.InvariantCulture, Globalization), Globalization));
        Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<System.Globalization.CultureInfo>(MessagePackSerializer.Serialize("zz-Zzzz-ZZ"), Globalization));
        Assert.Throws<InvalidOperationException>(() => MessagePackSerializer.Serialize(ja)); // not on the default chain
    }
}
