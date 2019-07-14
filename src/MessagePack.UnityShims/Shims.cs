// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;

#pragma warning disable SA1307 // Field should begin with upper-case letter
#pragma warning disable SA1300 // Field should begin with upper-case letter
#pragma warning disable IDE1006 // Field should begin with upper-case letter
#pragma warning disable SA1649 // type name matches file name

namespace UnityEngine
{
    [MessagePackObject]
    public struct Vector2
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;

        [SerializationConstructor]
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [MessagePackObject]
    public struct Vector3
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;

        [SerializationConstructor]
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }
    }

    [MessagePackObject]
    public struct Vector4
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;
        [Key(2)]
        public float z;
        [Key(3)]
        public float w;

        [SerializationConstructor]
        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
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

        [SerializationConstructor]
        public Quaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    [MessagePackObject]
    public struct Color
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
    }

    [MessagePackObject]
    public struct Bounds
    {
        [Key(0)]
        public Vector3 center { get; set; }

        [IgnoreMember]
        public Vector3 extents { get; set; }

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
        public float x { get; set; }

        [Key(1)]
        public float y { get; set; }

        [Key(2)]
        public float width { get; set; }

        [Key(3)]
        public float height { get; set; }

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
        public Keyframe[] keys { get; set; }

        [IgnoreMember]
        public int length
        {
            get { return this.keys.Length; }
        }

        [Key(1)]
        public WrapMode postWrapMode { get; set; }

        [Key(2)]
        public WrapMode preWrapMode { get; set; }
    }

    [MessagePackObject]
    public struct Keyframe
    {
        [Key(0)]
        public float time { get; set; }

        [Key(1)]
        public float value { get; set; }

        [Key(2)]
        public float inTangent { get; set; }

        [Key(3)]
        public float outTangent { get; set; }

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
        public GradientColorKey[] colorKeys { get; set; }

        [Key(1)]
        public GradientAlphaKey[] alphaKeys { get; set; }

        [Key(2)]
        public GradientMode mode { get; set; }
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
        public int left { get; set; }

        [Key(1)]
        public int right { get; set; }

        [Key(2)]
        public int top { get; set; }

        [Key(3)]
        public int bottom { get; set; }

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
        public int value { get; set; }
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
        public int x { get; set; }

        [Key(1)]
        public int y { get; set; }

        [Key(2)]
        public int width { get; set; }

        [Key(3)]
        public int height { get; set; }

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
        public Vector3Int position { get; set; }

        [Key(1)]
        public Vector3Int size { get; set; }

        [SerializationConstructor]
        public BoundsInt(Vector3Int position, Vector3Int size)
        {
            this.position = position;
            this.size = size;
        }
    }
}
