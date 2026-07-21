using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class MessagePackPrimitives
{
    const byte UInt8Code = 0xcc;
    const byte UInt16Code = 0xcd;
    const byte UInt32Code = 0xce;
    const byte UInt64Code = 0xcf;
    const byte Int8Code = 0xd0;
    const byte Int16Code = 0xd1;
    const byte Int32Code = 0xd2;
    const byte Int64Code = 0xd3;

    // Candidate 1 (baseline): straightforward if-else cascade, MessagePackWriter.WriteInt32 style
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32Cascade(Span<byte> destination, int value, out int bytesWritten)
    {
        if (value >= 0)
        {
            if (value <= 0x7f)
            {
                if (destination.Length < 1) goto Fail;
                destination[0] = (byte)value;
                bytesWritten = 1;
                return true;
            }
            if (value <= 0xff)
            {
                if (destination.Length < 2) goto Fail;
                destination[0] = UInt8Code;
                destination[1] = (byte)value;
                bytesWritten = 2;
                return true;
            }
            if (value <= 0xffff)
            {
                if (destination.Length < 3) goto Fail;
                destination[0] = UInt16Code;
                BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(1), (ushort)value);
                bytesWritten = 3;
                return true;
            }
            if (destination.Length < 5) goto Fail;
            destination[0] = UInt32Code;
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(1), (uint)value);
            bytesWritten = 5;
            return true;
        }
        else
        {
            if (value >= -32)
            {
                if (destination.Length < 1) goto Fail;
                destination[0] = unchecked((byte)value);
                bytesWritten = 1;
                return true;
            }
            if (value >= sbyte.MinValue)
            {
                if (destination.Length < 2) goto Fail;
                destination[0] = Int8Code;
                destination[1] = unchecked((byte)value);
                bytesWritten = 2;
                return true;
            }
            if (value >= short.MinValue)
            {
                if (destination.Length < 3) goto Fail;
                destination[0] = Int16Code;
                BinaryPrimitives.WriteInt16BigEndian(destination.Slice(1), (short)value);
                bytesWritten = 3;
                return true;
            }
            if (destination.Length < 5) goto Fail;
            destination[0] = Int32Code;
            BinaryPrimitives.WriteInt32BigEndian(destination.Slice(1), value);
            bytesWritten = 5;
            return true;
        }
    Fail:
        bytesWritten = 0;
        return false;
    }

    // Candidate 2: same branch structure but ref-based unaligned writes (no Slice, merged stores)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32CascadeUnsafe(Span<byte> destination, int value, out int bytesWritten)
    {
        ref byte d = ref MemoryMarshal.GetReference(destination);
        if (value >= 0)
        {
            if (value <= 0x7f)
            {
                if (destination.Length < 1) goto Fail;
                d = (byte)value;
                bytesWritten = 1;
                return true;
            }
            if (value <= 0xff)
            {
                if (destination.Length < 2) goto Fail;
                // header+value as one little-endian 16bit store: d[0]=0xcc, d[1]=value
                Unsafe.WriteUnaligned(ref d, (ushort)(UInt8Code | (value << 8)));
                bytesWritten = 2;
                return true;
            }
            if (value <= 0xffff)
            {
                if (destination.Length < 3) goto Fail;
                d = UInt16Code;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((ushort)value));
                bytesWritten = 3;
                return true;
            }
            if (destination.Length < 5) goto Fail;
            d = UInt32Code;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((uint)value));
            bytesWritten = 5;
            return true;
        }
        else
        {
            if (value >= -32)
            {
                if (destination.Length < 1) goto Fail;
                d = unchecked((byte)value);
                bytesWritten = 1;
                return true;
            }
            if (value >= sbyte.MinValue)
            {
                if (destination.Length < 2) goto Fail;
                Unsafe.WriteUnaligned(ref d, (ushort)(Int8Code | (value << 8)));
                bytesWritten = 2;
                return true;
            }
            if (value >= short.MinValue)
            {
                if (destination.Length < 3) goto Fail;
                d = Int16Code;
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((ushort)value));
                bytesWritten = 3;
                return true;
            }
            if (destination.Length < 5) goto Fail;
            d = Int32Code;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((uint)value));
            bytesWritten = 5;
            return true;
        }
    Fail:
        bytesWritten = 0;
        return false;
    }

    // Candidate 3: single-compare fast path for fixint [-32, 127] (the dominant case in typical
    // msgpack payloads), everything else in a non-inlined slow path
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32FixintFirst(Span<byte> destination, int value, out int bytesWritten)
    {
        if ((uint)(value + 32) <= 159 && destination.Length >= 1)
        {
            MemoryMarshal.GetReference(destination) = unchecked((byte)value);
            bytesWritten = 1;
            return true;
        }
        return TryWriteInt32MultiByte(destination, value, out bytesWritten);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TryWriteInt32MultiByte(Span<byte> destination, int value, out int bytesWritten)
        => TryWriteInt32CascadeUnsafe(destination, value, out bytesWritten);

    // Candidate 4: branchless. Classify by significant-bit count (lzcnt) into a format table,
    // then do one header store + one unconditional 4-byte store.
    // Note: may write scratch bytes beyond bytesWritten (always within destination).
    //
    // packed entry: length | header << 8 | fixint-mask << 16
    const uint Fix1 = 1 | (0x00u << 8) | (0xffu << 16);
    const uint U8 = 2 | ((uint)UInt8Code << 8);
    const uint U16 = 3 | ((uint)UInt16Code << 8);
    const uint U32 = 5 | ((uint)UInt32Code << 8);
    const uint I8 = 2 | ((uint)Int8Code << 8);
    const uint I16 = 3 | ((uint)Int16Code << 8);
    const uint I32 = 5 | ((uint)Int32Code << 8);

    // index = sign * 33 + significantBits(value ^ (value >> 31))
    static ReadOnlySpan<uint> Formats =>
    [
        // non-negative: bits 0..7 fixint, 8 uint8, 9..16 uint16, 17..32 uint32
        Fix1, Fix1, Fix1, Fix1, Fix1, Fix1, Fix1, Fix1,
        U8,
        U16, U16, U16, U16, U16, U16, U16, U16,
        U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32,
        // negative (bits of ~value): 0..5 fixint, 6..7 int8, 8..15 int16, 16..32 int32
        Fix1, Fix1, Fix1, Fix1, Fix1, Fix1,
        I8, I8,
        I16, I16, I16, I16, I16, I16, I16, I16,
        I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32BranchlessTable(Span<byte> destination, int value, out int bytesWritten)
    {
        if (destination.Length >= 5)
        {
            int x = value ^ (value >> 31);
            int bits = 32 - BitOperations.LeadingZeroCount((uint)x);
            int sign = value >>> 31;
            uint e = Formats[sign * 33 + bits];
            int len = (int)(e & 0xff);
            ref byte d = ref MemoryMarshal.GetReference(destination);
            // header | (value & fixint-mask): for len==1 the "header" is the value byte itself
            d = (byte)((e >> 8) | ((uint)value & (e >> 16)));
            // big-endian value bytes start right after the header; left-shift moves the
            // significant bytes to the front (len==1 shifts by 32&31==0, writing scratch)
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((uint)value << ((5 - len) * 8)));
            bytesWritten = len;
            return true;
        }
        // destination < 5: rare cold path, take the careful route
        return TryWriteInt32Cascade(destination, value, out bytesWritten);
    }

    // Round 2: significant bits of a non-negative int are 0..31, never 32, so a 64-entry table
    // indexed by ((value >>> 26) & 32) | bits works — single OR instead of imul, and the
    // power-of-two size lets us drop the bounds check via Unsafe
    static ReadOnlySpan<uint> Formats64 =>
    [
        // non-negative: bits 0..7 fixint, 8 uint8, 9..16 uint16, 17..31 uint32
        Fix1, Fix1, Fix1, Fix1, Fix1, Fix1, Fix1, Fix1,
        U8,
        U16, U16, U16, U16, U16, U16, U16, U16,
        U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32, U32,
        // negative (bits of ~value): 0..5 fixint, 6..7 int8, 8..15 int16, 16..31 int32
        Fix1, Fix1, Fix1, Fix1, Fix1, Fix1,
        I8, I8,
        I16, I16, I16, I16, I16, I16, I16, I16,
        I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32, I32,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32Branchless2(Span<byte> destination, int value, out int bytesWritten)
    {
        if (destination.Length >= 5)
        {
            int x = value ^ (value >> 31);
            int bits = 32 - BitOperations.LeadingZeroCount((uint)x); // 0..31
            int idx = ((value >>> 26) & 32) | bits;
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(Formats64), idx);
            int len = (int)(e & 0xff);
            ref byte d = ref MemoryMarshal.GetReference(destination);
            d = (byte)((e >> 8) | ((uint)value & (e >> 16)));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref d, 1), BinaryPrimitives.ReverseEndianness((uint)value << ((5 - len) * 8)));
            bytesWritten = len;
            return true;
        }
        return TryWriteInt32Cascade(destination, value, out bytesWritten);
    }

    // Round 2: predictable single-compare fixint fast path in front of the branchless body —
    // aims to combine FixintFirst's Small-distribution win with Branchless2's flat profile
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryWriteInt32Hybrid(Span<byte> destination, int value, out int bytesWritten)
    {
        if ((uint)(value + 32) <= 159 && destination.Length >= 1)
        {
            MemoryMarshal.GetReference(destination) = unchecked((byte)value);
            bytesWritten = 1;
            return true;
        }
        return TryWriteInt32Branchless2(destination, value, out bytesWritten);
    }

    // ===== TryReadInt32 =====
    // Reader counterpart. Accepts exactly the formats TryWriteInt32 emits:
    // positive/negative fixint, uint8(cc), uint16(cd), uint32(ce), int8(d0), int16(d1), int32(d2).
    // Values that don't fit int32 return false. int64(d3)/uint64(cf) are accepted when the
    // value fits — non-minimal encodings are legal msgpack (schema-typed or fixed-width
    // writers emit them) — handled on a cold path so the hot path stays 5-byte based.
    // Contract: on false, value is unspecified. bytesConsumed == 0 means invalid/truncated
    // input; a well-formed message whose value doesn't fit int32 reports its true length
    // (5 or 9) from the branchless variants so callers may skip it.

    // Candidate 1 (baseline): readable cascade, MessagePackReader style
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt32Cascade(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.IsEmpty) goto Fail;
        byte code = source[0];
        if (code <= 0x7f)
        {
            value = code;
            bytesConsumed = 1;
            return true;
        }
        if (code >= 0xe0)
        {
            value = unchecked((sbyte)code);
            bytesConsumed = 1;
            return true;
        }
        switch (code)
        {
            case UInt8Code:
                if (source.Length < 2) break;
                value = source[1];
                bytesConsumed = 2;
                return true;
            case UInt16Code:
                if (source.Length < 3) break;
                value = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1));
                bytesConsumed = 3;
                return true;
            case UInt32Code:
            {
                if (source.Length < 5) break;
                uint v = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1));
                if (v > int.MaxValue) break;
                value = (int)v;
                bytesConsumed = 5;
                return true;
            }
            case Int8Code:
                if (source.Length < 2) break;
                value = unchecked((sbyte)source[1]);
                bytesConsumed = 2;
                return true;
            case Int16Code:
                if (source.Length < 3) break;
                value = BinaryPrimitives.ReadInt16BigEndian(source.Slice(1));
                bytesConsumed = 3;
                return true;
            case Int32Code:
                if (source.Length < 5) break;
                value = BinaryPrimitives.ReadInt32BigEndian(source.Slice(1));
                bytesConsumed = 5;
                return true;
            case Int64Code:
            case UInt64Code:
                return TryReadInt32Wide(source, out value, out bytesConsumed);
        }
    Fail:
        value = 0;
        bytesConsumed = 0;
        return false;
    }

    // Cold path for the 9-byte formats int64(d3)/uint64(cf). Rare in minimally-encoded
    // streams, so kept out of the hot path (NoInlining keeps callers small).
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool TryReadInt32Wide(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length >= 9)
        {
            byte code = MemoryMarshal.GetReference(source);
            long v = unchecked((long)BinaryPrimitives.ReadUInt64BigEndian(source.Slice(1)));
            value = (int)v;
            bytesConsumed = 9;
            // int64: fits-in-int32 check. uint64: additionally require non-negative as long —
            // reinterpreting 0xFFFF_FFFF_FFFF_FFFF as -1 would slip through the fit check alone
            return ((int)v == v) & ((code == Int64Code) | (v >= 0));
        }
        value = 0;
        bytesConsumed = 0;
        return false;
    }

    // Candidate 2: branchless. 256-entry table indexed by the format code gives
    // {length, payload shift, sign-extension shift, take-value-from-code flag}.
    // The 4 payload bytes are read unconditionally (source >= 5 on the fast path),
    // then shifted/sign-extended arithmetically — no data-dependent branches.
    //
    // entry: len(bits 0..3) | rawShift(bits 4..9) | ext(bits 10..15) | codeMask(bits 16..23)
    static readonly uint[] ReadTable = BuildReadTable();

    static uint[] BuildReadTable()
    {
        static uint Pack(int len, int rawShift, int ext, int codeMask)
            => (uint)(len | (rawShift << 4) | (ext << 10) | (codeMask << 16));

        var t = new uint[256]; // len 0 = invalid
        for (int c = 0; c <= 0x7f; c++) t[c] = Pack(1, 32, 56, 0xff);  // positive fixint
        for (int c = 0xe0; c <= 0xff; c++) t[c] = Pack(1, 32, 56, 0xff); // negative fixint
        t[UInt8Code] = Pack(2, 24, 0, 0);
        t[UInt16Code] = Pack(3, 16, 0, 0);
        t[UInt32Code] = Pack(5, 0, 0, 0);   // overflow rejected via (int)v == v
        t[Int8Code] = Pack(2, 24, 56, 0);
        t[Int16Code] = Pack(3, 16, 48, 0);
        t[Int32Code] = Pack(5, 0, 32, 0);
        t[Int64Code] = Pack(9, 0, 0, 0);    // len 9 marks the wide cold path
        t[UInt64Code] = Pack(9, 0, 0, 0);
        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt32Branchless(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint e = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(ReadTable), code);
            if ((e & 0xf) == 9) return TryReadInt32Wide(source, out value, out bytesConsumed);
            // payload as big-endian u32, zero-extended to 64bit so a 32 shift really yields 0
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            // right-align the payload bytes (fixint shifts by 32 -> 0), or take the code byte itself
            ulong sel = (p >> (int)((e >> 4) & 0x3f)) | (ulong)(code & (e >> 16));
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            bool ok = (len != 0) & (v == value); // non-shortcircuit: stays branchless
            bytesConsumed = ok ? len : 0;
            return ok;
        }
        // source < 5: rare cold path (end of buffer), take the careful route
        return TryReadInt32Cascade(source, out value, out bytesConsumed);
    }

    // Candidate 3: single-compare fixint fast path (code 0x00-0x7f / 0xe0-0xff) + branchless rest
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt32Hybrid(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (!source.IsEmpty)
        {
            byte code = MemoryMarshal.GetReference(source);
            if ((byte)(code + 0x20) <= 0x9f) // wraps 0xe0-0xff to 0x00-0x1f
            {
                value = unchecked((sbyte)code);
                bytesConsumed = 1;
                return true;
            }
        }
        return TryReadInt32Branchless(source, out value, out bytesConsumed);
    }

    // Round 2: same logic as Branchless, but the table is a compile-time constant RVA span.
    // The runtime-built static uint[] cost a "static base load -> array ref load -> element load"
    // chain in the hot loop (visible in asm); RVA data is a single load from a constant address.
    // Same packing as BuildReadTable: len | rawShift<<4 | ext<<10 | codeMask<<16
    const uint RFx = 0x00FFE201; // fixint: len1, rawShift32, ext56, take value from code byte
    const uint RU1 = 0x00000182; // uint8:  len2, rawShift24
    const uint RU2 = 0x00000103; // uint16: len3, rawShift16
    const uint RU4 = 0x00000005; // uint32: len5
    const uint RI1 = 0x0000E182; // int8:   len2, rawShift24, ext56
    const uint RI2 = 0x0000C103; // int16:  len3, rawShift16, ext48
    const uint RI4 = 0x00008005; // int32:  len5, ext32
    const uint RW8 = 0x00000009; // int64/uint64: len 9 marks the wide cold path
    const uint ___ = 0;          // invalid

    static ReadOnlySpan<uint> ReadTableRva =>
    [
        // 0x00-0x7f: positive fixint
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        // 0x80-0xbf: fixmap/fixarray/fixstr — invalid here
        ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___,
        ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___,
        ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___,
        ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___,
        // 0xc0-0xcf: nil..float64, uint8(cc)/uint16(cd)/uint32(ce)/uint64(cf: wide cold path)
        ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, RU1, RU2, RU4, RW8,
        // 0xd0-0xdf: int8(d0)/int16(d1)/int32(d2)/int64(d3: wide cold path); str/ext invalid
        RI1, RI2, RI4, RW8, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___, ___,
        // 0xe0-0xff: negative fixint
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
        RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx, RFx,
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt32Branchless2(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ReadTableRva), code);
            if ((e & 0xf) == 9) return TryReadInt32Wide(source, out value, out bytesConsumed);
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            ulong sel = (p >> (int)((e >> 4) & 0x3f)) | (ulong)(code & (e >> 16));
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            // bytesConsumed = len directly: invalid codes already have len 0 in the table,
            // and a well-formed uint32 that doesn't fit int32 reports its true length (5)
            // with a false return. Gating on the ok flag here would tie the loop-carried
            // offset chain to the whole value computation if the JIT chose cmov over a branch.
            bytesConsumed = len;
            return (len != 0) & (v == value);
        }
        return TryReadInt32Cascade(source, out value, out bytesConsumed);
    }

    // Round 3: in a decode loop the only loop-carried dependency is bytesConsumed
    // (it determines the next message address). Branchless2 sources len from the format
    // table, keeping a second dependent load on that chain. Here len and validity come
    // from a few ALU ops on the code byte instead (load+load -> load+alu):
    //   cc/cd/ce and d0/d1/d2: payload width is 1 << (code & 3), so len = 1 + (1 << (code & 3))
    //   fixint: len = 1, everything else: len = 0
    // ext/codeMask still come from the table — the value computation is per-message work
    // that overlaps across iterations, so a load there is off the critical path.
    // Contract change: when a well-formed uint32 doesn't fit int32, this returns false
    // with bytesConsumed = 5 (message length); bytesConsumed = 0 means invalid/truncated.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryReadInt32Branchless3(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            int code = s;
            if (code == Int64Code || code == UInt64Code) return TryReadInt32Wide(source, out value, out bytesConsumed);
            // all-ones when 0x00-0x7f / 0xe0-0xff (fixint)
            int fixMask = ((((code + 0x20) & 0xff) - 0xa0) >> 31);
            // all-ones when 0xcc-0xce / 0xd0-0xd2
            int multiMask = ((((code - 0xcc) & 0xff) - 3) >> 31) | ((((code - 0xd0) & 0xff) - 3) >> 31);
            int len = (fixMask & 1) | (multiMask & (1 + (1 << (code & 3))));
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ReadTableRva), code);
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            ulong sel = (p >> (int)((e >> 4) & 0x3f)) | (ulong)((uint)code & (e >> 16));
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            value = (int)v;
            bytesConsumed = len;
            return (len != 0) & (v == value);
        }
        return TryReadInt32Cascade(source, out value, out bytesConsumed);
    }

    // ===== Round 4: DecodeResult (enum) contract =====
    // (Measurement record: this 4-category candidate proved the enum contract zero-cost.
    // The library then adopted a 3-category version — ValueOutOfRange folded into
    // TokenMismatch, since only InsufficientBuffer ever changes control flow and no Skip
    // consumer of the true-length report exists.)
    // MessagePack-CSharp v3 / Nerdbank style enum return, but designed for zero hot cost:
    // Success = 0 so the caller's check is test eax,eax (same as the bool's test al,al),
    // and the category encoding is ~2 extra ALU ops off the bytesConsumed critical chain.
    // Unlike the bool contract (false + consumed==0 conflates corrupt with incomplete),
    // the enum separates InsufficientBuffer from TokenMismatch, so sequence/async readers
    // can branch on the result instead of the retry-and-see heuristic.
    //   Success            - bytesConsumed = message length
    //   ValueOutOfRange    - well-formed int too wide for int32; bytesConsumed = true
    //                        length so callers may skip (richer than the v3 enum)
    //   TokenMismatch      - not an int token; bytesConsumed = 0
    //   InsufficientBuffer - need more data; bytesConsumed = 0
    public enum DecodeResult
    {
        Success = 0,
        ValueOutOfRange = 1,
        TokenMismatch = 2,
        InsufficientBuffer = 3,
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DecodeResult TryReadInt32Dr(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length >= 5)
        {
            ref byte s = ref MemoryMarshal.GetReference(source);
            byte code = s;
            uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ReadTableRva), code);
            if ((e & 0xf) == 9) return TryReadInt32DrWide(source, out value, out bytesConsumed);
            ulong p = BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref s, 1)));
            ulong sel = (p >> (int)((e >> 4) & 0x3f)) | (ulong)(code & (e >> 16));
            int ext = (int)((e >> 10) & 0x3f);
            long v = ((long)sel << ext) >> ext;
            int len = (int)(e & 0xf);
            value = (int)v;
            bytesConsumed = len;
            // category: invalid(len==0) -> TokenMismatch(2); else fit failure -> OutOfRange(1);
            // else Success(0). notFit must be masked out when invalid (v is garbage there).
            int notFit = v != value ? 1 : 0;
            int invalid = len == 0 ? 2 : 0;
            return (DecodeResult)(invalid | (notFit & (1 - (invalid >> 1))));
        }
        return TryReadInt32DrCareful(source, out value, out bytesConsumed);
    }

    // cold: int64(d3)/uint64(cf) wide encodings
    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult TryReadInt32DrWide(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (source.Length < 9)
        {
            value = 0;
            bytesConsumed = 0;
            return DecodeResult.InsufficientBuffer;
        }
        byte code = MemoryMarshal.GetReference(source);
        long v = unchecked((long)BinaryPrimitives.ReadUInt64BigEndian(source.Slice(1)));
        value = (int)v;
        bytesConsumed = 9;
        // uint64 reinterpreted negative (>= 2^63) never fits; otherwise int32 range check
        bool wellFormedFit = ((code == Int64Code) | (v >= 0)) & (v == value);
        return wellFormedFit ? DecodeResult.Success : DecodeResult.ValueOutOfRange;
    }

    // cold: source shorter than the 5-byte unconditional-load window; also the only place
    // that must tell truncated (InsufficientBuffer) apart from invalid (TokenMismatch)
    [MethodImpl(MethodImplOptions.NoInlining)]
    static DecodeResult TryReadInt32DrCareful(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        value = 0;
        bytesConsumed = 0;
        if (source.IsEmpty) return DecodeResult.InsufficientBuffer;

        int code = source[0];
        uint e = Unsafe.Add(ref MemoryMarshal.GetReference(ReadTableRva), code);
        if (e == 0) return DecodeResult.TokenMismatch;

        int len = (int)(e & 0xf);
        if (source.Length < len) return DecodeResult.InsufficientBuffer;

        long v = len switch
        {
            1 => unchecked((sbyte)code), // fixint: the code byte is the value
            2 => code == 0xcc ? source[1] : unchecked((sbyte)source[1]),
            3 => code == 0xcd ? BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1)) : BinaryPrimitives.ReadInt16BigEndian(source.Slice(1)),
            5 => code == 0xce ? BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1)) : BinaryPrimitives.ReadInt32BigEndian(source.Slice(1)),
            _ => unchecked((long)BinaryPrimitives.ReadUInt64BigEndian(source.Slice(1))), // 9: wide
        };
        value = (int)v;
        bytesConsumed = len;
        bool fits = len == 9
            ? ((code == Int64Code) | (v >= 0)) & (v == value)
            : v == value;
        return fits ? DecodeResult.Success : DecodeResult.ValueOutOfRange;
    }

    // ===== Round 5: rematch — branch speculation vs branchless =====
    // TryReadInt32CompareBenchmark showed the ecosystem cascades at 0.82-1.0ns on
    // class-homogeneous streams (a predicted class branch lets the CPU SPECULATE
    // bytesConsumed, breaking the consumed->next-address serial chain) vs the branchless
    // table's flat 2.57ns, while the table wins 3.5x+ on Mixed. These candidates return
    // the LIBRARY's 3-value DecodeResult so they are drop-in shapes for the real
    // MessagePackPrimitives.Read if one dethrones the table.

    // MessagePack-CSharp-style compare cascade under our contract: every path's consumed
    // is decided by a branch, so a predicted stream runs speculatively
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UltraMessagePack.DecodeResult TryReadInt32CascadeDr(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        value = 0;
        bytesConsumed = 0;
        if (source.IsEmpty) return UltraMessagePack.DecodeResult.InsufficientBuffer;
        byte code = source[0];
        if (code <= 0x7f) // positive fixint
        {
            value = code;
            bytesConsumed = 1;
            return UltraMessagePack.DecodeResult.Success;
        }
        if (code >= 0xe0) // negative fixint
        {
            value = unchecked((sbyte)code);
            bytesConsumed = 1;
            return UltraMessagePack.DecodeResult.Success;
        }
        switch (code)
        {
            case UInt8Code:
                if (source.Length < 2) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                value = source[1];
                bytesConsumed = 2;
                return UltraMessagePack.DecodeResult.Success;
            case UInt16Code:
                if (source.Length < 3) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                value = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(1));
                bytesConsumed = 3;
                return UltraMessagePack.DecodeResult.Success;
            case UInt32Code:
            {
                if (source.Length < 5) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                uint v = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(1));
                if (v > int.MaxValue) return UltraMessagePack.DecodeResult.TokenMismatch;
                value = unchecked((int)v);
                bytesConsumed = 5;
                return UltraMessagePack.DecodeResult.Success;
            }
            case Int8Code:
                if (source.Length < 2) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                value = unchecked((sbyte)source[1]);
                bytesConsumed = 2;
                return UltraMessagePack.DecodeResult.Success;
            case Int16Code:
                if (source.Length < 3) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                value = BinaryPrimitives.ReadInt16BigEndian(source.Slice(1));
                bytesConsumed = 3;
                return UltraMessagePack.DecodeResult.Success;
            case Int32Code:
                if (source.Length < 5) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                value = BinaryPrimitives.ReadInt32BigEndian(source.Slice(1));
                bytesConsumed = 5;
                return UltraMessagePack.DecodeResult.Success;
            case Int64Code:
            {
                if (source.Length < 9) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                long v = BinaryPrimitives.ReadInt64BigEndian(source.Slice(1));
                value = unchecked((int)v);
                if (value != v) { value = 0; return UltraMessagePack.DecodeResult.TokenMismatch; }
                bytesConsumed = 9;
                return UltraMessagePack.DecodeResult.Success;
            }
            case UInt64Code:
            {
                if (source.Length < 9) return UltraMessagePack.DecodeResult.InsufficientBuffer;
                ulong v = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(1));
                if (v > int.MaxValue) return UltraMessagePack.DecodeResult.TokenMismatch;
                value = unchecked((int)v);
                bytesConsumed = 9;
                return UltraMessagePack.DecodeResult.Success;
            }
            default:
                return UltraMessagePack.DecodeResult.TokenMismatch;
        }
    }

    // fixint-first branch (the single dominant class in real data), branchless table for
    // the rest: speculation where it is cheap, immunity where it matters
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UltraMessagePack.DecodeResult TryReadInt32HybridDr(ReadOnlySpan<byte> source, out int value, out int bytesConsumed)
    {
        if (!source.IsEmpty)
        {
            byte code = source[0];
            if ((byte)(code + 0x20) <= 0x9f) // wraps 0xe0-0xff to 0x00-0x1f
            {
                value = unchecked((sbyte)code);
                bytesConsumed = 1;
                return UltraMessagePack.DecodeResult.Success;
            }
        }
        return UltraMessagePack.MessagePackPrimitives.TryReadInt32(source, out value, out bytesConsumed);
    }

    // ===== Round 6: TryReadBoolean — naive switch vs biased compare =====
    // The library version derives the VALUE branchlessly (one biased compare validates
    // 0xc2/0xc3, value = d != 0 is a setcc). The naive shape switches on the code byte,
    // i.e. branches on the value itself — hypothesis: identical on homogeneous streams
    // (predictor learns), mispredicts on random bools (~13-20cyc each). tokenSize is a
    // constant 1 in every variant, so unlike int the consumed->next-address chain never
    // differs; the whole contest is value derivation.

    // user-posted naive shape, adapted to the library contract
    // (EmptyBuffer -> InsufficientBuffer, mismatch tokenSize = 0)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UltraMessagePack.DecodeResult TryReadBooleanNaiveSwitch(ReadOnlySpan<byte> source, out bool value, out int tokenSize)
    {
        tokenSize = 1;
        if (source.IsEmpty)
        {
            value = false;
            return UltraMessagePack.DecodeResult.InsufficientBuffer;
        }
        switch (source[0])
        {
            case 0xc3:
                value = true;
                return UltraMessagePack.DecodeResult.Success;
            case 0xc2:
                value = false;
                return UltraMessagePack.DecodeResult.Success;
            default:
                value = false;
                tokenSize = 0;
                return UltraMessagePack.DecodeResult.TokenMismatch;
        }
    }

    // limit probe: zero branches after the empty guard — validity also via setcc/cmov.
    // Trades the library's always-predicted validity branch for a longer dependent chain.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static UltraMessagePack.DecodeResult TryReadBooleanBranchless(ReadOnlySpan<byte> source, out bool value, out int tokenSize)
    {
        if (source.IsEmpty)
        {
            value = false;
            tokenSize = 1;
            return UltraMessagePack.DecodeResult.InsufficientBuffer;
        }
        uint d = (uint)source[0] - 0xc2;
        bool ok = d <= 1;
        value = d == 1;
        tokenSize = ok ? 1 : 0;
        return ok ? UltraMessagePack.DecodeResult.Success : UltraMessagePack.DecodeResult.TokenMismatch;
    }
}
