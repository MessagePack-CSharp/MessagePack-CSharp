// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack;

internal static class Constants
{
    internal const string DynamicFormatters = "This API is chiefly concerned with or relies on dynamically generated formatters.";

    internal const string DynamicFormattersIncluded = "This API includes support for dynamically generated formatters. Look for an alternative API that offers similar functionality without the dynamic aspect.";

    internal const string Typeless = "Typeless functionality involves loading types by name.";

    internal const string AvoidDynamicCodeRuntimeCheck = "An AvoidDynamicCode runtime check guards the dynamic code path.";

    internal const string ClosingGenerics = "This method constructs generic types with type arguments determined at runtime.";

    internal const string Wildcard = "This method cannot be statically analyzed, and probably relies on dynamic code and/or unreferenced code.";
}
