// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

namespace NewPropertyInDerivedType
{
    [MessagePackObject]
    public class Base : IEquatable<Base>
    {
#if FORCE_MAP_MODE
        [Key("A0")]
#else
        [Key(0)]
#endif
        public string? Prop { get; set; }

#if FORCE_MAP_MODE
        [Key("A1")]
#else
        [Key(1)]
#endif
        public string? Field;

#if FORCE_MAP_MODE
        [Key("A2")]
#else
        [Key(2)]
#endif
        public string? TwoFaced { get; set; } // derived declares a *field* by the same name.

        public bool Equals(Base? other) => other is not null && this.Prop == other.Prop && this.Field == other.Field && this.TwoFaced == other.TwoFaced;
    }

    [MessagePackObject]
    public class Derived : Base, IEquatable<Derived>
    {
#if FORCE_MAP_MODE
        [Key("A3")]
#else
        [Key(3)]
#endif
        public new string? Prop { get; set; }

#if FORCE_MAP_MODE
        [Key("A4")]
#else
        [Key(4)]
#endif
        public new string? Field;

#if FORCE_MAP_MODE
        [Key("A5")]
#else
        [Key(5)]
#endif
        public new string? TwoFaced;

        public bool Equals(Derived? other) => base.Equals(other) && this.Prop == other.Prop && this.Field == other.Field && this.TwoFaced == other.TwoFaced;
    }
}
