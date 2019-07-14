// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    public class ContractlessType
    {
        public ContractlessType(string name, ContractlessType nested)
        {
            this.Name = name;
            this.Nested = nested;
        }

        public string Name { get; }

        public ContractlessType Nested { get; }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

