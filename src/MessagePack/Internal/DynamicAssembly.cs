using System.Reflection;
using System.Reflection.Emit;

namespace MessagePack.Internal
{
    internal class DynamicAssembly
    {
        readonly object gate = new object();
        readonly string moduleName;
        readonly AssemblyBuilder assemblyBuilder;
        readonly ModuleBuilder moduleBuilder;

        public ModuleBuilder ModuleBuilder { get { return moduleBuilder; } }

        public DynamicAssembly(string moduleName)
        {
            this.moduleName = moduleName;
#if NET_35
            this.assemblyBuilder = System.AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.RunAndSave);
            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName, moduleName + ".dll");
#else
            this.assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(moduleName), AssemblyBuilderAccess.Run);
            this.moduleBuilder = assemblyBuilder.DefineDynamicModule(moduleName);
#endif
        }

#if NET_35

        public void Save()
        {
            assemblyBuilder.Save(moduleName + ".dll");
        }

#endif
    }
}