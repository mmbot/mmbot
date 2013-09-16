using System;
using System.Collections.Specialized;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MMBot
{
    public class Robot
    {
        private string _name = "mmbot";
        private IAdapter _adapter;
        private Brain brain;

        public Robot(IAdapter adapter, string name = "mmbot")
        {
            _adapter = adapter;
            _name = name;
        }

        public void Hear(Regex regex, Action<Response<TextMessage>> action)
        {

        }

        public void Respond(Regex regex, Action<Response<TextMessage>> action)
        {

        }

        public void Respond(string regex, Action<Response<TextMessage>> action)
        {

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

        public void Receive(Message message)
        {
            foreach (Listener listener in _listeners)
            {

            }
        }

        public void LoadAdapter()
        {

        }
        
    }

    public class Listener
    {
        private readonly Robot _robot;
        private readonly Func<Message, bool> _matcher;
        private readonly Action<Response<Message>> _callback;

        public Listener(Robot robot, Func<Message, bool> matcher, Action<Response<Message>> callback)
        {
            _robot = robot;
            _matcher = matcher;
            _callback = callback;
        }

        public bool Call(Message message)
        {
            if (_matcher(message))
            {
                // TODO: Log
                //@robot.logger.debug \
                //  "Message '#{message}' matched regex /#{inspect @regex}/" if @regex

                _callback(_robot.GetResponse())
            }
        }
    }
}