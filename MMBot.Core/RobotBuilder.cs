using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MMBot.Brains;
using MMBot.Router;
using MMBot.Scripts;
using ScriptCs;

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
        private readonly ContainerBuilder _containerBuilder;
        private IRobotPluginLocator _pluginLocator;
        private Type _scriptStoreType;
        private Type _scriptRunnerType;

        protected RobotBuilder()
        {
            _containerBuilder = new ContainerBuilder();
        }

        public RobotBuilder(LoggerConfigurator logConfig) : this()
        {
            _logConfig = logConfig;
        }

        public ContainerBuilder ContainerBuilder
        {
            get { return _containerBuilder; }
        }

        public Robot Build()
        {
            return Build(c => { });
        }

        public Robot Build(Action<ContainerBuilder> preBuild)
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

            ContainerBuilder.RegisterType<Robot>().SingleInstance();
            ContainerBuilder.RegisterInstance(robotPluginLocator).As<IRobotPluginLocator>();
            ContainerBuilder.RegisterType(_brainType ?? typeof(AkavacheBrain)).As<IBrain>().SingleInstance();
            ContainerBuilder.RegisterType(_scriptStoreType ?? typeof(LocalScriptStore)).As<IScriptStore>().SingleInstance();
            ContainerBuilder.RegisterType(_scriptRunnerType ?? typeof(ScriptRunner)).As<IScriptRunner>().SingleInstance();
            ContainerBuilder.RegisterType(_routerType ?? typeof(NullRouter)).As<IRouter>().SingleInstance();
            ContainerBuilder.RegisterTypes(_adapterTypes.ToArray());
            ContainerBuilder.RegisterInstance(_logConfig);
            ContainerBuilder.Register(context => context.Resolve<LoggerConfigurator>().GetLogger());
            ContainerBuilder.RegisterInstance(fileSystem);

            preBuild(ContainerBuilder);

            var container = ContainerBuilder.Build();

            var adapters = _adapterTypes.Select(a => container.Resolve(a, new NamedParameter("adapterId", a.Name))).ToDictionary(a => a.GetType().Name, a => a as IAdapter);

            var robot = container.Resolve<Robot>(
                new NamedParameter("name", _name ?? _config.GetValueOrDefault("MMBOT_ROBOT_NAME") ?? "mmbot"), 
                new NamedParameter("config", _config),
                new NamedParameter("adapters", adapters));

            robot.AutoLoadScripts = _scriptProbe;

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