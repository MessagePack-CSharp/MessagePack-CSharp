using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using UnityEngine;
using System.Buffers;

public class SandboxBehaviour : MonoBehaviour
{
    void Start()
    {
        // 'あ' is [227, 129, 130]
        var bin = MessagePackSerializer.Serialize(new string('あ', 11));

        // expected: 217, 33, 227, 129, 130, 227, 129, 130, ....
        // actual: 217, 33, 227, 227, 227, 227, 227, 227, 227, 227
        UnityEngine.Debug.Log(string.Join(", ", bin));
        
    }
}

class SimpleBufferWriter : IBufferWriter<byte>
{
    public byte[] buffer = new byte[1024];
    public int index;

    public void Advance(int count)
    {
        index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        return buffer;
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        return buffer;
    }
}
