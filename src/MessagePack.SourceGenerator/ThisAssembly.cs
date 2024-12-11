// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator
{
    public static class ThisAssembly
    {
        public static string AssemblyFileVersion
        {
            get
            {
                var version = typeof(ThisAssembly).Assembly.GetName().Version.ToString();
                return version;
            }
        }

        public static Version Version
        {
            get
            {
                var version = typeof(ThisAssembly).Assembly.GetName().Version;
                return version;
            }
        }
    }
}
