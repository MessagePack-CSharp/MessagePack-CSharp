// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace TestData2
{
    [Union(0, typeof(UnionInterfaceImplementation))]
    public interface IUnionInterface
    {
        float Value { get; }
    }

    [MessagePackObject(true)]
    public class UnionInterfaceImplementation : IUnionInterface
    {
        public float Value { get; set; }
    }
}
