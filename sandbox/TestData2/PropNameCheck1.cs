// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace TestData2
{
    [MessagePackObject(true)]
    public class PropNameCheck1
    {
        public string MyProperty1 { get; set; }

        public virtual string MyProperty2 { get; set; }
    }
}
