namespace AssemblyBrowserLibrary
{
    public interface IAssemblyBrowser
    {
        ContainerInfo[] GetNamespaces(string assemblyPath);
    }
}
