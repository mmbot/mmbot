using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using MMBot;
using MMBot.Brains;
using MMBot.Router;
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
            "reactiveui-core",
            "ScriptCs.Core",
            "ScriptCs.Hosting",
            "ScriptCs.Contracts",
            "Rx-WindowStoreApps",
            "Rx-WinRT",
            "MMBot.Core"
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

            var packagesFolder = Path.Combine(fileSystem.CurrentDirectory, "packages");

            if(fileSystem.DirectoryExists(packagesFolder))
            {
                // Delete any blacklisted packages to avoid various issues with PackageAssemblyResolver
                // https://github.com/scriptcs/scriptcs/issues/511
                foreach (var packagePath in
                    _blacklistedPackages.SelectMany(packageName => Directory.GetDirectories(packagesFolder)
                                .Where(d => new DirectoryInfo(d).Name.StartsWith(packageName, StringComparison.InvariantCultureIgnoreCase)),
                                (packageName, packagePath) => new {packageName, packagePath})
                        .Where(t => fileSystem.DirectoryExists(t.packagePath))
                        .Select(t => @t.packagePath))
                {
                    fileSystem.DeleteDirectory(packagePath);
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

        public Type GetCompiledBrainFromPackages(string name = null)
        {
            return ProbeForType(typeof(IBrain)).FirstOrDefault(t => (string.IsNullOrEmpty(name) || string.Equals(name, t.Name, StringComparison.InvariantCultureIgnoreCase)));
        }

        public Type GetCompiledRouterFromPackages(string name = null)
        {
            return ProbeForType(typeof(IRouter))
                .FirstOrDefault(t => t != typeof(NullRouter) && 
                    (string.IsNullOrEmpty(name) ||
                    string.Equals(name, t.Name, StringComparison.InvariantCultureIgnoreCase)));
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