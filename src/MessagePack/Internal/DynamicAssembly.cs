// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    internal class DynamicAssembly
    {
#if NETFRAMEWORK || NETSTANDARD2_0
        internal static readonly bool AvoidDynamicCode = false;
#else
        internal static readonly bool AvoidDynamicCode = !System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported;
#endif

#if NETFRAMEWORK // We don't ship a net472 target, but we might add one for debugging purposes
        private readonly string moduleName;
#endif
        private readonly AssemblyBuilder assemblyBuilder;
        private readonly ModuleBuilder moduleBuilder;

        // don't expose ModuleBuilder
        //// public ModuleBuilder ModuleBuilder { get { return moduleBuilder; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicAssembly"/> class.
        /// Please use <see cref="DynamicAssemblyFactory"/> instead in order to work across different AssemblyLoadContext that may have duplicate modules.
        /// </summary>
        /// <param name="moduleName">Name of the module to be generated.</param>
        /// <param name="skipVisibilityChecksTo">The names of assemblies that should be fully accessible to this dynamic one, bypassing visibility checks.</param>
        public DynamicAssembly(string moduleName, ImmutableHashSet<AssemblyName> skipVisibilityChecksTo)
        {
#if NETFRAMEWORK
            this.moduleName = moduleName;
#endif
            AssemblyBuilderAccess builderAccess = AssemblyBuilderAccess.RunAndCollect;
            this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(moduleName), builderAccess);
            this.moduleBuilder = this.assemblyBuilder.DefineDynamicModule(moduleName + ".dll");

            SkipClrVisibilityChecks skipChecks = new(this.assemblyBuilder, this.moduleBuilder);
            skipChecks.SkipVisibilityChecksFor(skipVisibilityChecksTo);
        }

        /* requires lock on mono environment. see: https://github.com/neuecc/MessagePack-CSharp/issues/161 */

        public TypeBuilder DefineType(string name, TypeAttributes attr) => this.moduleBuilder.DefineType(name, attr);

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type? parent) => this.moduleBuilder.DefineType(name, attr, parent);

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type? parent, Type[]? interfaces) => this.moduleBuilder.DefineType(name, attr, parent, interfaces);

#if NETFRAMEWORK

        internal AssemblyBuilder Save()
        {
            this.assemblyBuilder.Save(this.moduleName + ".dll");
            return this.assemblyBuilder;
        }

#endif
    }
}
