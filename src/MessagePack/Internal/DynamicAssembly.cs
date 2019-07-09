#if !UNITY_WSA

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    internal class DynamicAssembly
    {
#if NETFRAMEWORK // We don't ship a net472 target, but we might add one for debugging purposes
        readonly string moduleName;
#endif
        readonly AssemblyBuilder assemblyBuilder;
        readonly ModuleBuilder moduleBuilder;

        // don't expose ModuleBuilder
        // public ModuleBuilder ModuleBuilder { get { return moduleBuilder; } }

        readonly object gate = new object();

        public DynamicAssembly(string moduleName)
        {
#if NETFRAMEWORK // We don't ship a net472 target, but we might add one for debugging purposes
            var builderAccess = AssemblyBuilderAccess.RunAndSave;
            this.moduleName = moduleName;
#else
            var builderAccess = AssemblyBuilderAccess.Run;
#endif
            this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(moduleName), builderAccess);
            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
        }

        // requires lock on mono environment. see: https://github.com/neuecc/MessagePack-CSharp/issues/161

        public TypeBuilder DefineType(string name, TypeAttributes attr)
        {
            lock (gate)
            {
                return moduleBuilder.DefineType(name, attr);
            }
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent)
        {
            lock (gate)
            {
                return moduleBuilder.DefineType(name, attr, parent);
            }
        }

        public TypeBuilder DefineType(string name, TypeAttributes attr, Type parent, Type[] interfaces)
        {
            lock (gate)
            {
                return moduleBuilder.DefineType(name, attr, parent, interfaces);
            }
        }

#if NETFRAMEWORK

        public AssemblyBuilder Save()
        {
            assemblyBuilder.Save(this.moduleName + ".dll");
            return assemblyBuilder;
        }

#endif
    }
}

#endif
