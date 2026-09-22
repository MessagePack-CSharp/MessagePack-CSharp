// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Formatters;

internal class InaccessibleFormatter
{
    /// <summary>
    /// A private, nested class so the resolver cannot access it.
    /// This class verifies that the build isn't broken by the presence of this formatter.
    /// </summary>
#pragma warning disable MsgPack010 // This is the warning we expect due to an inaccessible formatter.
    private class MyFormatter : IMessagePackFormatter<int>
#pragma warning restore MsgPack010
    {
        public int Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) => throw new NotImplementedException();

        public void Serialize(ref MessagePackWriter writer, int value, MessagePackSerializerOptions options) => throw new NotImplementedException();
    }
}
