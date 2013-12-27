using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using log4net.Core;
using log4net.Layout;
using log4net;
using Common.Logging;
using log4net.Appender;

namespace MMBot
{
    public class RobotLogAppender : AppenderSkeleton
    {
        private Robot _robot;
        private List<string> _rooms = new List<string>();

        public RobotLogAppender(Robot robot)
        {
            _robot = robot;

            foreach (string logRooms in new string[] {
                robot.GetConfigVariable("MMBOT_JABBR_LOGROOMS"),
                robot.GetConfigVariable("MMBOT_HIPCHAT_LOGROOMS"),
                robot.GetConfigVariable("MMBOT_XMPP_LOGROOMS")})
            {
                if (!string.IsNullOrWhiteSpace(logRooms))
                    _rooms.AddRange(logRooms.Trim().Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray());
            }
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var msg = RenderLoggingEvent(loggingEvent);
            //hack to prevent this from triggering a cascade of logging events
            if (Regex.Matches(msg, @".{4,5} : \[").Count > 1)
                return;

            foreach (var room in _rooms)
            {
                _robot.Speak(room, msg.Trim());
            }
        }
    }
}
