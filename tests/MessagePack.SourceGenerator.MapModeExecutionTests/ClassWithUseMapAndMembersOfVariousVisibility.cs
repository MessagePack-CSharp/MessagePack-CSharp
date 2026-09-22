// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack;

#nullable enable

[MessagePackObject(true)]
public class ClassWithUseMapAndMembersOfVariousVisibility : IEquatable<ClassWithUseMapAndMembersOfVariousVisibility>
{
    public int PublicProperty { get; set; }

    internal int InternalProperty { get; set; }

    private int PrivateProperty { get; set; }

    public int PublicField;

    internal int InternalField;

    private int privateField;

    public ref int PrivateFieldAccessor() => ref this.privateField;

    public int GetPrivateProperty() => this.PrivateProperty;

    public void SetPrivateProperty(int value) => this.PrivateProperty = value;

    public bool Equals(ClassWithUseMapAndMembersOfVariousVisibility? other)
    {
        return other is not null
            && this.PublicProperty == other.PublicProperty
            && this.InternalProperty == other.InternalProperty
            && this.PrivateProperty == other.PrivateProperty
            && this.PublicField == other.PublicField
            && this.InternalField == other.InternalField
            && this.privateField == other.privateField;
    }
}

[MessagePackObject(true, AllowPrivate = true)]
public partial class ClassWithUseMapAndMembersOfVariousVisibilityAllowPrivate : IEquatable<ClassWithUseMapAndMembersOfVariousVisibilityAllowPrivate>
{
    public int PublicProperty { get; set; }

    internal int InternalProperty { get; set; }

    private int PrivateProperty { get; set; }

    public int PublicField;

    internal int InternalField;

    private int privateField;

    public ref int PrivateFieldAccessor() => ref this.privateField;

    public int GetPrivateProperty() => this.PrivateProperty;

    public void SetPrivateProperty(int value) => this.PrivateProperty = value;

    public bool Equals(ClassWithUseMapAndMembersOfVariousVisibilityAllowPrivate? other)
    {
        return other is not null
            && this.PublicProperty == other.PublicProperty
            && this.InternalProperty == other.InternalProperty
            && this.PrivateProperty == other.PrivateProperty
            && this.PublicField == other.PublicField
            && this.InternalField == other.InternalField
            && this.privateField == other.privateField;
    }
}
