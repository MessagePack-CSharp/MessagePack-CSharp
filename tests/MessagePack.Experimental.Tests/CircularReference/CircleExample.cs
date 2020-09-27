// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Experimental.Tests.CircularReference
{
    [MessagePackObject]
    public sealed class CircleExample
    {
        [Key(0)]
        public CircleExample Parent { get; set; }

        [Key(1)]
        public CircleExample Child0 { get; set; }

        [Key(2)]
        public CircleExample Child1 { get; set; }

        [Key(3)]
        public int Id { get; set; }
    }
}
