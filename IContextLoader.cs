namespace Configurator
{
    /// <summary>This is the main entry point for creating and populating a configuration context.</summary>
    public interface IContextLoader
    {
        /// <summary>Creates and returns a configuration context from a file.</summary><param name="path"></param><returns></returns>
        IContext loadContext(string path);
        /// <summary>Merges a configuration context with a file.</summary><param name="context"></param><param name="path"></param><returns></returns>
        IContext loadContext(IContext context, string path);
    }
}
