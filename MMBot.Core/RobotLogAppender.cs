using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;

namespace MMBot
{
    public class RobotLogAppender : AppenderSkeleton
    {
        private Robot _robot;

        public RobotLogAppender(Robot robot)
        {
            _robot = robot;
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var msg = RenderLoggingEvent(loggingEvent).Trim();
            //hack to prevent this from triggering a cascade of logging events
            if (Regex.Matches(msg, @"[A-Z]{4,5} : ").Count > 1)
                return;

            var speakTasks = from adapter in _robot.Adapters
                             from room in adapter.Value.LogRooms
                             select _robot.Speak(adapter.Key, room, msg);

            Task.WaitAll(speakTasks.ToArray());
        }
    }
}