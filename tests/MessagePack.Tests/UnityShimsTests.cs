using System.Buffers.Binary;
using MessagePack;
using MessagePack.Unity;
using UnityEngine;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;
using V4Options = MessagePack.MessagePackSerializerOptions;

namespace MessagePack.Tests;

// MessagePack.UnityShims: the UnityEngine formatters (shared source with the MessagePack.Unity package) against the
// shim types. The wire is MessagePack-CSharp v3's MessagePack.Unity form and is pinned here byte for byte, since
// v3's UnityShims assembly cannot load next to v4 (it binds to the assembly name "MessagePack"). Every type
// roundtrips, tolerates the v3 read semantics (extra elements skipped, missing ones default), and rides the default
// chain's generic tier for Nullable / array / List.
public class UnityShimsTests
{
    static V4Options Options { get; } = V4Options.Default.WithUnity();

    static byte[] Float32(float value)
    {
        var bytes = new byte[5];
        bytes[0] = 0xca;
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(1), BitConverter.SingleToInt32Bits(value));
        return bytes;
    }

    static byte[] Concat(params byte[][] parts) => parts.SelectMany(p => p).ToArray();

    [Fact]
    public void Vector3_Codec_IsFixArrayOfFloat32()
    {
        var bytes = V4.Serialize(new Vector3(1f, 2f, 3f), Options);
        Assert.Equal(Concat([0x93], Float32(1f), Float32(2f), Float32(3f)), bytes);
        Assert.Equal(new Vector3(1f, 2f, 3f), V4.Deserialize<Vector3>(bytes, Options));
    }

    [Fact]
    public void Vector2_Vector4_Quaternion_Color_Rect_Keyframe_Codec()
    {
        Assert.Equal(Concat([0x92], Float32(1f), Float32(2f)), V4.Serialize(new Vector2(1f, 2f), Options));
        Assert.Equal(Concat([0x94], Float32(1f), Float32(2f), Float32(3f), Float32(4f)), V4.Serialize(new Vector4(1f, 2f, 3f, 4f), Options));
        Assert.Equal(Concat([0x94], Float32(0f), Float32(0f), Float32(0f), Float32(1f)), V4.Serialize(Quaternion.identity, Options));
        Assert.Equal(Concat([0x94], Float32(0.5f), Float32(0.25f), Float32(0.125f), Float32(1f)), V4.Serialize(new Color(0.5f, 0.25f, 0.125f), Options));
        Assert.Equal(Concat([0x94], Float32(1f), Float32(2f), Float32(3f), Float32(4f)), V4.Serialize(new Rect(1f, 2f, 3f, 4f), Options));
        Assert.Equal(Concat([0x94], Float32(1f), Float32(2f), Float32(3f), Float32(4f)), V4.Serialize(new Keyframe(1f, 2f, 3f, 4f), Options));
    }

    [Fact]
    public void Matrix4x4_Codec_IsColumnMajorFieldOrder()
    {
        var m = Matrix4x4.identity;
        m.m10 = 2f; // row 1, column 0: the second element on the wire
        var bytes = V4.Serialize(m, Options);
        Assert.Equal(0xdc, bytes[0]); // array16(16)
        Assert.Equal(16, bytes[2]);
        Assert.Equal(Float32(1f), bytes.AsSpan(3, 5).ToArray());  // m00
        Assert.Equal(Float32(2f), bytes.AsSpan(8, 5).ToArray());  // m10
        Assert.Equal(Float32(0f), bytes.AsSpan(13, 5).ToArray()); // m20
        Assert.Equal(m, V4.Deserialize<Matrix4x4>(bytes, Options));
    }

    [Fact]
    public void Bounds_Codec_IsNestedCenterAndSize()
    {
        var bounds = new Bounds(new Vector3(1f, 2f, 3f), new Vector3(4f, 6f, 8f));
        var bytes = V4.Serialize(bounds, Options);
        Assert.Equal(Concat([0x92, 0x93], Float32(1f), Float32(2f), Float32(3f), [0x93], Float32(4f), Float32(6f), Float32(8f)), bytes);
        Assert.Equal(bounds, V4.Deserialize<Bounds>(bytes, Options));
    }

    [Fact]
    public void Color32_Codec_IsUInt8()
    {
        var bytes = V4.Serialize(new Color32(1, 127, 128, 255), Options);
        Assert.Equal(new byte[] { 0x94, 0x01, 0x7f, 0xcc, 0x80, 0xcc, 0xff }, bytes);
        Assert.Equal(new Color32(1, 127, 128, 255), V4.Deserialize<Color32>(bytes, Options));
    }

    [Fact]
    public void IntegerTypes_Codec()
    {
        Assert.Equal(new byte[] { 0x92, 0x01, 0xff }, V4.Serialize(new Vector2Int(1, -1), Options));
        Assert.Equal(new byte[] { 0x93, 0x01, 0x02, 0xcd, 0x01, 0x00 }, V4.Serialize(new Vector3Int(1, 2, 256), Options));
        Assert.Equal(new byte[] { 0x92, 0x05, 0x0a }, V4.Serialize(new RangeInt(5, 10), Options));
        Assert.Equal(new byte[] { 0x94, 0x01, 0x02, 0x03, 0x04 }, V4.Serialize(new RectInt(1, 2, 3, 4), Options));
        Assert.Equal(new byte[] { 0x92, 0x93, 0x01, 0x02, 0x03, 0x93, 0x04, 0x05, 0x06 }, V4.Serialize(new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6)), Options));
        Assert.Equal(new byte[] { 0x91, 0x08 }, V4.Serialize((LayerMask)8, Options));
    }

    [Fact]
    public void AnimationCurve_Codec_PostWrapModeBeforePre()
    {
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 2f, 3f, 4f))
        {
            preWrapMode = WrapMode.Loop,
            postWrapMode = WrapMode.PingPong,
        };
        var bytes = V4.Serialize(curve, Options);
        Assert.Equal(
            Concat(
                [0x93, 0x92, 0x94], Float32(0f), Float32(1f), Float32(0f), Float32(0f),
                [0x94], Float32(1f), Float32(2f), Float32(3f), Float32(4f),
                [(byte)WrapMode.PingPong, (byte)WrapMode.Loop]),
            bytes);
        var back = V4.Deserialize<AnimationCurve>(bytes, Options)!;
        Assert.Equal(curve.keys, back.keys);
        Assert.Equal(WrapMode.Loop, back.preWrapMode);
        Assert.Equal(WrapMode.PingPong, back.postWrapMode);
    }

    [Fact]
    public void Gradient_Codec_AndRoundtrip()
    {
        var gradient = new Gradient
        {
            colorKeys = [new GradientColorKey(new Color(1f, 0f, 0f, 1f), 0f), new GradientColorKey(new Color(0f, 0f, 1f, 1f), 1f)],
            alphaKeys = [new GradientAlphaKey(1f, 0f)],
            mode = GradientMode.Fixed,
        };
        var bytes = V4.Serialize(gradient, Options);
        Assert.Equal(
            Concat(
                [0x93, 0x92, 0x92, 0x94], Float32(1f), Float32(0f), Float32(0f), Float32(1f), Float32(0f),
                [0x92, 0x94], Float32(0f), Float32(0f), Float32(1f), Float32(1f), Float32(1f),
                [0x91, 0x92], Float32(1f), Float32(0f),
                [(byte)GradientMode.Fixed]),
            bytes);
        var back = V4.Deserialize<Gradient>(bytes, Options)!;
        Assert.Equal(gradient.colorKeys, back.colorKeys);
        Assert.Equal(gradient.alphaKeys, back.alphaKeys);
        Assert.Equal(GradientMode.Fixed, back.mode);
    }

    [Fact]
    public void ClassTypes_NullIsNil()
    {
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize<AnimationCurve?>(null, Options));
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize<Gradient?>(null, Options));
        Assert.Equal(new byte[] { 0xc0 }, V4.Serialize<RectOffset?>(null, Options));
        Assert.Null(V4.Deserialize<AnimationCurve?>(new byte[] { 0xc0 }, Options));
        Assert.Null(V4.Deserialize<Gradient?>(new byte[] { 0xc0 }, Options));
        Assert.Null(V4.Deserialize<RectOffset?>(new byte[] { 0xc0 }, Options));
        var offset = V4.Deserialize<RectOffset>(V4.Serialize(new RectOffset(1, 2, 3, 4), Options), Options)!;
        Assert.Equal((1, 2, 3, 4), (offset.left, offset.right, offset.top, offset.bottom));
    }

    [Fact]
    public void StructTypes_NilIsRejected()
    {
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Vector3>(new byte[] { 0xc0 }, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Bounds>(new byte[] { 0xc0 }, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Color32>(new byte[] { 0xc0 }, Options));
        Assert.Throws<MessagePackSerializationException>(() => V4.Deserialize<Vector2Int>(new byte[] { 0xc0 }, Options));
    }

    [Fact]
    public void SlowRead_ToleratesOtherWidthsAndCounts()
    {
        // ints where floats are expected (v3 wrote these through Write(float), readers accept any numeric)
        Assert.Equal(new Vector3(1f, 2f, 3f), V4.Deserialize<Vector3>(new byte[] { 0x93, 0x01, 0x02, 0x03 }, Options));
        // float64 elements
        var wide = new byte[1 + 2 * 9];
        wide[0] = 0x92;
        wide[1] = 0xcb;
        BinaryPrimitives.WriteInt64BigEndian(wide.AsSpan(2), BitConverter.DoubleToInt64Bits(1.5));
        wide[10] = 0xcb;
        BinaryPrimitives.WriteInt64BigEndian(wide.AsSpan(11), BitConverter.DoubleToInt64Bits(2.5));
        Assert.Equal(new Vector2(1.5f, 2.5f), V4.Deserialize<Vector2>(wide, Options));
        // too many elements: extras skipped; too few: rest default
        Assert.Equal(new Vector2(1f, 2f), V4.Deserialize<Vector2>(new byte[] { 0x93, 0x01, 0x02, 0x03 }, Options));
        Assert.Equal(new Vector4(1f, 0f, 0f, 0f), V4.Deserialize<Vector4>(new byte[] { 0x91, 0x01 }, Options));
        Assert.Equal(new Color32(1, 2, 0, 0), V4.Deserialize<Color32>(new byte[] { 0x92, 0x01, 0x02 }, Options));
        // a fifth, non-numeric element is skipped whole
        Assert.Equal(new RectInt(1, 2, 3, 4), V4.Deserialize<RectInt>(new byte[] { 0x95, 0x01, 0x02, 0x03, 0x04, 0xa1, (byte)'x' }, Options));
    }

    [Fact]
    public void Roundtrip_EveryType()
    {
        Roundtrip(new Vector2(1.5f, -2f));
        Roundtrip(new Vector3(1.5f, -2f, 3.25f));
        Roundtrip(new Vector4(1f, 2f, 3f, 4f));
        Roundtrip(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f));
        Roundtrip(new Color(0.1f, 0.2f, 0.3f, 0.4f));
        Roundtrip(new Color32(10, 20, 30, 40));
        Roundtrip(new Bounds(new Vector3(1f, 2f, 3f), new Vector3(2f, 4f, 6f)));
        Roundtrip(new Rect(1f, 2f, 3f, 4f));
        Roundtrip(new Keyframe(1f, 2f, 3f, 4f));
        Roundtrip(Matrix4x4.identity);
        Roundtrip(new GradientColorKey(new Color(1f, 1f, 1f, 1f), 0.5f));
        Roundtrip(new GradientAlphaKey(0.5f, 0.75f));
        Roundtrip((LayerMask)(1 << 5));
        Roundtrip(new Vector2Int(-1, 2));
        Roundtrip(new Vector3Int(1, -2, 300000));
        Roundtrip(new RangeInt(3, 4));
        Roundtrip(new RectInt(1, 2, 3, 4));
        Roundtrip(new BoundsInt(new Vector3Int(1, 2, 3), new Vector3Int(4, 5, 6)));
        Roundtrip(WrapMode.ClampForever);
        Roundtrip(GradientMode.PerceptualBlend);

        static void Roundtrip<T>(T value)
        {
            Assert.Equal(value, V4.Deserialize<T>(V4.Serialize(value, Options), Options));
        }
    }

    [Fact]
    public void GenericTier_WrapsUnityTypes()
    {
        Vector3? some = new Vector3(1f, 2f, 3f);
        Vector3? none = null;
        Assert.Equal(some, V4.Deserialize<Vector3?>(V4.Serialize(some, Options), Options));
        Assert.Equal(none, V4.Deserialize<Vector3?>(V4.Serialize(none, Options), Options));
        Vector3[] array = [new(1f, 2f, 3f), new(4f, 5f, 6f)];
        Assert.Equal(array, V4.Deserialize<Vector3[]>(V4.Serialize(array, Options), Options));
        var list = new List<Quaternion> { Quaternion.identity, new(1f, 2f, 3f, 4f) };
        Assert.Equal(list, V4.Deserialize<List<Quaternion>>(V4.Serialize(list, Options), Options));
        var dictionary = new Dictionary<string, Color32> { ["a"] = new(1, 2, 3, 4) };
        Assert.Equal(dictionary, V4.Deserialize<Dictionary<string, Color32>>(V4.Serialize(dictionary, Options), Options));
    }

    [Fact]
    public void GeneratedObject_WithUnityMembers()
    {
        var transform = new TransformSnapshot
        {
            Position = new Vector3(1f, 2f, 3f),
            Rotation = Quaternion.identity,
            Scale = new Vector3(1f, 1f, 1f),
            Tint = new Color32(255, 128, 0, 255),
            Path = [new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f)],
            Mask = 4,
        };
        var bytes = V4.Serialize(transform, Options);
        // [position, rotation, scale, tint, path, mask] as the generated array form
        Assert.Equal(0x96, bytes[0]);
        var back = V4.Deserialize<TransformSnapshot>(bytes, Options)!;
        Assert.Equal(transform.Position, back.Position);
        Assert.Equal(transform.Rotation, back.Rotation);
        Assert.Equal(transform.Scale, back.Scale);
        Assert.Equal(transform.Tint, back.Tint);
        Assert.Equal(transform.Path, back.Path);
        Assert.Equal(transform.Mask, back.Mask);
    }

    [Fact]
    public void DefaultOptions_DoNotServeUnityTypes()
    {
        // without the factory the types have no formatter (they are plain structs without [MessagePackObject]);
        // WithUnity is the opt-in
        Assert.ThrowsAny<Exception>(() => V4.Serialize(new Vector3(1f, 2f, 3f), V4Options.Default));
    }
}

[MessagePackObject]
public partial class TransformSnapshot
{
    [Key(0)] public Vector3 Position { get; set; }
    [Key(1)] public Quaternion Rotation { get; set; }
    [Key(2)] public Vector3 Scale { get; set; }
    [Key(3)] public Color32 Tint { get; set; }
    [Key(4)] public Vector3[]? Path { get; set; }
    [Key(5)] public LayerMask Mask { get; set; }
}
