using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
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

            foreach (var adapter in _robot.Adapters.Where(d => d.Value.LogRooms.Any()))
            {
                foreach (var room in adapter.Value.LogRooms)
                {
                    _robot.Speak(adapter.Key, room, msg);
                }
            }
        }
    }
}
