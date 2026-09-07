// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// V3COMPAT-EDIT: v3's [Union(0, typeof(Derived1))] becomes v4's [MessagePackObject] +
// [UnionTag(typeof(Derived1), 0)] - the mechanical migration the attribute rename prescribes.
// (The v3 attribute itself stays recognized by the runtime reflection tier only; this
// project exercises the source generator, so the native spelling is the faithful port.)
[MessagePackObject]
[UnionTag(typeof(Derived1), 0)]
[UnionTag(typeof(Derived2), 1)]
public interface IMyType
{
}
