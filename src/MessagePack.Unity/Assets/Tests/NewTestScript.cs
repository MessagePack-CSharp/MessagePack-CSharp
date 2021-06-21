using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class NewTestScript
{
    ArrayBufferWriter<byte> bufferWriter = default;
    const int loopCount = 100;
    [SetUp]
    public void Init()
    {
        bufferWriter = new ArrayBufferWriter<byte>(1024);
    }

    [Test]
    public void MessagePackV2()
    {
        for (int j = 0; j < loopCount; j++)
        {
            var writer = new MessagePack.MessagePackWriter(bufferWriter);
            writer.WriteArrayHeader(10);
            for (int i = 0; i < 10; i++)
            {
                writer.WriteInt32(1000);
                writer.WriteInt32(2000);
                writer.WriteInt32(3000);
                writer.WriteInt32(4000);
            }
            writer.Flush();
            var xs = bufferWriter.WrittenSpan.ToArray();
            bufferWriter.Clear();
        }
    }

    [Test]
    public void MessagePackV3_Array()
    {
        for (int j = 0; j < loopCount; j++)
        {
            var writer = new MessagePackv3.MessagePackWriter(bufferWriter, true);
            writer.WriteArrayHeader(10);
            for (int i = 0; i < 10; i++)
            {
                writer.WriteInt32(1000);
                writer.WriteInt32(2000);
                writer.WriteInt32(3000);
                writer.WriteInt32(4000);
            }
            writer.Flush();
            var xs = bufferWriter.WrittenSpan.ToArray();
            bufferWriter.Clear();
            //return xs;
        }
    }

    [Test]
    public void MessagePackV3_Span()
    {
        for (int j = 0; j < loopCount; j++)
        {
            var writer = new MessagePackv3.MessagePackWriter(bufferWriter, false);
            writer.WriteArrayHeader(10);
            for (int i = 0; i < 10; i++)
            {
                writer.WriteInt32(1000);
                writer.WriteInt32(2000);
                writer.WriteInt32(3000);
                writer.WriteInt32(4000);
            }
            writer.Flush();
            var xs = bufferWriter.WrittenSpan.ToArray();
            bufferWriter.Clear();
            //return xs;
        }
    }
}
