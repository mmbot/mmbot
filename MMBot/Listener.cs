using System;

namespace MMBot
{
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

        public virtual ScriptSource Source { get; set; }

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
}