// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// V3COMPAT-EDIT: rewritten to the v4 buffer-generic shape; the scenario is unchanged -
// the build must tolerate a formatter the generator cannot access
using SerializerFoundation;

internal class InaccessibleFormatter
{
    /// <summary>
    /// A private, nested class so the resolver cannot access it.
    /// This class verifies that the build isn't broken by the presence of this formatter.
    /// </summary>
    private class MyFormatter<TWriteBuffer, TReadBuffer> : MessagePack.IMessagePackFormatter<TWriteBuffer, TReadBuffer, int>
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        public void Initialize(MessagePackFormatterResolver resolver) => throw new NotImplementedException();

        public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, int value) => throw new NotImplementedException();

        public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref int value) => throw new NotImplementedException();
    }
}
