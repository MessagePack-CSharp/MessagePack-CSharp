// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(AllowPrivate = true)]
internal partial record MyMessagePackObject // V3COMPAT-EDIT: v4's AllowPrivate generates the formatter as a nested class, which requires partial (MsgPack010)
{
    [Key(0)]
    internal MyEnum EnumValue { get; set; }
}
