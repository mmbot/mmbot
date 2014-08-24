using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using MMBot.Adapters;
using MMBot.Brains;
using MMBot.Router;
using MMBot.Scripts;
using ScriptCs;
using TinyIoC;

namespace MMBot
{
    public class RobotBuilder
    {
        private readonly LoggerConfigurator _logConfig;
        private List<Type> _adapterTypes = new List<Type>();
        private Type _routerType = typeof(NullRouter);
        private Type _brainType;
        private bool _pluginProbe = true;
        private bool _scriptProbe = true;
        private string _workingDirectory;
        private string _name;
        private IDictionary<string, string> _config = new Dictionary<string, string>();
        private IRobotPluginLocator _pluginLocator;
        private Type _scriptStoreType;
        private Type _scriptRunnerType;
        private bool _watch;

        protected RobotBuilder()
        {
        }

        public RobotBuilder(LoggerConfigurator logConfig) : this()
        {
            _logConfig = logConfig;
        }
        
        public Robot Build()
        {
            return Build(c => { });
        }

        public Robot Build(Action<TinyIoCContainer> preBuild)
        {
            preBuild = preBuild ?? (c => { });

            // Probe for types
            var robotPluginLocator = (_pluginLocator ?? new NuGetPackageAssemblyResolver(_logConfig));

            if (_pluginProbe)
            {
                _adapterTypes.AddRange(robotPluginLocator.GetAdapters());
                _brainType = robotPluginLocator.GetBrain(_config.GetValueOrDefault("MMBOT_BRAIN_NAME"));
                _routerType = robotPluginLocator.GetRouter(_config.GetValueOrDefault("MMBOT_ROUTER_NAME"));
            }

            var fileSystem = new FileSystem();

            if(!string.IsNullOrEmpty(_workingDirectory))
            {
                fileSystem.CurrentDirectory = _workingDirectory;
            }

            var container = TinyIoCContainer.Current;

            container.Register<Robot>();
            container.Register(robotPluginLocator);
            container.Register(typeof (IBrain), _brainType ?? typeof (AkavacheBrain)).AsSingleton();
            container.Register(typeof (IScriptStore), _scriptStoreType ?? typeof (LocalScriptStore)).AsSingleton();
            container.Register(typeof (IScriptRunner), _scriptRunnerType ?? typeof (ScriptRunner)).AsSingleton();
            container.Register(typeof (IRouter), _routerType ?? typeof (NullRouter)).AsSingleton();
            _adapterTypes.ForEach(t => container.Register(t));
            container.Register(_logConfig);
            container.Register<ConsoleAdapter>();
            container.Register<ILog>((c, o) => c.Resolve<LoggerConfigurator>().GetLogger());
            container.Register<FileSystem>(fileSystem);

            preBuild(container);

            var adapters = _adapterTypes.Select(a => container.Resolve(a, new NamedParameterOverloads(new Dictionary<string, object>{{"adapterId", a.Name}}))).ToDictionary(a => a.GetType().Name, a => a as IAdapter);

            // Need to explicitly add the ConsoleAdapter as it may not be found before now
            if (Environment.UserInteractive)
            {
                try
                {
                    Console.WriteLine();
                    var name = typeof (ConsoleAdapter).Name;
                    if(!adapters.ContainsKey(name))
                    {
                        adapters.Add(name, container.Resolve<ConsoleAdapter>(
                            new NamedParameterOverloads(new Dictionary<string, object>
                            {
                                {"adapterId", name}
                            })));
                    }
                }
                catch (Exception e)
                {
                    // do nothing as we are not a console app
                }
            }

            var robot = container.Resolve<Robot>(
                new NamedParameterOverloads(new Dictionary<string, object>
                {
                    {"name", _name ?? _config.GetValueOrDefault("MMBOT_ROBOT_NAME") ?? "mmbot"},
                    {"config", _config},
                    {"adapters", adapters}
                }));
                
            robot.AutoLoadScripts = _scriptProbe;
            robot.Watch = _watch;

            return robot;
        }

        public RobotBuilder DisablePluginDiscovery()
        {
            _pluginProbe = false;
            return this;
        }

        public RobotBuilder DisableScriptDiscovery()
        {
            _scriptProbe = false;
            return this;
        }

        public RobotBuilder EnableScriptWatcher()
        {
            _watch = true;
            return this;
        }

        public RobotBuilder WithName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }
            _name = name;
            return this;
        }

        public RobotBuilder WithPluginLocator(IRobotPluginLocator pluginLocator)
        {
            _pluginLocator = pluginLocator;
            return this;
        }

        public RobotBuilder WithConfiguration(IDictionary<string, string> config)
        {
            _config = config;
            return this;
        }

        public RobotBuilder UseWorkingDirectory(string path)
        {
            _workingDirectory = path;
            return this;
        }

        public RobotBuilder UseAdapter<TAdapter>() where TAdapter : IAdapter
        {
            _adapterTypes.Add(typeof(TAdapter));
            return this;
        }

        public RobotBuilder UseScriptStore<TScriptStore>() where TScriptStore : IScriptStore
        {
            _scriptStoreType = typeof(TScriptStore);
            return this;
        }

        public RobotBuilder UseScriptRunner<TScriptRunner>() where TScriptRunner : IScriptRunner
        {
            _scriptRunnerType = typeof (TScriptRunner);
            return this;
        }

        public RobotBuilder UseAdapters(IEnumerable<Type> adapterTypes)
        {
            var types = adapterTypes as Type[] ?? adapterTypes.ToArray();
            if (types.Any(t => !typeof(IAdapter).IsAssignableFrom(t)))
            {
                throw new ArgumentException("The type(s) {0} do not implement IAdapter", string.Join(", ", types.Where(t => !typeof(Adapter).IsAssignableFrom(t)).Select(t => t.FullName)));
            }
            _adapterTypes.AddRange(types);
            return this;
        }

        public RobotBuilder UseRouter<TRouter>() where TRouter : IRouter
        {
            _routerType = typeof(TRouter);
            return this;
        }

        public RobotBuilder UseRouter(Type routerType) 
        {
            if (!typeof (IRouter).IsAssignableFrom(routerType))
            {
                throw new ArgumentException(string.Format("The type '{0}' does not implement IRouter", routerType));
            }
            _routerType = routerType;
            return this;
        }

        public RobotBuilder UseBrain<TBrain>() where TBrain : IBrain
        {
            _brainType = typeof (TBrain);
            return this;
        }

        public RobotBuilder UseBrain(Type brainType)
        {
            if (!typeof(IRouter).IsAssignableFrom(brainType))
            {
                throw new ArgumentException(string.Format("The type '{0}' does not implement IBrain", brainType));
            }
            _brainType = brainType;
            return this;
        }
    }

    // This may cause issues in the way we deal with scriptcs. 
    // It wants a script file or a script text, not sure of the implications of each right now
}