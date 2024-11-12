﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
public record HasPropertyWithCustomFormatterAttribute
{
    [Key(0), MessagePackFormatter(typeof(UnserializableRecordFormatter))]
    public UnserializableRecord? CustomValue { get; set; }
}
