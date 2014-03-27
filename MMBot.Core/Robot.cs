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
        private readonly IDictionary<string, IAdapter> _adapters = new Dictionary<string, IAdapter>();
        private readonly List<IListener> _listeners = new List<IListener>();
        private readonly List<Type> _loadedScriptTypes = new List<Type>();
        private string[] _admins;
        private IBrain _brain;
        private IDictionary<string, string> _config;
        private IContainer _container;
        private Dictionary<string, EventEmitItem> _emitTable = new Dictionary<string, EventEmitItem>();
        private bool _isReady = false;
        private string _name = "mmbot";
        private IRouter _router = new NullRouter();
        private readonly IScriptRunner _scriptRunner;
        private readonly IScriptStore _scriptStore;

        public Robot(string name, IDictionary<string, string> config, LoggerConfigurator logConfig, IDictionary<string, IAdapter> adapters, IRouter router, IBrain brain, IScriptStore scriptStore, IScriptRunner scriptRunner)
            : this(logConfig)
        {
            _name = name;
            _config = config;
            _scriptStore = scriptStore;
            _adapters = adapters;
            _brain = brain;
            _router = router;
            _scriptRunner = scriptRunner;
            _isConfigured = true;
            Initialize(adapters.Values.ToArray().Concat(new object[] { router, brain, scriptRunner }).ToArray());
        }

        protected Robot(LoggerConfigurator logConfig)
        {
            LogConfig = logConfig;
            Logger = logConfig == null
                ? new TraceLogger(false, "trace", LogLevel.Error, true, false, false, "F")
                : logConfig.GetLogger();
            AutoLoadScripts = true;
        }

        private void Initialize(params object[] dependencies)
        {
            foreach (var dep in dependencies.OfType<IMustBeInitializedWithRobot>())
            {
                dep.Initialize(this);
            }

            _router.Configure(int.Parse(GetConfigVariable("MMBOT_ROUTER_PORT") ?? "80"));
        }

        public IDictionary<string, IAdapter> Adapters
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

        public List<IListener> Listeners
        {
            get { return _listeners; }
        }

        public void CatchAll(Action<IResponse<CatchAllMessage>> action)
        {
            Listeners.Add(new CatchAllListener(this, action)
            {
                Source = _scriptRunner.CurrentScriptSource
            });
        }

        public void Enter(Action<IResponse<EnterMessage>> action)
        {
            Listeners.Add(new RosterListener(this, action)
            {
                Source = _scriptRunner.CurrentScriptSource
            });
        }

        public void Hear(string regex, Action<IResponse<TextMessage>> action)
        {
            regex = PrepareHearRegexPattern(regex);

            Listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action)
            {
                Source = _scriptRunner.CurrentScriptSource
            });
        }

        public void Leave(Action<IResponse<LeaveMessage>> action)
        {
            Listeners.Add(new RosterListener(this, action)
            {
                Source = _scriptRunner.CurrentScriptSource
            });
        }

        public void Receive(Message message)
        {
            if (!_isReady)
            {
                return;
            }
            SynchronizationContext.SetSynchronizationContext(new AsyncSynchronizationContext());
            foreach (var listener in Listeners.ToArray()) //  need to copy collection so as not to be affectied by a script modifying it
            {
                try
                {
                    listener.Call(message);
                    if (message.Done)
                    {
                        break;
                    }
                }
                catch (TaskCanceledException e)
                {
                    //Don't care.
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

            Listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action)
            {
                Source = _scriptRunner.CurrentScriptSource
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
            Listeners.Add(new TopicListener(this, action)
            {
                Source = _scriptRunner.CurrentScriptSource
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


        public string GetConfigVariable(string name)
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }
            return _config.ContainsKey(name) ? _config[name] : Environment.GetEnvironmentVariable(name);
        }

        private void LoadLogging()
        {
            if (LogConfig == null || LogConfig.GetAppenders().Any(d => d == "MMBot.RobotLogAppender"))
                return;

            if (Adapters.Values.Any(d => d.LogRooms.Any()))
            {
                LogConfig.ConfigureForRobot(this);
            }
            else
            {
                Logger.Info("No logging rooms are enabled");
            }
        }

        public void LoadScript<TScript>() where TScript : IMMBotScript, new()
        {
            _scriptRunner.RunScript(TypedScript.Create<TScript>());
        }

        public void LoadScriptFile(string scriptFile)
        {
            _scriptRunner.RunScript(_scriptStore.GetScriptByPath(scriptFile));
        }

        public void LoadScriptFile(string scriptName, string scriptFile)
        {
            try
            {
                var script = _scriptStore.GetScriptByPath(scriptFile);
                script.Name = scriptName;
                _scriptRunner.RunScript(script);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void LoadScriptName(string scriptName)
        {
            _scriptRunner.RunScript(_scriptStore.GetScriptByName(scriptName));
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

        private void LoadScript(Type scriptType)
        {
            _scriptRunner.RunScript(TypedScript.Create(scriptType));
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
            _scriptRunner.RegisterCleanup(cleanup);
        }

        public void RemoveListener(string regexPattern)
        {
            string actualRegex = PrepareRespondRegexPattern(regexPattern);
            Listeners.RemoveAll(l => l is TextListener && ((TextListener)l).RegexPattern.ToString() == actualRegex);
        }

        public async Task Reset()
        {
            Emit("Resetting", true);
            RaiseHardResetRequest(new EventArgs());
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
                foreach (var script in _scriptStore.GetAllScripts())
                {
                    _scriptRunner.RunScript(script);
                }
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
            _scriptRunner.Cleanup();
            Listeners.Clear();
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
            return builder.Build();
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
