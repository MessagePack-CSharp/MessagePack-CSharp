using MessagePack;
using SerializerFoundation;

namespace SandboxLibrary;

public struct SandboxPoint
{
    public int X;
    public int Y;
}

// The Round's partial style, now the baseline for custom formatters: no constraints and
// no #if here — the BufferConstraintsGenerator emits the other partial declaration with
// `where T : struct, IWriteBuffer/IReadBuffer` plus `allows ref struct` on TFMs that can
// express it. Bodies are intentionally no-ops: the sandbox exercises the constraint/TFM
// surface, not serialization logic (hence the untouched-buffer rule is switched off here).
#pragma warning disable MsgPack114
public sealed partial class SandboxPointFormatter<TWriteBuffer, TReadBuffer>
    : IMessagePackFormatter<TWriteBuffer, TReadBuffer, SandboxPoint>
{
    public void Initialize(MessagePackFormatterResolver resolver)
    {
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, SandboxPoint value)
    {
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref SandboxPoint value)
    {
        value = default;
    }
}
#pragma warning restore MsgPack114
