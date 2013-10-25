using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Common.Logging;
using Common.Logging.Simple;
using MMBot.Adapters;
using MMBot.Scripts;
using ScriptCs.Contracts;
using LogLevel = Common.Logging.LogLevel;

namespace MMBot
{
    public class Robot : IScriptPackContext
    {
        private string _name = "mmbot";
        private Adapter _adapter;
        private Type _adapterType;
        private Brain brain;
        private readonly List<IListener> _listeners = new List<IListener>();
        private readonly Dictionary<string, Action> _cleanup = new Dictionary<string, Action>();
        private readonly List<string> _helpCommands = new List<string>();
        private readonly List<Type> _loadedScriptTypes = new List<Type>();
        private IDictionary<string, string> _config;
        private Brain _brain;
        protected bool _isConfigured = false;
        private bool _isReady = false;
        private ScriptRunner _scriptRunner;
        private IContainer _container;
        public ILog Logger { get; private set; }

        public Adapter Adapter
        {
            get { return _adapter; }
        }

        public List<string> HelpCommands
        {
            get { return _helpCommands; }
        }

        public string Alias { get; set; }

        public string ScriptPath { get; set; }

        public string Name
        {
            get { return _name; }
        }

        public Brain Brain
        {
            get { return _brain; }
        }

        public static Robot Create<TAdapter>() where TAdapter : Adapter
        {
            return Create<TAdapter>("mmbot", null, null);
        }

        public static Robot Create<TAdapter>(string name, IDictionary<string, string> config) where TAdapter : Adapter
        {
            return Create<TAdapter>(name, config, null);
        }
        
        public static Robot Create<TAdapter>(string name, IDictionary<string, string> config, ILog logger) where TAdapter : Adapter
        {
            var robot = new Robot(logger ?? new TraceLogger(false, "trace", LogLevel.Error, true, false, false, "F"));

            robot.Configure<TAdapter>(name, config);

            robot.LoadAdapter();

            return robot;
        }

        protected Robot() : this(new TraceLogger(false, "default", LogLevel.Error, true, false, false, "F"))
        {
        }

        protected Robot(ILog logger)
        {
            Logger = logger;
        }

        public void Configure<TAdapter>(string name = "mmbot", IDictionary<string, string> config = null) where TAdapter : Adapter
        {
            _adapterType = typeof(TAdapter);
            _scriptRunner = Container.Resolve<ScriptRunner>();
            _brain = Container.Resolve<Brain>();
            _name = name;
            _config = config;
            
            _isConfigured = true;

            _brain.Initialize();
            _scriptRunner.Initialize();
        }

        public void Hear(Regex regex, Action<Response<TextMessage>> action)
        {

        }

        public void Respond(Regex regex, Action<IResponse<TextMessage>> action)
        {

        }

        public void Respond(string regex, Action<IResponse<TextMessage>> action)
        {
            regex = string.Format("^[@]?{0}[:,]?\\s*(?:{1})", _name, regex);

            _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action)
            {
                Source = _currentScriptSource
            });
        }

        public void AddHelp(params string[] helpMessages)
        {
            _helpCommands.AddRange(helpMessages.Except(_helpCommands).ToArray());
        }

        //public void Respond(string regex, Func<IResponse<TextMessage>, Task> action)
        //{
        //    regex = string.Format("^[@]?{0}[:,]?\\s*(?:{1})", _name, regex);

        //    _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), a => action(a)));
        //}

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

        protected IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance<ILog>(Logger);
            builder.RegisterInstance<Robot>(this);
            builder.RegisterType<ScriptRunner>();
            builder.RegisterType<Brain>();
            builder.RegisterType(_adapterType);
            return builder.Build();
        }

        public void Enter(Action<Response<EnterMessage>> action)
        {

        }

        public void Leave(Action<Response<LeaveMessage>> action)
        {

        }

        public void Topic(Action<Response<TopicMessage>> action)
        {

        }

        public void CatchAll(Action<Response<CatchAllMessage>> action)
        {

        }

        private ScriptSource _currentScriptSource = null;

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

        private void CleanupScript(string name)
        {
            if (_cleanup.ContainsKey(name))
            {
                try
                {
                    _cleanup[name]();
                }
                catch (Exception)
                {
                }
                finally
                {
                    _cleanup.Remove(name);
                }
            }
        }

        public virtual async Task Run()
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }
            LoadScripts(Path.Combine(Environment.CurrentDirectory, "scripts"));
            await _adapter.Run();
            _isReady = true;
        }

        public void Receive(Message message)
        {
            if (!_isReady)
            {
                return;
            }
            SynchronizationContext.SetSynchronizationContext(new AsyncSynchronizationContext());
            foreach (var listener in _listeners)
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

        public void LoadAdapter()
        {
            _adapter = Container.Resolve(_adapterType) as Adapter;
        }

        public void LoadScripts(Assembly assembly)
        {
            assembly.GetTypes().Where(t => typeof(IMMBotScript).IsAssignableFrom(t) && t.IsClass && !t.IsGenericTypeDefinition && !t.IsAbstract && t.GetConstructors().Any(c => !c.GetParameters().Any())).ForEach(s =>
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

        }

        public void LoadScript<TScript>() where TScript : IMMBotScript, new()
        {
            using(StartScriptProcessingSession(new ScriptSource(typeof(TScript).Name, typeof(TScript).AssemblyQualifiedName)))
            {
                var script = new TScript();
                RegisterScript(script);
                if (!_loadedScriptTypes.Contains(typeof(TScript)))
                {
                    _loadedScriptTypes.Add(typeof(TScript));
                }
            }
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

        public string GetConfigVariable(string name)
        {
            if (!_isConfigured)
            {
                throw new RobotNotConfiguredException();
            }
            return _config.ContainsKey(name) ? _config[name] : Environment.GetEnvironmentVariable(name);
        }

        private void RegisterScript(IMMBotScript script)
        {
            script.Register(this);


            HelpCommands.AddRange(script.GetHelp());
        }

        public async Task Shutdown()
        {
            _isReady = false;
            _cleanup.Keys.ToList().ForEach(CleanupScript);
            if (_adapter != null)
            {
                await _adapter.Close();
            }
            if (_brain != null)
            {
                await _brain.Close();
            }
        }

        public async Task Reset()
        {
            await Shutdown();
            LoadAdapter();

            _loadedScriptTypes.ForEach(LoadScript);
            await Run();
            _brain.Initialize();
        }

        

        public void RegisterCleanup(Action cleanup)
        {
            if(_currentScriptSource != null)
            {
                _cleanup[_currentScriptSource.Name] =  cleanup;
            }
        }
    }

    public class ScriptProcessingException : Exception
    {
        public ScriptProcessingException(string message) : base(message)
        {
        }

        public ScriptProcessingException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public class ScriptSource
    {
        public ScriptSource(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// This is the unique name for the script source. Scripts will overwrite previous entries with the same name.
        /// </summary>
        public string Name { get; set; }
        
        public string Description { get; set; }
    }
}