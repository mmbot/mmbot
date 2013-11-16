using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using MMBot;
using MMBot.Scripts;
using ScriptCs;
using ScriptCs.Hosting.Package;

namespace mmbot
{
    internal class NuGetPackageAssemblyResolver
    {
        private readonly ILog _log;
        private static IEnumerable<string> _assemblies;

        public NuGetPackageAssemblyResolver(ILog log)
        {
            _log = log;
            RefreshAssemblies(log);
        }

        public static IEnumerable<string> Assemblies
        {
            get { return _assemblies; }
        }

        private static void RefreshAssemblies(ILog log)
        {
            var fileSystem = new FileSystem();

            var par = new PackageAssemblyResolver(fileSystem, new PackageContainer(fileSystem), log);

            _assemblies = par.GetAssemblyNames(fileSystem.CurrentDirectory);
        }

        public Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            return
                Assemblies.Where(
                    a =>
                        String.Equals(Path.GetFileNameWithoutExtension(a), args.Name,
                            StringComparison.InvariantCultureIgnoreCase))
                    .Select(Assembly.LoadFrom).FirstOrDefault();
        }

        public IEnumerable<Type> GetCompiledScriptsFromPackages()
        {
            return ProbeForType(typeof(IMMBotScript));
        }

        public IEnumerable<Type> GetCompiledAdaptersFromPackages()
        {
            return ProbeForType(typeof(Adapter));
        }

        private IEnumerable<Type> ProbeForType(Type type)
        {
            var assemblies = from path in Assemblies
                let fileName = Path.GetFileNameWithoutExtension(path)
                where fileName.Split('.').Contains("mmbot", StringComparer.InvariantCultureIgnoreCase)
                select path;


            return assemblies.SelectMany(assemblyFile =>
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyFile);
                    return assembly.GetTypes().Where(type.IsAssignableFrom);
                }
                catch (Exception ex)
                {
                    _log.WarnFormat("Could not load assembly '{0}': {1}", ex, assemblyFile, ex.Message);
                    return new Type[0];
                }
            });
        }
    }
}