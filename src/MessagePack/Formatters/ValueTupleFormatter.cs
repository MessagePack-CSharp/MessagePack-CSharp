// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/* THIS (.cs) FILE IS GENERATED. DO NOT CHANGE IT.
 * CHANGE THE .tt FILE INSTEAD. */

using System;
using System.Buffers;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters
{
    public sealed class ValueTupleFormatter<T1> : IMessagePackFormatter<ValueTuple<T1>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(1);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
        }

        public ValueTuple<T1> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 1)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);

                    return new ValueTuple<T1>(item1);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2> : IMessagePackFormatter<ValueTuple<T1, T2>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(2);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
        }

        public ValueTuple<T1, T2> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 2)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2>(item1, item2);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3> : IMessagePackFormatter<ValueTuple<T1, T2, T3>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(3);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
        }

        public ValueTuple<T1, T2, T3> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 3)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3>(item1, item2, item3);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(4);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
        }

        public ValueTuple<T1, T2, T3, T4> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 4)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3, T4>(item1, item2, item3, item4);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4, T5>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4, T5> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(5);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
            resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, options);
        }

        public ValueTuple<T1, T2, T3, T4, T5> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 5)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(6);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
            resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, options);
            resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, options);
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 6)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6, T7> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(7);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
            resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, options);
            resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, options);
            resolver.GetFormatterWithVerify<T7>().Serialize(ref writer, value.Item7, options);
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 7)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, options);
                    T7 item7 = resolver.GetFormatterWithVerify<T7>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7, TRest> : IMessagePackFormatter<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
        where TRest : struct
    {
        public void Serialize(ref MessagePackWriter writer, ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> value, MessagePackSerializerOptions options)
        {
            writer.WriteArrayHeader(8);

            IFormatterResolver resolver = options.Resolver;
            resolver.GetFormatterWithVerify<T1>().Serialize(ref writer, value.Item1, options);
            resolver.GetFormatterWithVerify<T2>().Serialize(ref writer, value.Item2, options);
            resolver.GetFormatterWithVerify<T3>().Serialize(ref writer, value.Item3, options);
            resolver.GetFormatterWithVerify<T4>().Serialize(ref writer, value.Item4, options);
            resolver.GetFormatterWithVerify<T5>().Serialize(ref writer, value.Item5, options);
            resolver.GetFormatterWithVerify<T6>().Serialize(ref writer, value.Item6, options);
            resolver.GetFormatterWithVerify<T7>().Serialize(ref writer, value.Item7, options);
            resolver.GetFormatterWithVerify<TRest>().Serialize(ref writer, value.Rest, options);
        }

        public ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                throw new MessagePackSerializationException("Data is Nil, ValueTuple can not be null.");
            }
            else
            {
                var count = reader.ReadArrayHeader();
                if (count != 8)
                {
                    throw new MessagePackSerializationException("Invalid ValueTuple count");
                }

                IFormatterResolver resolver = options.Resolver;
                options.Security.DepthStep(ref reader);
                try
                {
                    T1 item1 = resolver.GetFormatterWithVerify<T1>().Deserialize(ref reader, options);
                    T2 item2 = resolver.GetFormatterWithVerify<T2>().Deserialize(ref reader, options);
                    T3 item3 = resolver.GetFormatterWithVerify<T3>().Deserialize(ref reader, options);
                    T4 item4 = resolver.GetFormatterWithVerify<T4>().Deserialize(ref reader, options);
                    T5 item5 = resolver.GetFormatterWithVerify<T5>().Deserialize(ref reader, options);
                    T6 item6 = resolver.GetFormatterWithVerify<T6>().Deserialize(ref reader, options);
                    T7 item7 = resolver.GetFormatterWithVerify<T7>().Deserialize(ref reader, options);
                    TRest item8 = resolver.GetFormatterWithVerify<TRest>().Deserialize(ref reader, options);

                    return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, item8);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }
}
