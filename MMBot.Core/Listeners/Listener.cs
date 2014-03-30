using System;
using MMBot.Scripts;

namespace MMBot
{
    public class Listener<T> : IListener where T : Message
    {
        private readonly Robot _robot;
        private readonly Func<T, MatchResult> _matcher;
        private readonly Action<IResponse<T>> _callback;

        protected Listener()
        {

        }

        public Listener(Robot robot, Func<T, MatchResult> matcher, Action<IResponse<T>> callback)
        {
            _robot = robot;
            _matcher = matcher;
            _callback = callback;
        }

        public virtual ScriptSource Source { get; set; }

        public virtual bool Call(Message message)
        {
            if (!(message is T))
            {
                return false;
            }

            MatchResult matchResult = _matcher(message as T);

            if (!matchResult.IsMatch)
            {
                return false;
            }
            
            if (message is TextMessage)
            {
                // Special handling for TextMessage
                _callback(Response.Create(_robot, message as T, matchResult));
                return true;
            }

            // All other message types are passed through
            _callback(Response.Create(_robot, message as T));
            return true;
        }
    }
}