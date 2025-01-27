// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Internal;

// In Unity's AOT environment, MakeGenericType often works for Full Generic Sharing.
// https://unity.com/blog/engine-platform/il2cpp-full-generic-sharing-in-unity-2022-1-beta

// Therefore, even with AvoidDynamicCode, we include DynamicGenericResolver in StandardResolver.
// However, if the Formatter itself is stripped, it naturally won't function.
// While this behavior changes depending on Unity's build settings' StrippingLevel,
// we want MessagePack for C# to operate as naturally as possible even at the highest stripping level.

// To achieve this, we'll make Generic Formatters preserve.
// Since PreserveAttribute can be application-specific, we'll implement it as an internal type.
// https://docs.unity3d.com/6000.0/Documentation/Manual/managed-code-stripping-preserving.html

// Additionally, when supporting the generation of Generic-compatible Formatters,
// it might be worth considering making it a public type and applying it to generated types.

internal sealed class PreserveAttribute : System.Attribute
{
}
