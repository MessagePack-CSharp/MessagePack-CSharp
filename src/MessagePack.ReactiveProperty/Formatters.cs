using MessagePack.Formatters;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;

namespace MessagePack.ReactiveProperty
{
    public static class ReactivePropertySchedulerMapper
    {
        // default map
        static Dictionary<int, IScheduler> mapTo = new Dictionary<int, IScheduler>
        {
            {-1, UIDispatcherScheduler.Default },
            {-2, CurrentThreadScheduler.Instance },
            {-3, ImmediateScheduler.Instance },
            // more schedulers...
            // {-4,  ThreadPoolScheduler.Instance },
        };

        static Dictionary<IScheduler, int> mapFrom = new Dictionary<IScheduler, int>
        {
            {UIDispatcherScheduler.Default,-1 },
            {CurrentThreadScheduler.Instance,-2 },
            {ImmediateScheduler.Instance,-3 },
        };

        public static IScheduler GetScheduler(int id)
        {
            IScheduler scheduler;
            if (mapTo.TryGetValue(id, out scheduler))
            {
                return scheduler;
            }
            else
            {
                throw new ArgumentException("Can't find registered scheduler. Id:" + id);
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
                throw new ArgumentException("Can't find registered id. Scheduler:" + scheduler.GetType().Name);
            }
        }

        public static void RegisterScheduler(uint id, IScheduler scheduler)
        {
            mapTo[(int)id] = scheduler;
            mapFrom[scheduler] = (int)id;
        }
    }

    // [Mode, SchedulerId, Value] : length should be three.
    public class ReactivePropertyFormatter<T> : IMessagePackFormatter<IReactiveProperty<T>>
    {
        public int Serialize(ref byte[] bytes, int offset, IReactiveProperty<T> value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            else
            {

                var startOffset = offset;

                offset += MessagePackBinary.WriteFixedArrayHeaderUnsafe(ref bytes, offset, 3);
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 1); // TODO:Write Mode
                offset += MessagePackBinary.WriteInt32(ref bytes, offset, 1); // TODO:ReactivePropertySchedulerMapper.GetSchedulerId(
                offset += formatterResolver.GetFormatterWithVerify<T>().Serialize(ref bytes, offset, value.Value, formatterResolver);

                return offset - startOffset;
            }
        }

        public IReactiveProperty<T> Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
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

    // TODO:IReactiveProperty

    // TODO:ReactiveCollection?
}
