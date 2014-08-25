using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text.RegularExpressions;
using MMBot.Scripts;

namespace MMBot.Tests.CompiledScripts
{
    public class ListenerTest : IMMBotScript
    {
        private Robot _robot;

        public void Register(Robot robot)
        {
            _robot = robot;

            robot.Listen<TextMessage>(WithRegex,
                msg => { if (msg != null && msg.Message != null && msg.Message.Text != null && !msg.Message.Text.StartsWith("Handled")) msg.Send("Handled TextMessage with regex"); });
            robot.Listen<TextMessage>(WithoutRegex,
                msg => { if (msg != null && msg.Message != null && msg.Message.Text != null && !msg.Message.Text.StartsWith("Handled")) msg.Send("Handled TextMessage without regex"); });
            robot.Listen<TextMessage>(NoOtherHandlers,
                msg => { if (msg != null && msg.Message != null && msg.Message.Text != null && !msg.Message.Text.StartsWith("Handled")) msg.Send("Handled TextMessage with no other handlers"); });
        }

        private MatchResult WithRegex(TextMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Text))
            {
                return new MatchResult(false);
            }
            MatchCollection matches = Regex.Matches(msg.Text, "testregex", RegexOptions.IgnoreCase);

            return matches.Cast<Match>().Any(m => m.Success)
                ? new MatchResult(true, matches)
                : new MatchResult(false);
        }

        private MatchResult WithoutRegex(TextMessage msg)
        {
            return new MatchResult(true);
        }

        private MatchResult NoOtherHandlers(TextMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Text))
            {
                return new MatchResult(false);
            }
            if (_robot.Listeners.OfType<TextListener>().Any(t => t.RegexPattern.IsMatch(msg.Text)))
            {
                return new MatchResult(false);
            }

            return new MatchResult(true);
        }

        public IEnumerable<string> GetHelp()
        {
            return new string[0];
        }
    }
}
