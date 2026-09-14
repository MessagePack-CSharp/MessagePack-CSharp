// Stand-ins for the UnityEngine value types, so code outside Unity (servers, tools, tests) can exchange the same
// wire. Member names and constructors mirror UnityEngine exactly where the shared formatters touch them; the rest of
// each type's Unity surface is deliberately absent. No MessagePack attributes: the formatters in
// MessagePack.Unity/Runtime serve these types on both sides, so there is one wire definition.
#nullable enable
#pragma warning disable IDE1006 // Unity's lower-case member names
using System;

namespace UnityEngine
{
    public struct Vector2 : IEquatable<Vector2>
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Vector2 other) => x == other.x && y == other.y;
        public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
        public static bool operator !=(Vector2 left, Vector2 right) => !left.Equals(right);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector3 : IEquatable<Vector3>
    {
        public float x;
        public float y;
        public float z;

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public bool Equals(Vector3 other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
        public static bool operator !=(Vector3 left, Vector3 right) => !left.Equals(right);
        public override string ToString() => $"({x}, {y}, {z})";
    }

    public struct Vector4 : IEquatable<Vector4>
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public bool Equals(Vector4 other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public override bool Equals(object? obj) => obj is Vector4 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);
        public static bool operator !=(Vector4 left, Vector4 right) => !left.Equals(right);
    }

    public struct Quaternion : IEquatable<Quaternion>
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Quaternion identity => new Quaternion(0f, 0f, 0f, 1f);

        public bool Equals(Quaternion other) => x == other.x && y == other.y && z == other.z && w == other.w;
        public override bool Equals(object? obj) => obj is Quaternion other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y, z, w);
        public static bool operator ==(Quaternion left, Quaternion right) => left.Equals(right);
        public static bool operator !=(Quaternion left, Quaternion right) => !left.Equals(right);
    }

    public struct Color : IEquatable<Color>
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public Color(float r, float g, float b)
            : this(r, g, b, 1f)
        {
        }

        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public bool Equals(Color other) => r == other.r && g == other.g && b == other.b && a == other.a;
        public override bool Equals(object? obj) => obj is Color other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public static bool operator ==(Color left, Color right) => left.Equals(right);
        public static bool operator !=(Color left, Color right) => !left.Equals(right);
    }

    public struct Color32 : IEquatable<Color32>
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public bool Equals(Color32 other) => r == other.r && g == other.g && b == other.b && a == other.a;
        public override bool Equals(object? obj) => obj is Color32 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public static bool operator ==(Color32 left, Color32 right) => left.Equals(right);
        public static bool operator !=(Color32 left, Color32 right) => !left.Equals(right);
    }

    public struct Bounds : IEquatable<Bounds>
    {
        Vector3 m_Center;
        Vector3 m_Extents;

        public Bounds(Vector3 center, Vector3 size)
        {
            m_Center = center;
            m_Extents = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        }

        public Vector3 center
        {
            get => m_Center;
            set => m_Center = value;
        }

        public Vector3 size
        {
            get => new Vector3(m_Extents.x * 2f, m_Extents.y * 2f, m_Extents.z * 2f);
            set => m_Extents = new Vector3(value.x * 0.5f, value.y * 0.5f, value.z * 0.5f);
        }

        public Vector3 extents
        {
            get => m_Extents;
            set => m_Extents = value;
        }

        public bool Equals(Bounds other) => m_Center == other.m_Center && m_Extents == other.m_Extents;
        public override bool Equals(object? obj) => obj is Bounds other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_Center, m_Extents);
        public static bool operator ==(Bounds left, Bounds right) => left.Equals(right);
        public static bool operator !=(Bounds left, Bounds right) => !left.Equals(right);
    }

    public struct Rect : IEquatable<Rect>
    {
        float m_XMin;
        float m_YMin;
        float m_Width;
        float m_Height;

        public Rect(float x, float y, float width, float height)
        {
            m_XMin = x;
            m_YMin = y;
            m_Width = width;
            m_Height = height;
        }

        public float x { get => m_XMin; set => m_XMin = value; }
        public float y { get => m_YMin; set => m_YMin = value; }
        public float width { get => m_Width; set => m_Width = value; }
        public float height { get => m_Height; set => m_Height = value; }

        public bool Equals(Rect other) => m_XMin == other.m_XMin && m_YMin == other.m_YMin && m_Width == other.m_Width && m_Height == other.m_Height;
        public override bool Equals(object? obj) => obj is Rect other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_XMin, m_YMin, m_Width, m_Height);
        public static bool operator ==(Rect left, Rect right) => left.Equals(right);
        public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
    }

    public enum WrapMode
    {
        Once = 1,
        Loop = 2,
        PingPong = 4,
        Default = 0,
        ClampForever = 8,
        Clamp = 1,
    }

    public struct Keyframe : IEquatable<Keyframe>
    {
        float m_Time;
        float m_Value;
        float m_InTangent;
        float m_OutTangent;

        public Keyframe(float time, float value)
            : this(time, value, 0f, 0f)
        {
        }

        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            m_Time = time;
            m_Value = value;
            m_InTangent = inTangent;
            m_OutTangent = outTangent;
        }

        public float time { get => m_Time; set => m_Time = value; }
        public float value { get => m_Value; set => m_Value = value; }
        public float inTangent { get => m_InTangent; set => m_InTangent = value; }
        public float outTangent { get => m_OutTangent; set => m_OutTangent = value; }

        public bool Equals(Keyframe other) => m_Time == other.m_Time && m_Value == other.m_Value && m_InTangent == other.m_InTangent && m_OutTangent == other.m_OutTangent;
        public override bool Equals(object? obj) => obj is Keyframe other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_Time, m_Value, m_InTangent, m_OutTangent);
        public static bool operator ==(Keyframe left, Keyframe right) => left.Equals(right);
        public static bool operator !=(Keyframe left, Keyframe right) => !left.Equals(right);
    }

    public sealed class AnimationCurve
    {
        Keyframe[] m_Keys;

        public AnimationCurve()
            : this(Array.Empty<Keyframe>())
        {
        }

        public AnimationCurve(params Keyframe[] keys)
        {
            m_Keys = keys;
        }

        /// <summary>A copy of the keys, as in Unity.</summary>
        public Keyframe[] keys
        {
            get => (Keyframe[])m_Keys.Clone();
            set => m_Keys = value;
        }

        public int length => m_Keys.Length;
        public WrapMode preWrapMode { get; set; }
        public WrapMode postWrapMode { get; set; }
    }

    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        public float m00;
        public float m10;
        public float m20;
        public float m30;
        public float m01;
        public float m11;
        public float m21;
        public float m31;
        public float m02;
        public float m12;
        public float m22;
        public float m32;
        public float m03;
        public float m13;
        public float m23;
        public float m33;

        public static Matrix4x4 identity
        {
            get
            {
                var m = default(Matrix4x4);
                m.m00 = 1f;
                m.m11 = 1f;
                m.m22 = 1f;
                m.m33 = 1f;
                return m;
            }
        }

        public bool Equals(Matrix4x4 o) =>
            m00 == o.m00 && m10 == o.m10 && m20 == o.m20 && m30 == o.m30 &&
            m01 == o.m01 && m11 == o.m11 && m21 == o.m21 && m31 == o.m31 &&
            m02 == o.m02 && m12 == o.m12 && m22 == o.m22 && m32 == o.m32 &&
            m03 == o.m03 && m13 == o.m13 && m23 == o.m23 && m33 == o.m33;
        public override bool Equals(object? obj) => obj is Matrix4x4 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HashCode.Combine(m00, m10, m20, m30, m01, m11, m21, m31), HashCode.Combine(m02, m12, m22, m32, m03, m13, m23, m33));
        public static bool operator ==(Matrix4x4 left, Matrix4x4 right) => left.Equals(right);
        public static bool operator !=(Matrix4x4 left, Matrix4x4 right) => !left.Equals(right);
    }

    public enum GradientMode
    {
        Blend = 0,
        Fixed = 1,
        PerceptualBlend = 2,
    }

    public struct GradientColorKey : IEquatable<GradientColorKey>
    {
        public Color color;
        public float time;

        public GradientColorKey(Color col, float time)
        {
            color = col;
            this.time = time;
        }

        public bool Equals(GradientColorKey other) => color == other.color && time == other.time;
        public override bool Equals(object? obj) => obj is GradientColorKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(color, time);
        public static bool operator ==(GradientColorKey left, GradientColorKey right) => left.Equals(right);
        public static bool operator !=(GradientColorKey left, GradientColorKey right) => !left.Equals(right);
    }

    public struct GradientAlphaKey : IEquatable<GradientAlphaKey>
    {
        public float alpha;
        public float time;

        public GradientAlphaKey(float alpha, float time)
        {
            this.alpha = alpha;
            this.time = time;
        }

        public bool Equals(GradientAlphaKey other) => alpha == other.alpha && time == other.time;
        public override bool Equals(object? obj) => obj is GradientAlphaKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(alpha, time);
        public static bool operator ==(GradientAlphaKey left, GradientAlphaKey right) => left.Equals(right);
        public static bool operator !=(GradientAlphaKey left, GradientAlphaKey right) => !left.Equals(right);
    }

    public sealed class Gradient
    {
        public GradientColorKey[] colorKeys { get; set; } = Array.Empty<GradientColorKey>();
        public GradientAlphaKey[] alphaKeys { get; set; } = Array.Empty<GradientAlphaKey>();
        public GradientMode mode { get; set; }
    }

    public sealed class RectOffset
    {
        public RectOffset()
        {
        }

        public RectOffset(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }

    public struct LayerMask : IEquatable<LayerMask>
    {
        public int value { get; set; }

        public static implicit operator int(LayerMask mask) => mask.value;
        public static implicit operator LayerMask(int intVal) => new LayerMask { value = intVal };

        public bool Equals(LayerMask other) => value == other.value;
        public override bool Equals(object? obj) => obj is LayerMask other && Equals(other);
        public override int GetHashCode() => value;
        public static bool operator ==(LayerMask left, LayerMask right) => left.Equals(right);
        public static bool operator !=(LayerMask left, LayerMask right) => !left.Equals(right);
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        int m_X;
        int m_Y;

        public Vector2Int(int x, int y)
        {
            m_X = x;
            m_Y = y;
        }

        public int x { get => m_X; set => m_X = value; }
        public int y { get => m_Y; set => m_Y = value; }

        public bool Equals(Vector2Int other) => m_X == other.m_X && m_Y == other.m_Y;
        public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_X, m_Y);
        public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);
        public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);
    }

    public struct Vector3Int : IEquatable<Vector3Int>
    {
        int m_X;
        int m_Y;
        int m_Z;

        public Vector3Int(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        public int x { get => m_X; set => m_X = value; }
        public int y { get => m_Y; set => m_Y = value; }
        public int z { get => m_Z; set => m_Z = value; }

        public bool Equals(Vector3Int other) => m_X == other.m_X && m_Y == other.m_Y && m_Z == other.m_Z;
        public override bool Equals(object? obj) => obj is Vector3Int other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_X, m_Y, m_Z);
        public static bool operator ==(Vector3Int left, Vector3Int right) => left.Equals(right);
        public static bool operator !=(Vector3Int left, Vector3Int right) => !left.Equals(right);
    }

    public struct RangeInt : IEquatable<RangeInt>
    {
        public int start;
        public int length;

        public RangeInt(int start, int length)
        {
            this.start = start;
            this.length = length;
        }

        public bool Equals(RangeInt other) => start == other.start && length == other.length;
        public override bool Equals(object? obj) => obj is RangeInt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(start, length);
        public static bool operator ==(RangeInt left, RangeInt right) => left.Equals(right);
        public static bool operator !=(RangeInt left, RangeInt right) => !left.Equals(right);
    }

    public struct RectInt : IEquatable<RectInt>
    {
        int m_XMin;
        int m_YMin;
        int m_Width;
        int m_Height;

        public RectInt(int xMin, int yMin, int width, int height)
        {
            m_XMin = xMin;
            m_YMin = yMin;
            m_Width = width;
            m_Height = height;
        }

        public int x { get => m_XMin; set => m_XMin = value; }
        public int y { get => m_YMin; set => m_YMin = value; }
        public int width { get => m_Width; set => m_Width = value; }
        public int height { get => m_Height; set => m_Height = value; }

        public bool Equals(RectInt other) => m_XMin == other.m_XMin && m_YMin == other.m_YMin && m_Width == other.m_Width && m_Height == other.m_Height;
        public override bool Equals(object? obj) => obj is RectInt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_XMin, m_YMin, m_Width, m_Height);
        public static bool operator ==(RectInt left, RectInt right) => left.Equals(right);
        public static bool operator !=(RectInt left, RectInt right) => !left.Equals(right);
    }

    public struct BoundsInt : IEquatable<BoundsInt>
    {
        Vector3Int m_Position;
        Vector3Int m_Size;

        public BoundsInt(Vector3Int position, Vector3Int size)
        {
            m_Position = position;
            m_Size = size;
        }

        public Vector3Int position { get => m_Position; set => m_Position = value; }
        public Vector3Int size { get => m_Size; set => m_Size = value; }

        public bool Equals(BoundsInt other) => m_Position == other.m_Position && m_Size == other.m_Size;
        public override bool Equals(object? obj) => obj is BoundsInt other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(m_Position, m_Size);
        public static bool operator ==(BoundsInt left, BoundsInt right) => left.Equals(right);
        public static bool operator !=(BoundsInt left, BoundsInt right) => !left.Equals(right);
    }
}
