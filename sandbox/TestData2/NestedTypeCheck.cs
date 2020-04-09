// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1649 // File name should match first type name

namespace TestData2
{
    [MessagePackObject(true)]
    public class Nest1
    {
        public Id EnumId { get; set; }

        public IdType ClassId { get; set; }

        public enum Id
        {
            A,
        }

        [MessagePackObject(true)]
        public class IdType
        {
        }
    }

    [MessagePackObject(true)]
    public class Nest2
    {
        public Id EnumId { get; set; }

        public IdType ClassId { get; set; }

        public enum Id
        {
            A,
        }

        [MessagePackObject(true)]
        public class IdType
        {
        }
    }
}
