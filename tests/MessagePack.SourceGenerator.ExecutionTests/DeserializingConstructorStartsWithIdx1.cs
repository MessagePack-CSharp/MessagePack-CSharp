// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
public class DeserializingConstructorStartsWithIdx1 : IEquatable<DeserializingConstructorStartsWithIdx1>
{
    [Key(1)] // 1 instead of 0. Works on v2. Test case from https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/1993
    public string Name { get; }

    [SerializationConstructor]
    public DeserializingConstructorStartsWithIdx1(string name) => Name = name;

    public bool Equals(DeserializingConstructorStartsWithIdx1? other) => other is not null && this.Name == other.Name;
}
