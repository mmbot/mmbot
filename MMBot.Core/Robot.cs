using Autofac;
using Common.Logging;
using Common.Logging.Simple;
using MMBot.Brains;
using MMBot.Router;
using MMBot.Scripts;
using ScriptCs.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LogLevel = Common.Logging.LogLevel;

namespace MMBot
{
    public class Robot : IScriptPackContext
    {
        public readonly List<ScriptMetadata> ScriptData = new List<ScriptMetadata>();
        protected bool _isConfigured = false;
        private readonly Dictionary<string, Adapter> _adapters = new Dictionary<string, Adapter>();
        private readonly Dictionary<string, Action> _cleanup = new Dictionary<string, Action>();
        private readonly List<IListener> _listeners = new List<IListener>();
        private readonly List<Type> _loadedScriptTypes = new List<Type>();
        private IEnumerable<Type> _adapterTypes;
        private string[] _admins;
        private IBrain _brain;
        private IDictionary<string, string> _config;
        private IContainer _container;
        private ScriptSource _currentScriptSource = null;
        private Dictionary<string, EventEmitItem> _emitTable = new Dictionary<string, EventEmitItem>();
        private bool _isReady = false;
        private string _name = "mmbot";
        private IRouter _router = new NullRouter();
        private ScriptRunner _scriptRunner;

        public static Robot Create<TAdapter>() where TAdapter : Adapter
        {
            return Create<TAdapter>("mmbot", null, null);
        }

        public static Robot Create<TAdapter>(string name, IDictionary<string, string> config) where TAdapter : Adapter
        {
            return Create<TAdapter>(name, config, null);
        }

        public static Robot Create<TAdapter>(string name, IDictionary<string, string> config, LoggerConfigurator logConfig) where TAdapter : Adapter
        {
            return Create(name, config, logConfig, new Type[] { typeof(TAdapter) });
        }

        public static Robot Create(string name, IDictionary<string, string> config, LoggerConfigurator logConfig, params Type[] adapterTypes)
        {
            var robot = new Robot(logConfig);

            robot.Configure(name, config, adapterTypes);

            robot.LoadAdapter();

            return robot;
        }

        protected Robot()
            : this(null)
        {
        }

        protected Robot(LoggerConfigurator logConfig)
        {
            LogConfig = logConfig;
            Logger = logConfig == null
                ? new TraceLogger(false, "trace", LogLevel.Error, true, false, false, "F")
                : logConfig.GetLogger();
            AutoLoadScripts = true;
        }

        public Dictionary<string, Adapter> Adapters
        {
            get { return _adapters; }
        }

        public string[] Admins
        {
            get
            {
                return _admins ?? (_admins = (GetConfigVariable("MMBOT_AUTH_ADMIN") ?? string.Empty)
                    .Trim()
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Union(new string[] { "ConsoleUser" }).ToArray());
            }
        }

        public string Alias { get; set; }

        public bool AutoLoadScripts { get; set; }

        public IBrain Brain
        {
            get { return _brain; }
        }

        public IContainer Container
        {
            get
            {
                if (_container == null)
                {
                    _container = CreateContainer();
                }

                return _container;
            }
        }

        public string[] Emitters
        {
            get { return _emitTable.Keys.ToArray(); }
        }

        public List<string> HelpCommands
        {
            get { return ScriptData.SelectMany(d => d.Commands).Where(d => d.HasValue()).ToList(); }
        }

        public LoggerConfigurator LogConfig { get; private set; }

        public ILog Logger { get; private set; }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public IRouter Router
        {
            get { return _router; }
        }

        public string ScriptPath { get; set; }

        public void CatchAll(Action<IResponse<CatchAllMessage>> action)
        {
            _listeners.Add(new CatchAllListener(this, action)
            {
                Source = _currentScriptSource
            });
        }

        public void Enter(Action<IResponse<EnterMessage>> action)
        {
            _listeners.Add(new RosterListener(this, action)
            {
                Source = _currentScriptSource
            });
        }

        public void Hear(string regex, Action<IResponse<TextMessage>> action)
        {
            regex = PrepareHearRegexPattern(regex);

            _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action)
            {
                Source = _currentScriptSource
            });
        }

        public void Leave(Action<IResponse<LeaveMessage>> action)
        {
            _listeners.Add(new RosterListener(this, action)
            {
                Source = _currentScriptSource
            });
        }

        public void Receive(Message message)
        {
            if (!_isReady)
            {
                return;
            }
            SynchronizationContext.SetSynchronizationContext(new AsyncSynchronizationContext());
            foreach (var listener in _listeners.ToArray()) //  need to copy collection so as not to be affectied by a script modifying it
            {
                try
                {
                    listener.Call(message);
                    if (message.Done)
                    {
                        break;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error receiving message", e);
                    // TODO: Logging exception in listener
                }
            }
        }

        public void Respond(string regex, Action<IResponse<TextMessage>> action)
        {
            regex = PrepareRespondRegexPattern(regex);

            _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action)
            {
                Source = _currentScriptSource
            });
        }

        public async void Speak(string room, params string[] messages)
        {
            foreach (
                var adapter in
                    _adapters.Where(a => a.Value.Rooms.Contains(room, StringComparer.InvariantCultureIgnoreCase)))
            {
                await adapter.Value.Send(
                    new Envelope(new TextMessage(this.GetUser(_name, _name, room, adapter.Key),
                        string.Join(Environment.NewLine, messages))), messages);
            }
        }

        public async void Speak(string adapterId, string room, params string[] messages)
        {
            await Adapters[adapterId].Send(
                    new Envelope(new TextMessage(this.GetUser(_name, _name, room, adapterId),
                        string.Join(Environment.NewLine, messages))), messages);
        }

        public void Topic(Action<IResponse<TopicMessage>> action)
        {
            _listeners.Add(new TopicListener(this, action)
            {
                Source = _currentScriptSource
            });
        }

        public void AddHelp(params string[] helpMessages)
        {
            if (!ScriptData.Any(d => d.Name == "UnReferenced"))
                ScriptData.Add(new ScriptMetadata() { Name = "UnReferenced", Description = "Commands not referenced in a script file's summary details" });
            var unreferencedHelpCommands = ScriptData.Where(d => d.Name == "UnReferenced").First();

            unreferencedHelpCommands.Commands.AddRange(helpMessages.Except(unreferencedHelpCommands.Commands).ToArray());
        }

        public void AddMetadata(ScriptMetadata metadata)
        {
            ScriptData.Add(metadata);
        }

        public void Configure(string name = "mmbot", IDictionary<string, string> config = null, params Type[] adapterTypes)
        {
            _adapterTypes = adapterTypes;
            _scriptRunner = Container.Resolve<ScriptRunner>();
            _name = name;
            _config = config ?? new Dictionary<string, string>();

            _isConfigured = true;

            _scriptRunner.Initialize();
        }

        public void ConfigureRouter(Type routerType)
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }

            if (!typeof(IRouter).IsAssignableFrom(routerType))
            {
                throw new TypeLoadException(string.Format("Could not configure router type '{0}' as it does not implement IRouter", routerType));
            }

            var router = Activator.CreateInstance(routerType) as IRouter;
            router.Configure(this, int.Parse(GetConfigVariable("MMBOT_ROUTER_PORT") ?? "80"));
            _router = router;
        }

        public void ConfigureBrain(Type brainType)
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }

            if (!typeof(IBrain).IsAssignableFrom(brainType))
            {
                throw new TypeLoadException(string.Format("Could not configure brain type '{0}' as it does not implement IBrain", brainType));
            }

            IBrain brain = Activator.CreateInstance(brainType) as IBrain;
            brain.Initialize(this);
            _brain = brain;
        }

        public string GetConfigVariable(string name)
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }
            return _config.ContainsKey(name) ? _config[name] : Environment.GetEnvironmentVariable(name);
        }

        public void LoadAdapter()
        {
            _adapters.Clear();
            foreach (var adapterType in _adapterTypes.Distinct(new GenericEqualityComparer<Type>((t1, t2) => t1.FullName == t2.FullName, type => type.FullName.GetHashCode())))
            {
                Logger.Info(string.Format("Loading Adapter '{0}'", adapterType.Name));
                try
                {
                    var id = adapterType.Name;
                    int count = 0;
                    while (_adapters.Keys.Contains(id))
                    {
                        count++;
                        id = adapterType.Name + count;
                    }
                    var adapter = Container.Resolve(adapterType, new NamedParameter("adapterId", id)) as Adapter;

                    _adapters.Add(id, adapter);
                }
                catch (Exception e)
                {
                    Logger.Error(string.Format("Could not instantiate '{0}' adapter", adapterType.Name), e);
                }
            }
        }

        public void LoadLogging()
        {
            if (LogConfig == null || LogConfig.GetAppenders().Any(d => d == "MMBot.RobotLogAppender"))
                return;

            if (Adapters.Values.Any(d => d.LogRooms.Any()))
            {
                LogConfig.ConfigureForRobot(this);
            }
            else
            {
                Logger.Info("No logging rooms to enabled");
            }
        }

        public void LoadScript<TScript>() where TScript : IMMBotScript, new()
        {
            using (StartScriptProcessingSession(new ScriptSource(typeof(TScript).Name, typeof(TScript).AssemblyQualifiedName)))
            {
                Logger.Info(string.Format("Loading script '{0}'", Path.GetFileNameWithoutExtension(typeof(TScript).Name)));

                var script = new TScript();
                RegisterScript(script);
                if (!_loadedScriptTypes.Contains(typeof(TScript)))
                {
                    _loadedScriptTypes.Add(typeof(TScript));
                }
            }
        }

        public void LoadScriptFile(string scriptFile)
        {
            try
            {
                string scriptFileName = Path.GetFileName(scriptFile);
                string scriptName = Path.GetFileNameWithoutExtension(scriptFile);

                Logger.Info(string.Format("Loading script '{0}'", scriptFileName));
                using (StartScriptProcessingSession(new ScriptSource(scriptName, scriptFile)))
                {
                    _scriptRunner.RunScriptFile(scriptFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void LoadScriptFile(string scriptName, string scriptFile)
        {
            Logger.Info(string.Format("Loading script '{0}'", Path.GetFileNameWithoutExtension(scriptName)));
            using (StartScriptProcessingSession(new ScriptSource(scriptName, scriptFile)))
            {
                _scriptRunner.RunScriptFile(scriptFile);
            }
        }

        public void LoadScriptName(string ScriptName)
        {
            string filePath = Path.Combine(Environment.CurrentDirectory, "scripts", ScriptName.EndsWith(".csx") ? ScriptName : ScriptName + ".csx");
            if (File.Exists(filePath))
                LoadScriptFile(filePath);
        }

        public void LoadScripts(Assembly assembly)
        {
            LoadScripts(assembly.GetTypes());
        }

        public void LoadScripts(IEnumerable<Type> scriptTypes)
        {
            scriptTypes.Where(t => typeof(IMMBotScript).IsAssignableFrom(t) && t.IsClass && !t.IsGenericTypeDefinition && !t.IsAbstract && t.GetConstructors().Any(c => !c.GetParameters().Any())).ForEach(s =>
            {
                Logger.Info(string.Format("Loading script {0}", s.Name));

                LoadScript(s);
            });
        }

        public void LoadScripts(string path)
        {
            if (!Directory.Exists(path))
            {
                Logger.Warn(string.Format("Script directory '{0}' does not exist", path));
                return;
            }

            foreach (var scriptFile in Directory.GetFiles(path, "*.csx"))
            {
                LoadScriptFile(scriptFile);
            }
        }

        public IDisposable StartScriptProcessingSession(ScriptSource scriptSource)
        {
            if (scriptSource == null)
            {
                throw new ArgumentNullException("scriptSource");
            }

            if (_currentScriptSource != null)
            {
                throw new ScriptProcessingException("Cannot process multiple script sources at the same time");
            }
            _currentScriptSource = scriptSource;

            CleanupScript(scriptSource.Name);

            _listeners.RemoveAll(l => l.Source != null && l.Source.Name == scriptSource.Name);

            return Disposable.Create(() => _currentScriptSource = null);
        }

        private void LoadScript(Type scriptType)
        {
            using (StartScriptProcessingSession(new ScriptSource(scriptType.Name, scriptType.AssemblyQualifiedName)))
            {
                var script = (Activator.CreateInstance(scriptType) as IMMBotScript);
                RegisterScript(script);
                if (!_loadedScriptTypes.Contains(scriptType))
                {
                    _loadedScriptTypes.Add(scriptType);
                }
            }
        }

        public void Emit<T>(string key, T data)
        {
            if (_emitTable.ContainsKey(key))
                _emitTable[key].Raise(data);
        }

        public void On<T>(string key, Action<T> action)
        {
            if (!_emitTable.ContainsKey(key))
            {
                _emitTable.Add(key, new EventEmitItem());
            }

            _emitTable[key].Emitted += delegate(object o, EventArgs e) { action((T)o); };
        }

        public void RegisterCleanup(Action cleanup)
        {
            if (_currentScriptSource != null)
            {
                _cleanup[_currentScriptSource.Name] = cleanup;
            }
        }

        public void RemoveListener(string regexPattern)
        {
            string actualRegex = PrepareRespondRegexPattern(regexPattern);
            _listeners.RemoveAll(l => l is TextListener && ((TextListener)l).RegexPattern.ToString() == actualRegex);
        }

        public async Task Reset()
        {
            Emit("Resetting", true);
            HardReset();
            Emit("ResetComplete", true);
        }

        public virtual async Task Run()
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }

            if (AutoLoadScripts)
            {
                LoadScripts(Path.Combine(Environment.CurrentDirectory, "scripts"));
                Emit("ScriptsLoaded", this.ScriptData.Select(d => d.Name));
            }

            try
            {
                _router.Start();
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Could not start router '{0}'", _router.GetType().Name), e);
            }

            foreach (var adapter in _adapters.Values)
            {
                try
                {
                    await adapter.Run();
                    Emit("AdapterRunning", adapter.Id);
                }
                catch (AdapterNotConfiguredException)
                {
                    Logger.WarnFormat("The adapter '{0}' is not configured and will not be loaded",
                        adapter.GetType().Name);
                }
                catch (Exception e)
                {
                    Logger.ErrorFormat("Could not run the adapter '{0}': {1}", e, adapter.GetType().Name, e.Message);
                }
            }

            try
            {
                LoadLogging();
            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("Could not enable room logging : {0}", ex.ToString());
            }

            _isReady = true;
            Emit("RobotReady", true);
        }

        public async Task Shutdown()
        {
            Emit("ShuttingDown", true);
            _isReady = false;
            _cleanup.Keys.ToList().ForEach(CleanupScript);
            _cleanup.Clear();
            _listeners.Clear();
            foreach (var adapter in _adapters.Values)
            {
                await adapter.Close();
            }
            if (_brain != null)
            {
                await _brain.Close();
            }
            Emit("ShutdownComplete", true);
        }

        protected IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<ILog>(Logger);
            builder.RegisterInstance<Robot>(this);
            builder.RegisterType<ScriptRunner>();
            _adapterTypes.ForEach(t => builder.RegisterType(t));
            return builder.Build();
        }

        private void CleanupScript(string name)
        {
            if (_cleanup.ContainsKey(name))
            {
                try
                {
                    _cleanup[name]();
                }
                catch (Exception e)
                {
                    Logger.Error("Error during cleanup", e);
                }
                finally
                {
                    _cleanup.Remove(name);
                }
            }
        }

        private string PrepareHearRegexPattern(string regex)
        {
            return string.Format("^(?:{0})", regex);
        }

        private string PrepareRespondRegexPattern(string regex)
        {
            return string.Format("^[@]?{0}[:,]?\\s*(?:{1})", _name, regex);
        }

        private void RegisterScript(IMMBotScript script)
        {
            script.Register(this);

            AddHelp(script.GetHelp().ToArray());
        }

        public void HardReset()
        {
            RaiseHardResetRequest(new EventArgs());
        }

        public event EventHandler HardResetRequested;

        protected virtual void RaiseHardResetRequest(EventArgs e)
        {
            EventHandler handler = HardResetRequested;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private class EventEmitItem
        {
            public event EventHandler Emitted;

            public void Raise<T>(T data)
            {
                Emitted.Raise(data, null);
            }
        }
    }
}
