using System;
using System.Reflection;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Provides assembly resolution by searching currently loaded assemblies in the application domain.
    /// </summary>
    public static class AssemblyResolver
    {
        /// <summary>
        /// Resolves an assembly by searching currently loaded assemblies in the application domain.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The event data containing the name of the assembly to resolve.</param>
        /// <returns>The loaded assembly if found; otherwise, <see langword="null"/>.</returns>
        public static Assembly? Resolve(object sender, ResolveEventArgs args)
        {
            if (_resolving)
            {
                return null;
            }
            _resolving = true;
            try
            {
                string simpleName = new AssemblyName(args.Name).Name;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (!asm.IsDynamic && asm.GetName().Name == simpleName)
                    {
                        return asm;
                    }
                }
                return null;
            }
            finally
            {
                _resolving = false;
            }
        }

        /// <summary>
        /// Registers the assembly resolver for the current application domain.
        /// </summary>
        public static void Register()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        /// <summary>
        /// Removes the assembly resolution event handler from the current application domain.
        /// </summary>
        public static void Unregister()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
        }

        /// <summary>
        /// Indicates whether a resolution operation is currently in progress.
        /// </summary>
        private static bool _resolving;
    }
}
