// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack
{
    /// <summary>
    /// An exception thrown during serializing an object graph or deserializing a messagepack sequence.
    /// </summary>
    [Serializable]
#if MESSAGEPACK_INTERNAL
    internal
#else
    public
#endif
    class MessagePackSerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
        /// </summary>
        public MessagePackSerializationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MessagePackSerializationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public MessagePackSerializationException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializationException"/> class.
        /// </summary>
        /// <param name="info">Serialization info.</param>
        /// <param name="context">Serialization context.</param>
        protected MessagePackSerializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }
}
