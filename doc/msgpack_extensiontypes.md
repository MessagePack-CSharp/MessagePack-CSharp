# Reserved Extension Types

MessagePack for C# already used some messagepack ext type codes, be careful to use same ext code.

| Code | Type | Use by |
| ---  | ---  | --- |
| -1 | DateTime | msgpack-spec reserved for timestamp |
| 30 | Vector2[] | for Unity, UnsafeBlitFormatter |
| 31 | Vector3[] | for Unity, UnsafeBlitFormatter |
| 32 | Vector4[] | for Unity, UnsafeBlitFormatter |
| 33 | Quaternion[] | for Unity, UnsafeBlitFormatter |
| 34 | Color[] | for Unity, UnsafeBlitFormatter |
| 35 | Bounds[] | for Unity, UnsafeBlitFormatter |
| 36 | Rect[] | for Unity, UnsafeBlitFormatter |
| 37 | Int[] | for Unity, UnsafeBlitFormatter |
| 38 | Float[] | for Unity, UnsafeBlitFormatter |
| 39 | Double[] | for Unity, UnsafeBlitFormatter |
| 99 | All | LZ4MessagePackSerializer |
| 100 | object | TypelessFormatter |
