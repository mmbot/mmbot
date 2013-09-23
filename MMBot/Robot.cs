using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MMBot.Adapters;
using MMBot.Scripts;

namespace MMBot
{
    public class Robot
    {
        private string _name = "mmbot";
        private Adapter _adapter;
        private Type _adapterType;
        private Brain brain;
        private readonly List<IListener> _listeners = new List<IListener>();
        private readonly List<string> _helpCommands = new List<string>();
        private IDictionary<string, string> _config;
        private Brain _brain;

        public Adapter Adapter {
            get { return _adapter; }
        }

        public List<string> HelpCommands
        {
            get { return _helpCommands; }
        }

        public string Alias { get; set; }
        public string Name {
            get { return _name; }
        }

        public Brain Brain {
            get { return _brain; }
        }

        public static Robot Create<TAdapter>(string name = "mmbot", IDictionary<string, string> config = null) where TAdapter : Adapter
        {
            var robot = new Robot(typeof(TAdapter), name, config);
            robot.LoadAdapter();
            return robot;
        }

        private Robot(Type adapterType, string name, IDictionary<string, string> config)
        {
            _adapterType = adapterType;
            _name = name;
            _config = config;
            _brain = new Brain(this);
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

            _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), action));
        }

        //public void Respond(string regex, Func<IResponse<TextMessage>, Task> action)
        //{
        //    regex = string.Format("^[@]?{0}[:,]?\\s*(?:{1})", _name, regex);

        //    _listeners.Add(new TextListener(this, new Regex(regex, RegexOptions.Compiled | RegexOptions.IgnoreCase), a => action(a)));
        //}

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

        public async Task Run()
        {
            await _brain.Initialize();

            await _adapter.Run();
            
        }

        public void Receive(Message message)
        {
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
                    Console.WriteLine(e);
                    // TODO: Logging exception in listener
                }
                
            }
        }

        public void LoadAdapter()
        {
            _adapter = Activator.CreateInstance(_adapterType, this) as Adapter;
        }
        
        public void LoadScripts(Assembly assembly)
        {
            assembly.GetTypes().Where(t => typeof(IMMBotScript).IsAssignableFrom(t) && t.IsClass && !t.IsGenericTypeDefinition && !t.IsAbstract && t.GetConstructors().Any(c => !c.GetParameters().Any())).ForEach( s =>
            {
                Console.WriteLine("Loading script {0}", s.Name);
                var script = (Activator.CreateInstance(s) as IMMBotScript);
                RegisterScript(script);
            });
        }

        public void LoadScript<TScript>() where TScript : IMMBotScript, new()
        {
            var script = new TScript();
            RegisterScript(script);
        }

        public string GetConfigVariable(string name)
        {
            return _config.ContainsKey(name) ? _config[name] : Environment.GetEnvironmentVariable(name);
        }

        private void RegisterScript(IMMBotScript script)
        {
            script.Register(this);
            

            HelpCommands.AddRange(script.GetHelp());
        }

        public async Task Shutdown()
        {
            if(_adapter != null)
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
            
            await Run();
            await _brain.Initialize();
        }
    }
}