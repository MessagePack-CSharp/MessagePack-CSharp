using MessagePack;

namespace UnityEngine
{
    [MessagePackObject]
    public struct Vector2
    {
        [Key(0)]
        public float x;
        [Key(1)]
        public float y;

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
}
