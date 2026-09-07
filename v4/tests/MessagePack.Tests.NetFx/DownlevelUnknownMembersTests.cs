using MessagePack;

namespace MessagePack.Tests.NetFx;

// Runs MessagePackUnknownMembers capture/replay on the netstandard2.0 asset via .NET
// Framework 4.8, where the whole graph rides the Compatible buffer pair: ReadRaw's
// measure-then-copy walk and the replay writes execute their downlevel branches. Byte
// semantics are asserted by the main test project; this is the tier smoke.
public class DownlevelUnknownMembersTests
{
    [Fact]
    public void CaptureAndReplayRoundtrip()
    {
        var bytes = MessagePackSerializer.Serialize(new DlWideMap { A = 42, B = "b", C = "extra" });
        var narrow = MessagePackSerializer.Deserialize<DlNarrowMap>(bytes)!;
        Assert.Equal(1, narrow.Extra!.Count);
        Assert.Equal(bytes, MessagePackSerializer.Serialize(narrow));

        var wideBack = MessagePackSerializer.Deserialize<DlWideMap>(MessagePackSerializer.Serialize(narrow))!;
        Assert.Equal("extra", wideBack.C);
    }
}

[MessagePackObject(true)]
public class DlWideMap
{
    public int A { get; set; }
    public string? B { get; set; }
    public string? C { get; set; }
}

[MessagePackObject(true)]
public class DlNarrowMap
{
    public int A { get; set; }
    public string? B { get; set; }
    public MessagePackUnknownMembers? Extra { get; set; }
}
