using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMBot.Scripts;

namespace MMBot
{
    public class CatchAllListener : IListener
    {
        private readonly Robot _robot;
        private readonly Action<IResponse<CatchAllMessage>> _callback;

        public CatchAllListener(Robot robot, Action<IResponse<CatchAllMessage>> callback)
        {
            _robot = robot;
            _callback = callback;
        }

        public ScriptSource Source { get; set; }

        public bool Call(Message message)
        {
            var cam = message as CatchAllMessage;
            if (cam != null)
            {
                _callback(Response.Create(_robot, cam));
                return true;
            }

            var tm = message as TextMessage;
            if (tm != null)
            {
                var catchAll = new CatchAllMessage(tm.User, tm.Text);
                _callback(Response.Create(_robot, catchAll));
                return true;
            }

            var em = message as EnterMessage;
            if (em != null)
            {
                var catchAll = new CatchAllMessage(em.User, string.Format("{0} joined {1}", em.User.Name, em.User.Room));
                _callback(Response.Create(_robot, catchAll));
                return true;
            }

            var lm = message as LeaveMessage;
            if (lm != null)
            {
                var catchAll = new CatchAllMessage(lm.User, string.Format("{0} left {1}", lm.User.Name, lm.User.Room));
                _callback(Response.Create(_robot, catchAll));
                return true;
            }

            var topm = message as TopicMessage;
            if (topm != null)
            {
                var catchAll = new CatchAllMessage(topm.User, topm.Topic);
                _callback(Response.Create(_robot, catchAll));
                return true;
            }

            if (message != null)
            {
                var catchAll = new CatchAllMessage(message.User, "");
                _callback(Response.Create(_robot, catchAll));
                return true;
            }

            return false;
        }
    }
}

