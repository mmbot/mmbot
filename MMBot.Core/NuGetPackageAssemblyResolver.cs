using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Logging;
using MMBot.Brains;
using MMBot.Router;
using MMBot.Scripts;
using Roslyn.Compilers.CSharp;
using ScriptCs;
using ScriptCs.Hosting.Package;

namespace MMBot
{
    public class NuGetPackageAssemblyResolver : IRobotPluginLocator
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

        public NuGetPackageAssemblyResolver(LoggerConfigurator logConfig)
        {
            _log = logConfig.GetLogger();
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            RefreshAssemblies(_log);
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

            var par = new PackageAssemblyResolver(fileSystem, new PackageContainer(fileSystem, log), log);

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

        public IEnumerable<IScript> GetPluginScripts()
        {
            return GetCompiledScriptsFromPackages().Select(TypedScript.Create).ToArray();
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
            return ProbeForType(typeof(IBrain)).FirstOrDefault(t => (string.IsNullOrEmpty(name) 
                || string.Equals(name, t.Name, StringComparison.InvariantCultureIgnoreCase) 
                || string.Equals(name + "Brain", t.Name, StringComparison.InvariantCultureIgnoreCase)));
        }

        public Type GetCompiledRouterFromPackages(string name = null)
        {
            return ProbeForType(typeof(IRouter))
                .FirstOrDefault(t => t != typeof(NullRouter) && 
                    (string.IsNullOrEmpty(name) ||
                    string.Equals(name, t.Name, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(name + "Router", t.Name, StringComparison.InvariantCultureIgnoreCase)));
        }

        private IEnumerable<Type> ProbeForType(Type type)
        {
            var assemblies = from path in Assemblies
                let fileName = Path.GetFileNameWithoutExtension(path)
                where fileName.Split('.').Contains("mmbot", StringComparer.InvariantCultureIgnoreCase)
                select path;

            assemblies = FilterAssembliesToMostRecent(assemblies);

            return assemblies.SelectMany(assemblyFile =>
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyFile);
                    return GetTypesFromAssembly(assembly, type);
                }
                catch (Exception ex)
                {
                    _log.WarnFormat("Could not load assembly '{0}': {1}", ex, assemblyFile, ex.Message);
                    return new Type[0];
                }
            }).Concat(GetTypesFromAssembly(typeof(NuGetPackageAssemblyResolver).Assembly, type));
        }

        private IEnumerable<Type> GetTypesFromAssembly(Assembly assembly, Type type)
        {
            if (assembly == null)
            {
                return new Type[0];
            }
            return
                assembly.GetTypes().Where(t => type.IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericTypeDefinition);
        }

        public static IEnumerable<string> FilterAssembliesToMostRecent(IEnumerable<string> assemblies)
        {
            var filtered = from assemblyPath in assemblies
                           let name = AssemblyName.GetAssemblyName(assemblyPath)
                           group new { Path = assemblyPath, Name = name } by name.Name;

            return filtered.Select(g => g.OrderByDescending(a => a.Name.Version).First().Path);
        }


        public Type[] GetAdapters()
        {
            var adapters = GetCompiledAdaptersFromPackages().ToArray();
            if(!adapters.Any())
            {
                _log.Warn("Could not find any adapters. Loading the default console adapter only");
            }

            return adapters;
        }

        public Type GetBrain(string name)
        {
            var brain = GetCompiledBrainFromPackages(name);

            if (brain == null && !string.IsNullOrEmpty(name))
            {
                _log.Fatal("No IBrain implementation found. If you have configured MMBOT_BRAIN_NAME, verify that you have installed the relevant package.");
            }

            return brain;
        }

        public Type GetRouter(string name)
        {
            var router = GetCompiledRouterFromPackages(name);
            if (router == null && !string.IsNullOrEmpty(name))
            {
                _log.Fatal("The router was enabled but no implementation was found. Make sure you have installed the relevant router package");
            }
            return router;
        }
    }
}