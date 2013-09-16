using System;
using System.Collections.Generic;
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
        public List<Listener> _listeners = new List<Listener>();

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
                    // TODO: Logging exception in listener
                }
                
            }
        }

        public void LoadAdapter()
        {

        }
        
    }

    public class Listener
    {
        private readonly Robot _robot;
        private readonly Func<Message, MatchResult> _matcher;
        private readonly Action<IResponse<Message>> _callback;

        public Listener(Robot robot, Func<Message, MatchResult> matcher, Action<IResponse<Message>> callback)
        {
            _robot = robot;
            _matcher = matcher;
            _callback = callback;
        }

        public bool Call(Message message)
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

    public class MatchResult
    {
        public bool IsMatch { get; private set; }


    }
}