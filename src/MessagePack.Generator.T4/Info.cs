// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.T4;

internal sealed record Info(string Path, string HintName, string? Namespace, List<(string Name, bool IsValueType)> Types)
{
}
