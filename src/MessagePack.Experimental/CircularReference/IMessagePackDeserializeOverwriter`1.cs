// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack
{
    public interface IMessagePackDeserializeOverwriter<T>
        where T : class
    {
        void DeserializeOverwrite(ref MessagePackReader reader, MessagePackSerializerOptions options, T value);
    }
}
