using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
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
        private static List<string> _assemblies;

        private static readonly string[] _blacklistedPackages =
        {
            "Akavache",
            "ScriptCs.Hosting",
            "ScriptCs.Contracts"
        };

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

            foreach(var packageName in _blacklistedPackages)
            {
                foreach(var packagePath in Directory.GetDirectories(Path.Combine(fileSystem.CurrentDirectory, "packages")).Where(d => new DirectoryInfo(d).Name.StartsWith(packageName, StringComparison.InvariantCultureIgnoreCase)))
                {

                    if(fileSystem.DirectoryExists(packagePath))
                    {
                        fileSystem.DeleteDirectory(packagePath);
                    }
                }
            }

            var par = new PackageAssemblyResolver(fileSystem, new PackageContainer(fileSystem), log);

            _assemblies = par.GetAssemblyNames(fileSystem.CurrentDirectory).ToList();

            // Add the assemblies in the current directory
            _assemblies.AddRange(Directory.GetFiles(fileSystem.CurrentDirectory, "*.dll")
                .Where(a => new AssemblyUtility().IsManagedAssembly(a)));
        }

        public Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = Assemblies.Where(
                a =>
                    String.Equals(Path.GetFileNameWithoutExtension(a), args.Name.Split(',').First(),
                        StringComparison.InvariantCultureIgnoreCase))
                .Select(Assembly.LoadFrom).FirstOrDefault();
            return
                assembly;
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
                    return assembly.GetTypes().Where(t => type.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition);
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