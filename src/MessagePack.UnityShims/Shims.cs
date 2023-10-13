// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack;

#pragma warning disable SA1307 // Field should begin with upper-case letter
#pragma warning disable SA1300 // Field should begin with upper-case letter
#pragma warning disable IDE1006 // Field should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private (we need fields rather than auto-properties for .NET Native compilation to work).
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // type name matches file name

namespace UnityEngine
{
    [MessagePackObject]
    public struct Vector2 : IEquatable<Vector2>
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;

        private static readonly Vector2 ZeroVector = new(0.0f, 0.0f);
        private static readonly Vector2 OneVector = new(1f, 1f);
        private static readonly Vector2 UpVector = new(0.0f, 1f);
        private static readonly Vector2 DownVector = new(0.0f, -1f);
        private static readonly Vector2 LeftVector = new(-1f, 0.0f);
        private static readonly Vector2 RightVector = new(1f, 0.0f);

        [SerializationConstructor]
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object other) => other is Vector2 other1 && this.Equals(other1);

        public bool Equals(Vector2 other) => this.x == (double)other.x && this.y == (double)other.y;

        public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2;

        public static Vector2 zero = ZeroVector;

        public static Vector2 one => OneVector;

        public static Vector2 up => UpVector;

        public static Vector2 down => DownVector;

        public static Vector2 left => LeftVector;

        public static Vector2 right => RightVector;

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);

        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);

        public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.x * b.x, a.y * b.y);

        public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.x / b.x, a.y / b.y);

        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);

        public static Vector2 operator *(Vector2 a, float d) => new(a.x * d, a.y * d);

        public static Vector2 operator *(float d, Vector2 a) => new(a.x * d, a.y * d);

        public static Vector2 operator /(Vector2 a, float d) => new(a.x / d, a.y / d);

        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            return (num1 * (double)num1) + (num2 * (double)num2) < 9.9999994396249292E-11;
        }

        public static bool operator !=(Vector2 lhs, Vector2 rhs) => !(lhs == rhs);
    }

    [MessagePackObject]
    public struct Vector3 : IEquatable<Vector3>
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;

        private static readonly Vector3 ZeroVector = new(0.0f, 0.0f, 0.0f);
        private static readonly Vector3 OneVector = new(1f, 1f, 1f);
        private static readonly Vector3 UpVector = new(0.0f, 1f, 0.0f);
        private static readonly Vector3 DownVector = new(0.0f, -1f, 0.0f);
        private static readonly Vector3 LeftVector = new(-1f, 0.0f, 0.0f);
        private static readonly Vector3 RightVector = new(1f, 0.0f, 0.0f);
        private static readonly Vector3 ForwardVector = new(0.0f, 0.0f, 1f);
        private static readonly Vector3 BackVector = new(0.0f, 0.0f, -1f);

        [SerializationConstructor]
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override bool Equals(object other) => other is Vector3 other1 && this.Equals(other1);

        public bool Equals(Vector3 other) => this.x == (double)other.x && this.y == (double)other.y && this.z == (double)other.z;

        public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;

        public static Vector3 zero => ZeroVector;

        public static Vector3 one => OneVector;

        public static Vector3 forward => ForwardVector;

        public static Vector3 back => BackVector;

        public static Vector3 up => UpVector;

        public static Vector3 down => DownVector;

        public static Vector3 left => LeftVector;

        public static Vector3 right => RightVector;

        public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.x + b.x, a.y + b.y, a.z + b.z);

        public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.x - b.x, a.y - b.y, a.z - b.z);

        public static Vector3 operator -(Vector3 a) => new(-a.x, -a.y, -a.z);

        public static Vector3 operator *(Vector3 a, float d) => new(a.x * d, a.y * d, a.z * d);

        public static Vector3 operator *(float d, Vector3 a) => new(a.x * d, a.y * d, a.z * d);

        public static Vector3 operator /(Vector3 a, float d) => new(a.x / d, a.y / d, a.z / d);

        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            return (num1 * (double)num1) + (num2 * (double)num2) + (num3 * (double)num3) < 9.9999994396249292E-11;
        }

        public static bool operator !=(Vector3 lhs, Vector3 rhs) => !(lhs == rhs);
    }

    [MessagePackObject]
    public struct Vector4 : IEquatable<Vector4>
    {
        [Key(0)] public float x;
        [Key(1)] public float y;
        [Key(2)] public float z;
        [Key(3)] public float w;

        private static readonly Vector4 ZeroVector = new(0.0f, 0.0f, 0.0f, 0.0f);
        private static readonly Vector4 OneVector = new(1f, 1f, 1f, 1f);

        [SerializationConstructor]
        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public override bool Equals(object other) => other is Vector4 other1 && this.Equals(other1);

        public bool Equals(Vector4 other) => this.x == (double)other.x && this.y == (double)other.y && this.z == (double)other.z && this.w == (double)other.w;

        public override int GetHashCode() => this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2 ^ this.w.GetHashCode() >> 1;

        public static Vector4 zero => ZeroVector;

        public static Vector4 one = OneVector;

        public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);

        public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);

        public static Vector4 operator -(Vector4 a) => new(-a.x, -a.y, -a.z, -a.w);

        public static Vector4 operator *(Vector4 a, float d) => new(a.x * d, a.y * d, a.z * d, a.w * d);

        public static Vector4 operator *(float d, Vector4 a) => new(a.x * d, a.y * d, a.z * d, a.w * d);

        public static Vector4 operator /(Vector4 a, float d) => new(a.x / d, a.y / d, a.z / d, a.w / d);

        public static bool operator ==(Vector4 lhs, Vector4 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            float num4 = lhs.w - rhs.w;
            return (num1 * (double)num1) + (num2 * (double)num2) + (num3 * (double)num3) +
                (num4 * (double)num4) < 9.9999994396249292E-11;
        }

        public static bool operator !=(Vector4 lhs, Vector4 rhs) => !(lhs == rhs);
    }

    [MessagePackObject]
    public struct Quaternion
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;
        [Key(3)]
        public float w;

        private static readonly Quaternion IdentityQuaternion = new(0.0f, 0.0f, 0.0f, 1f);

        [SerializationConstructor]
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static Quaternion identity => IdentityQuaternion;
    }

    [MessagePackObject]
    public struct Color : IEquatable<Color>
    {
        [Key(0)]
        public float r;
        [Key(1)]
        public float g;
        [Key(2)]
        public float b;
        [Key(3)]
        public float a;

        public Color(float r, float g, float b)
            : this(r, g, b, 1.0f)
        {
        }

        [SerializationConstructor]
        public Color(float r, float g, float b, float a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public override bool Equals(object other) => other is Color other1 && this.Equals(other1);

        public override int GetHashCode() => this.r.GetHashCode() ^ this.g.GetHashCode() << 2 ^ this.b.GetHashCode() >> 2 ^ this.a.GetHashCode() >> 1;

        public bool Equals(Color other) => this.r.Equals(other.r) && this.g.Equals(other.g) && this.b.Equals(other.b) && this.a.Equals(other.a);

        public static Color operator +(Color a, Color b) => new(a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a);

        public static Color operator -(Color a, Color b) => new(a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a);

        public static Color operator *(Color a, Color b) => new(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);

        public static Color operator *(Color a, float b) => new(a.r * b, a.g * b, a.b * b, a.a * b);

        public static Color operator *(float b, Color a) => new(a.r * b, a.g * b, a.b * b, a.a * b);

        public static Color operator /(Color a, float b) => new(a.r / b, a.g / b, a.b / b, a.a / b);

        public static bool operator ==(Color lhs, Color rhs) => (Vector4)lhs == (Vector4)rhs;

        public static bool operator !=(Color lhs, Color rhs) => !(lhs == rhs);

        public static Color red => new(1f, 0.0f, 0.0f, 1f);

        public static Color green => new(0.0f, 1f, 0.0f, 1f);

        public static Color blue => new(0.0f, 0.0f, 1f, 1f);

        public static Color white => new(1f, 1f, 1f, 1f);

        public static Color black => new(0.0f, 0.0f, 0.0f, 1f);

        public static Color yellow => new(1f, 0.921568632f, 0.0156862754f, 1f);

        public static Color cyan => new(0.0f, 1f, 1f, 1f);

        public static Color magenta => new(1f, 0.0f, 1f, 1f);

        public static Color gray => new(0.5f, 0.5f, 0.5f, 1f);

        public static Color clear => new(0.0f, 0.0f, 0.0f, 0.0f);

        public static implicit operator Vector4(Color c) => new(c.r, c.g, c.b, c.a);

        public static implicit operator Color(Vector4 v) => new(v.x, v.y, v.z, v.w);
    }

    [MessagePackObject]
    public struct Bounds
    {
        [Key(0)]
        public Vector3 center;

        [IgnoreMember]
        public Vector3 extents;

        [Key(1)]
        public Vector3 size
        {
            get
            {
                return this.extents * 2f;
            }

            set
            {
                this.extents = value * 0.5f;
            }
        }

        [SerializationConstructor]
        public Bounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            this.extents = size * 0.5f;
        }
    }

    [MessagePackObject]
    public struct Rect
    {
        [Key(0)]
        public float x;

        [Key(1)]
        public float y;

        [Key(2)]
        public float width;

        [Key(3)]
        public float height;

        [SerializationConstructor]
        public Rect(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public Rect(Vector2 position, Vector2 size)
        {
            this.x = position.x;
            this.y = position.y;
            this.width = size.x;
            this.height = size.y;
        }

        public Rect(Rect source)
        {
            this.x = source.x;
            this.y = source.y;
            this.width = source.width;
            this.height = source.height;
        }
    }

    // additional from 1.7.3.3
    [MessagePackObject]
    public sealed class AnimationCurve
    {
        [Key(0)]
        public Keyframe[]? keys;

        [IgnoreMember]
        public int length
        {
            get { return this.keys?.Length ?? 0; }
        }

        [Key(1)]
        public WrapMode postWrapMode;

        [Key(2)]
        public WrapMode preWrapMode;
    }

    [MessagePackObject]
    public struct Keyframe
    {
        [Key(0)]
        public float time;

        [Key(1)]
        public float value;

        [Key(2)]
        public float inTangent;

        [Key(3)]
        public float outTangent;

        public Keyframe(float time, float value)
        {
            this.time = time;
            this.value = value;
            this.inTangent = 0f;
            this.outTangent = 0f;
        }

        [SerializationConstructor]
        public Keyframe(float time, float value, float inTangent, float outTangent)
        {
            this.time = time;
            this.value = value;
            this.inTangent = inTangent;
            this.outTangent = outTangent;
        }
    }

    public enum WrapMode
    {
        Once = 1,
        Loop,
        PingPong = 4,
        Default = 0,
        ClampForever = 8,
        Clamp = 1,
    }

    [MessagePackObject]
    public struct Matrix4x4
    {
        [Key(0)]
        public float m00;
        [Key(1)]
        public float m10;
        [Key(2)]
        public float m20;
        [Key(3)]
        public float m30;
        [Key(4)]
        public float m01;
        [Key(5)]
        public float m11;
        [Key(6)]
        public float m21;
        [Key(7)]
        public float m31;
        [Key(8)]
        public float m02;
        [Key(9)]
        public float m12;
        [Key(10)]
        public float m22;
        [Key(11)]
        public float m32;
        [Key(12)]
        public float m03;
        [Key(13)]
        public float m13;
        [Key(14)]
        public float m23;
        [Key(15)]
        public float m33;
    }

    [MessagePackObject]
    public sealed class Gradient
    {
        [Key(0)]
        public GradientColorKey[]? colorKeys;

        [Key(1)]
        public GradientAlphaKey[]? alphaKeys;

        [Key(2)]
        public GradientMode mode;
    }

    [MessagePackObject]
    public struct GradientColorKey
    {
        [Key(0)]
        public Color color;
        [Key(1)]
        public float time;

        public GradientColorKey(Color col, float time)
        {
            this.color = col;
            this.time = time;
        }
    }

    [MessagePackObject]
    public struct GradientAlphaKey
    {
        [Key(0)]
        public float alpha;
        [Key(1)]
        public float time;

        public GradientAlphaKey(float alpha, float time)
        {
            this.alpha = alpha;
            this.time = time;
        }
    }

    public enum GradientMode
    {
        Blend,
        Fixed,
    }

    [MessagePackObject]
    public struct Color32
    {
        [Key(0)]
        public byte r;
        [Key(1)]
        public byte g;
        [Key(2)]
        public byte b;
        [Key(3)]
        public byte a;

        public Color32(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }
    }

    [MessagePackObject]
    public sealed class RectOffset
    {
        [Key(0)]
        public int left;

        [Key(1)]
        public int right;

        [Key(2)]
        public int top;

        [Key(3)]
        public int bottom;

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
    }

    [MessagePackObject]
    public struct LayerMask
    {
        [Key(0)]
        public int value;
    }

    // from Unity2017.2
    [MessagePackObject]
    public struct Vector2Int
    {
        [Key(0)]
        public int x;
        [Key(1)]
        public int y;

        [SerializationConstructor]
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct Vector3Int
    {
        [Key(0)]
        public int x;
        [Key(1)]
        public int y;
        [Key(2)]
        public int z;

        [SerializationConstructor]
        public Vector3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3Int operator *(Vector3Int a, int d)
        {
            return new Vector3Int(a.x * d, a.y * d, a.z * d);
        }
    }

    [MessagePackObject]
    public struct RangeInt
    {
        [Key(0)]
        public int start;
        [Key(1)]
        public int length;

        public RangeInt(int start, int length)
        {
            this.start = start;
            this.length = length;
        }
    }

    [MessagePackObject]
    public struct RectInt
    {
        [Key(0)]
        public int x;

        [Key(1)]
        public int y;

        [Key(2)]
        public int width;

        [Key(3)]
        public int height;

        [SerializationConstructor]
        public RectInt(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public RectInt(Vector2Int position, Vector2Int size)
        {
            this.x = position.x;
            this.y = position.y;
            this.width = size.x;
            this.height = size.y;
        }

        public RectInt(RectInt source)
        {
            this.x = source.x;
            this.y = source.y;
            this.width = source.width;
            this.height = source.height;
        }
    }

    [MessagePackObject]
    public struct BoundsInt
    {
        [Key(0)]
        public Vector3Int position;

        [Key(1)]
        public Vector3Int size;

        [SerializationConstructor]
        public BoundsInt(Vector3Int position, Vector3Int size)
        {
            this.position = position;
            this.size = size;
        }
    }
}

namespace Unity.Mathematics
{
    [MessagePackObject]
    public struct bool2
    {
        [Key(0)]
        public bool x;
        [Key(1)]
        public bool y;

        [SerializationConstructor]
        public bool2(bool x, bool y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct bool3
    {
        [Key(0)]
        public bool x;
        [Key(1)]
        public bool y;
        [Key(2)]
        public bool z;

        [SerializationConstructor]
        public bool3(bool x, bool y, bool z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [MessagePackObject]
    public struct double2
    {
        [Key(0)]
        public double x;
        [Key(1)]
        public double y;

        [SerializationConstructor]
        public double2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct double3
    {
        [Key(0)]
        public double x;
        [Key(1)]
        public double y;
        [Key(2)]
        public double z;

        [SerializationConstructor]
        public double3(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [MessagePackObject]
    public struct float2
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;

        [SerializationConstructor]
        public float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct float3
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;

        [SerializationConstructor]
        public float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    [MessagePackObject]
    public struct int2
    {
        [Key(0)]
        public int x;
        [Key(1)]
        public int y;

        [SerializationConstructor]
        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct int3
    {
        [Key(0)]
        public int x;
        [Key(1)]
        public int y;
        [Key(2)]
        public int z;

        [SerializationConstructor]
        public int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}
