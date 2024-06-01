﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
internal record HasPropertyWithTypeWithCustomFormatter
{
    [Key(0)]
    internal CustomFormatterRecord? CustomValue { get; set; }
}