using MessagePack.Formatters;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;

namespace MessagePack.ReactivePropertyExtension
{
    public static class ReactivePropertySchedulerMapper
    {
#pragma warning disable CS0618

        static Dictionary<int, IScheduler> mapTo = new Dictionary<int, IScheduler>();
        static Dictionary<IScheduler, int> mapFrom = new Dictionary<IScheduler, int>();

        static ReactivePropertySchedulerMapper()
        {
            // default map
            var mappings = new[]
            {
               (-2, CurrentThreadScheduler.Instance ),
               (-3, ImmediateScheduler.Instance ),
               (-4, TaskPoolScheduler.Default ),
               (-5, System.Reactive.Concurrency.NewThreadScheduler.Default ),
               (-6, Scheduler.ThreadPool ),
               (-7, System.Reactive.Concurrency.DefaultScheduler.Instance),

               (-1, UIDispatcherScheduler.Default ), // override
            };

            foreach (var item in mappings)
            {
                ReactivePropertySchedulerMapper.mapTo[item.Item1] = item.Item2;
                ReactivePropertySchedulerMapper.mapFrom[item.Item2] = item.Item1;
            }
        }

#pragma warning restore CS0618

        public static IScheduler GetScheduler(int id)
        {
            IScheduler scheduler;
            if (mapTo.TryGetValue(id, out scheduler))
            {
                return scheduler;
            }
            else
            {
                throw new ArgumentException("Can't find registered scheduler. Id:" + id + ". Please call ReactivePropertySchedulerMapper.RegisterScheudler on application startup.");
            }
        }

        public static int GetSchedulerId(IScheduler scheduler)
        {
            int id;
            if (mapFrom.TryGetValue(scheduler, out id))
            {
                return id;
            }
            else
            {
                throw new ArgumentException("Can't find registered id. Scheduler:" + scheduler.GetType().Name + ". . Please call ReactivePropertySchedulerMapper.RegisterScheudler on application startup.");
            }
        }

        public static void RegisterScheduler(uint id, IScheduler scheduler)
        {
            mapTo[(int)id] = scheduler;
            mapFrom[scheduler] = (int)id;
        }

        internal static int ToReactivePropertyModeInt<T>(global::Reactive.Bindings.ReactiveProperty<T> reactiveProperty)
        {
            var mode = ReactivePropertyMode.None;
            if (reactiveProperty.IsDistinctUntilChanged)
            {
                mode |= ReactivePropertyMode.DistinctUntilChanged;
            }
            if (reactiveProperty.IsRaiseLatestValueOnSubscribe)
            {
                mode |= ReactivePropertyMode.RaiseLatestValueOnSubscribe;
            }

            return (int)mode;
        }

        internal static int ToReactivePropertySlimModeInt<T>(global::Reactive.Bindings.ReactivePropertySlim<T> reactiveProperty)
        {
            var mode = ReactivePropertyMode.None;
            if (reactiveProperty.IsDistinctUntilChanged)
            {
                mode |= ReactivePropertyMode.DistinctUntilChanged;
            }
            if (reactiveProperty.IsRaiseLatestValueOnSubscribe)
            {
                mode |= ReactivePropertyMode.RaiseLatestValueOnSubscribe;
            }
            return (int)mode;
        }
    }

    // [Mode, SchedulerId, Value] : length should be three.
    public class ReactivePropertyFormatter<T> : IMessagePackFormatter<ReactiveProperty<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;

                offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);

                offset += MessagePackBinary.WriteInt32(ref bytes, offset, ReactivePropertySchedulerMapper.ToReactivePropertyModeInt(value));
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, ReactivePropertySchedulerMapper.GetSchedulerId(value.RaiseEventScheduler));
                offset += formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);

                return offset - startOffset;
            }
        }

        public ReactiveProperty<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                if (length != 3) throw new InvalidOperationException("Invalid ReactiveProperty data.");

                var mode = (ReactivePropertyMode)MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                offset += readSize;

                var schedulerId = MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                offset += readSize;

                var scheduler = ReactivePropertySchedulerMapper.GetScheduler(schedulerId);

                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;

                return new ReactiveProperty<T>(scheduler, v, mode);
            }

        }
    }

    public class InterfaceReactivePropertyFormatter<T> : IMessagePackFormatter<IReactiveProperty<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, IReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref bytes, offset, rxProp, formatterResolver);
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref bytes, offset, slimProp, formatterResolver);
            }

            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                throw new InvalidOperationException("Serializer only supports ReactiveProperty<T> or ReactivePropertySlim<T>. If serialize is ReadOnlyReactiveProperty, should mark [Ignore] and restore on IMessagePackSerializationCallbackReceiver.OnAfterDeserialize. Type:" + value.GetType().Name);
            }
        }

        public IReactiveProperty<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);

            switch (length)
            {
                case 2:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                case 3:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                default:
                    throw new InvalidOperationException("Invalid ReactiveProperty or ReactivePropertySlim data.");
            }
        }
    }

    public class InterfaceReadOnlyReactivePropertyFormatter<T> : IMessagePackFormatter<IReadOnlyReactiveProperty<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, IReadOnlyReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref bytes, offset, rxProp, formatterResolver);
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref bytes, offset, slimProp, formatterResolver);
            }

            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                throw new InvalidOperationException("Serializer only supports ReactiveProperty<T> or ReactivePropertySlim<T>. If serialize is ReadOnlyReactiveProperty, should mark [Ignore] and restore on IMessagePackSerializationCallbackReceiver.OnAfterDeserialize. Type:" + value.GetType().Name);
            }
        }

        public IReadOnlyReactiveProperty<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);

            switch (length)
            {
                case 2:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                case 3:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(bytes, offset, formatterResolver, out readSize);
                default:
                    throw new InvalidOperationException("Invalid ReactiveProperty or ReactivePropertySlim data.");
            }
        }
    }

    public class ReactiveCollectionFormatter<T> : CollectionFormatterBase<T, ReactiveCollection<T>>
    {
        protected override void Add(ReactiveCollection<T> collection, int index, T value)
        {
            collection.Add(value);
        }

        protected override ReactiveCollection<T> Create(int count)
        {
            return new ReactiveCollection<T>();
        }
    }

    public class UnitFormatter : IMessagePackFormatter<Unit>
    {
        public static IMessagePackFormatter<Unit> Instance = new UnitFormatter();

        UnitFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Unit value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public Unit Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code:" + MessagePackCode.ToFormatName(bytes[offset]));
            }
        }
    }

    public class NullableUnitFormatter : IMessagePackFormatter<Unit?>
    {
        public static IMessagePackFormatter<Unit?> Instance = new NullableUnitFormatter();

        NullableUnitFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Unit? value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public Unit? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code:" + MessagePackCode.ToFormatName(bytes[offset]));
            }
        }
    }

    // [Mode, Value]
    public class ReactivePropertySlimFormatter<T> : IMessagePackFormatter<ReactivePropertySlim<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, ReactivePropertySlim<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {
                var startOffset = offset;

                offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 2);

                offset += MessagePackBinary.WriteInt32(ref bytes, offset, ReactivePropertySchedulerMapper.ToReactivePropertySlimModeInt(value));
                offset += formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);

                return offset - startOffset;
            }
        }

        public ReactivePropertySlim<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = offset;

                var length = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset += readSize;
                if (length != 2) throw new InvalidOperationException("Invalid ReactivePropertySlim data.");

                var mode = (ReactivePropertyMode)MessagePackBinary.ReadInt32(bytes, offset, out readSize);
                offset += readSize;

                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(bytes, offset, formatterResolver, out readSize);
                offset += readSize;

                readSize = offset - startOffset;

                return new ReactivePropertySlim<T>(v, mode);
            }

        }
    }
}