using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Common.Logging;
using MMBot.Brains;
using MMBot.Router;
using ScriptCs;

namespace MMBot
{
    public class RobotBuilder
    {
        private readonly LoggerConfigurator _logConfig;
        private List<Type> _adapterTypes = new List<Type>();
        private Type _routerType = typeof(NullRouter);
        private Type _brainType;
        private bool _probe = true;
        private string _workingDirectory;
        private string _name = "mmbot";
        private IDictionary<string, string> _config = new Dictionary<string, string>();
        private readonly ContainerBuilder _containerBuilder;
        private IRobotPluginLocator _pluginLocater;
        private Type _scriptStoreType;

        protected RobotBuilder()
        {
            _containerBuilder = new ContainerBuilder();
        }

        public RobotBuilder(LoggerConfigurator logConfig)
        {
            _logConfig = logConfig;
        }

        public ContainerBuilder ContainerBuilder
        {
            get { return _containerBuilder; }
        }

        public Robot Build()
        {
            if (_probe)
            {
                // Probe for types
                var robotPluginLocator = (_pluginLocater ?? new NuGetPackageAssemblyResolver(_logConfig));

                _adapterTypes.AddRange(robotPluginLocator.GetAdapters());
                _brainType = robotPluginLocator.GetBrain(_config["MMBOT_BRAIN_NAME"]);
                _routerType = robotPluginLocator.GetRouter(_config["MMBOT_ROUTER_NAME"]);
            }

            var fileSystem = new FileSystem();
            if(string.IsNullOrEmpty(_workingDirectory))
            {
                fileSystem.CurrentDirectory = _workingDirectory;
            }

            ContainerBuilder.RegisterType<Robot>().SingleInstance();
            ContainerBuilder.RegisterType(_brainType ?? typeof(AkavacheBrain)).SingleInstance();
            ContainerBuilder.RegisterType(_scriptStoreType ?? typeof(LocalScriptStore)).SingleInstance();
            ContainerBuilder.RegisterType(_routerType).SingleInstance();
            ContainerBuilder.RegisterTypes(_adapterTypes.ToArray());
            ContainerBuilder.RegisterInstance(_logConfig);
            ContainerBuilder.RegisterInstance(fileSystem);
            
            var container = ContainerBuilder.Build();

            return container.Resolve<Robot>(new NamedParameter("name", _name), new NamedParameter("config", _config));
        }

        public RobotBuilder DisableProbing()
        {
            _probe = false;
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
            _pluginLocater = pluginLocator;
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

        public RobotBuilder UseScriptStore<TScriptStore>()
        {
            _scriptStoreType = typeof(TScriptStore);
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

    public interface IRobotPluginLocator
    {
        Type[] GetAdapters();

        Type GetBrain(string name);

        Type GetRouter(string name);
    }


    public interface IRobotConfiguration
    {
        string Get(string key);
    }

    // This may cause issues in the way we deal with scriptcs. 
    // It wants a script file or a script text, not sure of the implications of each right now
    public interface IScriptStore
    {
        IEnumerable<IScript> GetAllScripts();
        
        Task<IScript> SaveScript(string name, string contents);

        IObservable<IScript> ScriptUpdated { get; }
    }

    public class LocalScriptStore : IScriptStore
    {
        private readonly FileSystem _fileSystem;
        private Subject<IScript> _scriptUpdated;
        private ILog _log;

        public LocalScriptStore(LoggerConfigurator logConfig, FileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            _scriptUpdated = new Subject<IScript>();
            _log = logConfig.GetLogger();
        }

        private string ScriptsPath
        {
            get { return Path.Combine(_fileSystem.CurrentDirectory, "scripts"); }
        }

        public IEnumerable<IScript> GetAllScripts()
        {
            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "scripts")))
            {
                _log.Warn(
                    "There is no scripts folder. Have you forgotten to run 'mmbot --init' to initialise the current running directory?");
            }

            return Directory.GetFiles(ScriptsPath, "*.csx").Select(scriptFile => new ScriptCsScriptFile()
            {
                Name = Path.GetFileNameWithoutExtension(scriptFile),
                Path = scriptFile
            });
        }

        public Task<IScript> SaveScript(string name, string contents)
        {
            var path = Path.Combine(ScriptsPath, string.Concat(name, ".csx"));
            return Task.Run(() =>
            {
                File.WriteAllText(path, contents, Encoding.UTF8);
                return new ScriptCsScriptFile()
                {
                    Name = Path.GetFileNameWithoutExtension(name),
                    Path = path
                } as IScript;
            });
        }

        public IObservable<IScript> ScriptUpdated
        {
            get { return _scriptUpdated; }
        } 
    }

    public interface IScript
    {
        string Name { get; set; }
    }

    public class ScriptCsScriptFile : IScript
    {
        public string Name { get; set; }

        public string Path { get; set; }

        protected bool Equals(ScriptCsScriptFile other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScriptCsScriptFile)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Path != null ? Path.GetHashCode() : 0);
            }
        }

    }


    public interface IMustBeInitializedWithRobot
    {
        void Initialize(Robot robot);
    }
}