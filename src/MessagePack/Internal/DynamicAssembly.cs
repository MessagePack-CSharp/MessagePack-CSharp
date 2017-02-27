using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    internal class DynamicAssembly
    {
#if NET_35
        readonly string moduleName;
#endif
        readonly AssemblyBuilder assemblyBuilder;
        readonly ModuleBuilder moduleBuilder;

        public ModuleBuilder ModuleBuilder { get { return moduleBuilder; } }

        public DynamicAssembly(string moduleName)
        {
#if NET_35
            this.moduleName = moduleName;
            this.assemblyBuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.RunAndSave);
            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName, moduleName + ".dll");
#else
#if NETSTANDARD1_4
            this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.Run);
#else
            this.assemblyBuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.Run);
#endif

            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
#endif
        }

#if NET_35

        public AssemblyBuilder Save()
        {
            assemblyBuilder.Save(moduleName + ".dll");
            return assemblyBuilder;
        }

#endif
    }
}