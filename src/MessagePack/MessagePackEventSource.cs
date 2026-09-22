// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using System;
using System.Diagnostics.Tracing;

namespace MessagePack
{
    [EventSource(Name = "MessagePack")]
    internal class MessagePackEventSource : EventSource
    {
        internal static readonly MessagePackEventSource Instance = new();

        private const int FormatterDynamicallyGeneratedStartEvent = 1;
        private const int FormatterDynamicallyGeneratedStopEvent = 2;

        private MessagePackEventSource()
        {
        }

        [Event(FormatterDynamicallyGeneratedStartEvent, Task = Tasks.FormatterDynamicallyGenerated, Opcode = EventOpcode.Start)]
        public void FormatterDynamicallyGeneratedStart()
        {
            WriteEvent(FormatterDynamicallyGeneratedStartEvent);
        }

        [Event(FormatterDynamicallyGeneratedStopEvent, Task = Tasks.FormatterDynamicallyGenerated, Opcode = EventOpcode.Stop)]
        public void FormatterDynamicallyGeneratedStop(string? dataType)
        {
            WriteEvent(FormatterDynamicallyGeneratedStopEvent, dataType);
        }

        /// <summary>
        /// Names of constants in this class make up the middle term in the event name
        /// E.g.: MessagePack/InvokeMethod/Start.
        /// </summary>
        /// <remarks>Name of this class is important for EventSource.</remarks>
        public static class Tasks
        {
            public const EventTask FormatterDynamicallyGenerated = (EventTask)1;
        }
    }

    /// <summary>
    /// Helper methods for <see cref="MessagePackEventSource"/>.
    /// </summary>
    /// <remarks>
    /// This methods may contain parameter types that are not allowed on the
    /// <see cref="MessagePackEventSource"/> class itself.
    /// If these methods were to be moved to the class itself,
    /// eventing would silently fail at runtime, observable only by watching the events (e.g. with PerfView).
    /// </remarks>
    internal static class MessagePackEventSourceExtensions
    {
        internal static void FormatterDynamicallyGeneratedStop(this MessagePackEventSource source, Type dataType)
        {
            if (source.IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                source.FormatterDynamicallyGeneratedStop(dataType.AssemblyQualifiedName);
            }
        }
    }
}
