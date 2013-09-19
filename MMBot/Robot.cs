using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using Microsoft.Win32;
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

        public static Robot Create<TAdapter>(string name = "mmbot") where TAdapter : Adapter
        {
            var robot = new Robot(typeof(TAdapter), name);
            robot.LoadAdapter();
            return robot;
        }

        private Robot(Type adapterType, string name)
        {
            _adapterType = adapterType;
            _name = name;
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

        public void Run()
        {
            _adapter.Run();
        }

        public void Receive(Message message)
        {
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

        public void Configure(Dictionary<string, string> config)
        {
            Adapter.Configure(config);
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

        private void RegisterScript(IMMBotScript script)
        {
            script.Register(this);
            HelpCommands.AddRange(script.GetHelp());
        }
    }

    public class Listener<T> : IListener
    {
        private readonly Robot _robot;
        private readonly Func<Message, MatchResult> _matcher;
        private readonly Action<IResponse<Message>> _callback;

        protected Listener()
        {

        }

        public Listener(Robot robot, Func<Message, MatchResult> matcher, Action<IResponse<Message>> callback)
        {
            _robot = robot;
            _matcher = matcher;
            _callback = callback;
        }

        public virtual bool Call(Message message)
        {
            MatchResult matchResult = _matcher(message);
            if (matchResult.IsMatch)
            {
                // TODO: Log
                //@robot.logger.debug \
                //  "Message '#{message}' matched regex /#{inspect @regex}/" if @regex

                _callback(Response.Create(_robot, message, matchResult));
                return true;
            }
            return false;
        }
    }

    public interface IListener
    {
        bool Call(Message message);
    }

    public class TextListener : IListener
    {
        private readonly Robot _robot;
        private readonly Regex _regex;
        private readonly Action<IResponse<TextMessage>> _callback;

        public TextListener(Robot robot, Regex regex, Action<IResponse<TextMessage>> callback)
        {
            _robot = robot;
            _regex = regex;
            _callback = callback;
        }

        private static MatchResult Match(Regex regex, Message message)
        {
            if (!(message is TextMessage))
            {
                return new MatchResult(false);
            }
            var match = regex.Matches(((TextMessage) message).Text);
            return match.Cast<Match>().Any(m => m.Success) 
                ? new MatchResult(true, match) 
                : new MatchResult(false);
        }

        public bool Call(Message message)
        {
            if (!(message is TextMessage))
            {
                return false;
            }
            var match = Match(_regex, message);
            if (match.IsMatch)
            {
                // TODO: Log
                //@robot.logger.debug \
                //  "Message '#{message}' matched regex /#{inspect @regex}/" if @regex

                _callback(Response.Create(_robot, message as TextMessage, match));
                return true;
            }
            return false;
        }
    }

    public class MatchResult
    {

        public MatchResult(bool isMatch, MatchCollection match = null)
        {
            IsMatch = isMatch;
            Match = match;
        }

        public bool IsMatch { get; private set; }
        public MatchCollection Match { get; private set; }
    }
}