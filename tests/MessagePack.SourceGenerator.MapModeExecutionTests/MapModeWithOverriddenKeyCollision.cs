// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public static class MapModeWithOverriddenKeyCollision
{
    [MessagePackObject(true)]
    public class Base : IEquatable<Base>
    {
        public string? Prop1 { get; set; }

        public bool Equals(Base? other) => other is not null && this.Prop1 == other.Prop1;
    }

    [MessagePackObject(true)]
    public class Derived : Base, IEquatable<Derived>
    {
        [Key("B_Prop1")]
        public new string? Prop1 { get; set; }

        public bool Equals(Derived? other) => base.Equals(other) && this.Prop1 == other.Prop1;
    }
}
