namespace MessagePack.SourceGenerator
{
    public static class ThisAssembly
    {
        public static string AssemblyFileVersion
        {
            get
            {
                return typeof(ThisAssembly).Assembly.GetName().Version.ToString();
            }
        }
    }
}
