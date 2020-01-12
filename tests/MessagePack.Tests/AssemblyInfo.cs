using System.Runtime.InteropServices;
using Xunit;

// Given MessagePack v1.x has mutable statics, we cannot afford to parallelize tests that might be mutating them
[assembly: CollectionBehavior(DisableTestParallelization = true)]
