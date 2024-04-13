// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
internal record MyMessagePackRecord([property: Key("p")] string PhoneNumber, [property: Key("c")] int Count);
