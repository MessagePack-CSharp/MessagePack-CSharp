// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using MessagePack.Formatters;
using Reactive.Bindings;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.ReactivePropertyExtension
{
    public static class ReactivePropertySchedulerMapper
    {
#pragma warning disable CS0618

        private static Dictionary<int, IScheduler> mapTo = new Dictionary<int, IScheduler>();
        private static Dictionary<IScheduler, int> mapFrom = new Dictionary<IScheduler, int>();

        static ReactivePropertySchedulerMapper()
        {
            // default map
            (int, IScheduler)[] mappings = new[]
            {
               (-2, CurrentThreadScheduler.Instance),
               (-3, ImmediateScheduler.Instance),
               (-4, TaskPoolScheduler.Default),
               (-5, System.Reactive.Concurrency.NewThreadScheduler.Default),
               (-6, Scheduler.ThreadPool),
               (-7, System.Reactive.Concurrency.DefaultScheduler.Instance),

               (-1, UIDispatcherScheduler.Default), // override
            };

            foreach ((int, IScheduler) item in mappings)
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
            ReactivePropertyMode mode = ReactivePropertyMode.None;
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
            ReactivePropertyMode mode = ReactivePropertyMode.None;
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
    public class ReactivePropertyFormatter<T> : IMessagePackFormatter<ReactiveProperty<T>?>
    {
        public void Serialize(ref MessagePackWriter writer, ReactiveProperty<T>? value, MessagePackSerializerOptions options)
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
                options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public ReactiveProperty<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var length = reader.ReadArrayHeader();
                if (length != 3)
                {
                    throw new InvalidOperationException("Invalid ReactiveProperty data.");
                }

                var mode = (ReactivePropertyMode)reader.ReadInt32();

                var schedulerId = reader.ReadInt32();

                IScheduler scheduler = ReactivePropertySchedulerMapper.GetScheduler(schedulerId);

                options.Security.DepthStep(ref reader);
                try
                {
                    T v = options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);

                    return new ReactiveProperty<T>(scheduler, v, mode);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }

    public class InterfaceReactivePropertyFormatter<T> : IMessagePackFormatter<IReactiveProperty<T>>
    {
        public void Serialize(ref MessagePackWriter writer, IReactiveProperty<T> value, MessagePackSerializerOptions options)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref writer, rxProp, options);
                return;
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref writer, slimProp, options);
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

        public IReactiveProperty<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var length = reader.ReadArrayHeader();

            options.Security.DepthStep(ref reader);
            try
            {
                switch (length)
                {
                    case 2:
                        return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(ref reader, options);
                    case 3:
                        return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(ref reader, options);
                    default:
                        throw new InvalidOperationException("Invalid ReactiveProperty or ReactivePropertySlim data.");
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public class InterfaceReadOnlyReactivePropertyFormatter<T> : IMessagePackFormatter<IReadOnlyReactiveProperty<T>>
    {
        public void Serialize(ref MessagePackWriter writer, IReadOnlyReactiveProperty<T> value, MessagePackSerializerOptions options)
        {
            var rxProp = value as ReactiveProperty<T>;
            if (rxProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Serialize(ref writer, rxProp, options);
                return;
            }

            var slimProp = value as ReactivePropertySlim<T>;
            if (slimProp != null)
            {
                ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Serialize(ref writer, slimProp, options);
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

        public IReadOnlyReactiveProperty<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var length = reader.ReadArrayHeader();

            options.Security.DepthStep(ref reader);
            try
            {
                switch (length)
                {
                    case 2:
                        return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactivePropertySlim<T>>().Deserialize(ref reader, options);
                    case 3:
                        return ReactivePropertyResolver.Instance.GetFormatterWithVerify<ReactiveProperty<T>>().Deserialize(ref reader, options);
                    default:
                        throw new InvalidOperationException("Invalid ReactiveProperty or ReactivePropertySlim data.");
                }
            }
            finally
            {
                reader.Depth--;
            }
        }
    }

    public class ReactiveCollectionFormatter<T> : CollectionFormatterBase<T, ReactiveCollection<T>>
    {
        protected override void Add(ReactiveCollection<T> collection, int index, T value, MessagePackSerializerOptions options)
        {
            collection.Add(value);
        }

        protected override ReactiveCollection<T> Create(int count, MessagePackSerializerOptions options)
        {
            return new ReactiveCollection<T>();
        }
    }

    public class UnitFormatter : IMessagePackFormatter<Unit>
    {
        public static readonly UnitFormatter Instance = new UnitFormatter();

        private UnitFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Unit value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Unit Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
        public static readonly NullableUnitFormatter Instance = new NullableUnitFormatter();

        private NullableUnitFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Unit? value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Unit? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
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
    public class ReactivePropertySlimFormatter<T> : IMessagePackFormatter<ReactivePropertySlim<T>?>
    {
        public void Serialize(ref MessagePackWriter writer, ReactivePropertySlim<T>? value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                writer.WriteArrayHeader(2);

                writer.Write(ReactivePropertySchedulerMapper.ToReactivePropertySlimModeInt(value));
                options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public ReactivePropertySlim<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                var length = reader.ReadArrayHeader();
                if (length != 2)
                {
                    throw new InvalidOperationException("Invalid ReactivePropertySlim data.");
                }

                options.Security.DepthStep(ref reader);
                try
                {
                    var mode = (ReactivePropertyMode)reader.ReadInt32();

                    T v = options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);

                    return new ReactivePropertySlim<T>(v, mode);
                }
                finally
                {
                    reader.Depth--;
                }
            }
        }
    }
}
