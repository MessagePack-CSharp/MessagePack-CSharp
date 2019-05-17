﻿using MessagePack.Formatters;
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
        public void Serialize(ref MessagePackWriter writer, ReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(3);

                writer.Write(ReactivePropertySchedulerMapper.ToReactivePropertyModeInt(value));
                writer.Write(ReactivePropertySchedulerMapper.GetSchedulerId(value.RaiseEventScheduler));
                formatterResolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, formatterResolver);
            }
        }

        public ReactiveProperty<T> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var length = reader.ReadArrayHeader();
                if (length != 3) throw new InvalidOperationException("Invalid ReactiveProperty data.");

                var mode = (ReactivePropertyMode)reader.ReadInt32();

                var schedulerId = reader.ReadInt32();

                var scheduler = ReactivePropertySchedulerMapper.GetScheduler(schedulerId);

                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);

                return new ReactiveProperty<T>(scheduler, v, mode);
            }

        }
    }

    public class InterfaceReactivePropertyFormatter<T> : IMessagePackFormatter<IReactiveProperty<T>>
    {
        public void Serialize(ref MessagePackWriter writer, IReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref writer, rxProp, formatterResolver);
                return;
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref writer, slimProp, formatterResolver);
                return;
            }

            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                throw new InvalidOperationException("Serializer only supports ReactiveProperty<T> or ReactivePropertySlim<T>. If serialize is ReadOnlyReactiveProperty, should mark [Ignore] and restore on IMessagePackSerializationCallbackReceiver.OnAfterDeserialize. Type:" + value.GetType().Name);
            }
        }

        public IReactiveProperty<T> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var length = reader.ReadArrayHeader();

            switch (length)
            {
                case 2:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(ref reader, formatterResolver);
                case 3:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(ref reader, formatterResolver);
                default:
                    throw new InvalidOperationException("Invalid ReactiveProperty or ReactivePropertySlim data.");
            }
        }
    }

    public class InterfaceReadOnlyReactivePropertyFormatter<T> : IMessagePackFormatter<IReadOnlyReactiveProperty<T>>
    {
        public void Serialize(ref MessagePackWriter writer, IReadOnlyReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref writer, rxProp, formatterResolver);
                return;
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref writer, slimProp, formatterResolver);
                return;
            }

            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                throw new InvalidOperationException("Serializer only supports ReactiveProperty<T> or ReactivePropertySlim<T>. If serialize is ReadOnlyReactiveProperty, should mark [Ignore] and restore on IMessagePackSerializationCallbackReceiver.OnAfterDeserialize. Type:" + value.GetType().Name);
            }
        }

        public IReadOnlyReactiveProperty<T> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            var length = reader.ReadArrayHeader();

            switch (length)
            {
                case 2:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(ref reader, formatterResolver);
                case 3:
                    return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(ref reader, formatterResolver);
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

        public void Serialize(ref MessagePackWriter writer, Unit value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil();
        }

        public Unit Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code: " + MessagePackCode.ToFormatName(reader.NextCode));
            }
        }
    }

    public class NullableUnitFormatter : IMessagePackFormatter<Unit?>
    {
        public static IMessagePackFormatter<Unit?> Instance = new NullableUnitFormatter();

        NullableUnitFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Unit? value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil();
        }

        public Unit? Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code: " + MessagePackCode.ToFormatName(reader.NextCode));
            }
        }
    }

    // [Mode, Value]
    public class ReactivePropertySlimFormatter<T> : IMessagePackFormatter<ReactivePropertySlim<T>>
    {
        public void Serialize(ref MessagePackWriter writer, ReactivePropertySlim<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(2);

                writer.Write(ReactivePropertySchedulerMapper.ToReactivePropertySlimModeInt(value));
                formatterResolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, formatterResolver);
            }
        }

        public ReactivePropertySlim<T> Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var length = reader.ReadArrayHeader();
                if (length != 2) throw new InvalidOperationException("Invalid ReactivePropertySlim data.");

                var mode = (ReactivePropertyMode)reader.ReadInt32();

                var v = formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);

                return new ReactivePropertySlim<T>(v, mode);
            }

        }
    }
}