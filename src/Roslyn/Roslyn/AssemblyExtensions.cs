using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MSBuilder
{
    internal static class AssemblyExtensions
    {
        public static string[] GetAllReferences(this Assembly assembly)
        {
            var assemblies = new HashSet<Assembly>();
            PopulateAssemblies(assembly, assemblies);

            return assemblies
                .Select(asm => asm.ManifestModule.FullyQualifiedName)
                .ToArray();
        }

        static void PopulateAssemblies(Assembly assembly, HashSet<Assembly> assemblies)
        {
            if (assemblies.Contains(assembly))
                return;

            assemblies.Add(assembly);
            foreach (var referenced in assembly.GetReferencedAssemblies().Select(name => Assembly.Load(name)))
            {
                PopulateAssemblies(referenced, assemblies);
            }
        }
    }
}
