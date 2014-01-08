using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace MMBot
{
    [DebuggerDisplay("{Source.Name} - {_regex}")]
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

        public ScriptSource Source { get; set; }
        public Regex RegexPattern {
            get { return _regex; }
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
}