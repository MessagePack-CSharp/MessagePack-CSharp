using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfNetFramework
{
    [EventSource(Name = "MessagePack-Benchmark")]
    internal sealed class BenchmarkEventSource : EventSource
    {
        private const int MessagePackSerializeStartEvent = 1;
        private const int MessagePackSerializeStopEvent = 2;
        private const int MessagePackDeserializeStartEvent = 3;
        private const int MessagePackDeserializeStopEvent = 4;
        private const int MessagePackSessionStartEvent = 5;
        private const int MessagePackSessionStopEvent = 6;

        internal static readonly BenchmarkEventSource Instance = new BenchmarkEventSource();

        private BenchmarkEventSource() { }

        /// <summary>
        /// Marks the start of a serialization benchmark.
        /// </summary>
        /// <param name="impl">The library performing the serialization.</param>
        [Event(MessagePackSerializeStartEvent, Task = Tasks.Serialize, Opcode = EventOpcode.Start)]
        public void Serialize(string impl)
        {
            this.WriteEvent(MessagePackSerializeStartEvent, impl);
        }

        [Event(MessagePackSerializeStopEvent, Task = Tasks.Serialize, Opcode = EventOpcode.Stop)]
        public void SerializeEnd()
        {
            this.WriteEvent(MessagePackSerializeStopEvent);
        }

        /// <summary>
        /// Marks the start of a deserialization benchmark.
        /// </summary>
        /// <param name="impl">The library performing the serialization.</param>
        [Event(MessagePackDeserializeStartEvent, Task = Tasks.Deserialize, Opcode = EventOpcode.Start)]
        public void Deserialize(string impl)
        {
            this.WriteEvent(MessagePackDeserializeStartEvent, impl);
        }

        [Event(MessagePackDeserializeStopEvent, Task = Tasks.Deserialize, Opcode = EventOpcode.Stop)]
        public void DeserializeEnd()
        {
            this.WriteEvent(MessagePackDeserializeStopEvent);
        }

        [Event(MessagePackSessionStartEvent, Task = Tasks.Session, Opcode = EventOpcode.Start)]
        public void Session(int count)
        {
            this.WriteEvent(MessagePackSessionStartEvent, count);
        }

        [Event(MessagePackSessionStopEvent, Task = Tasks.Session, Opcode = EventOpcode.Stop)]
        public void SessionEnd()
        {
            this.WriteEvent(MessagePackSessionStopEvent);
        }

        internal static class Tasks
        {
            internal const EventTask Session = (EventTask)1;
            internal const EventTask Serialize = (EventTask)2;
            internal const EventTask Deserialize = (EventTask)3;
        }
    }
}
