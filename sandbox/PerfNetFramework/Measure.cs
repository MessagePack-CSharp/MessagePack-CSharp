// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace PerfNetFramework
{
    internal struct Measure : IDisposable
    {
        private string label;
        private Stopwatch sw;

        public Measure(string label)
        {
            this.label = label;
            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            if (!Program.Deserializing)
            {
                BenchmarkEventSource.Instance.Serialize(label);
            }
            else
            {
                BenchmarkEventSource.Instance.Deserialize(label);
            }

            this.sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            this.sw.Stop();
            if (!Program.Deserializing)
            {
                BenchmarkEventSource.Instance.SerializeEnd();
            }
            else
            {
                BenchmarkEventSource.Instance.DeserializeEnd();
            }

            Console.WriteLine($"{this.label,-25}   {this.sw.Elapsed.TotalMilliseconds,12:F2} ms");

            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
    }
}
