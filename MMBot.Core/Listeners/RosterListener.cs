using System;
using MMBot.Scripts;

namespace MMBot
{
    public class RosterListener : IListener
    {
        private readonly Robot _robot;
        private readonly Action<IResponse<EnterMessage>> _enterCallback;
        private readonly Action<IResponse<LeaveMessage>> _leaveCallback;

        public RosterListener(Robot robot, Action<IResponse<EnterMessage>> callback)
        {
            _robot = robot;
            _enterCallback = callback;
        }

        public RosterListener(Robot robot, Action<IResponse<LeaveMessage>> callback)
        {
            _robot = robot;
            _leaveCallback = callback;
        }

        public ScriptSource Source { get; set; }

        public bool Call(Message message)
        {
            var lm = message as LeaveMessage;
            if (lm != null && _leaveCallback != null)
            {
                _leaveCallback(Response.Create(_robot, lm));
                return true;
            }

            var em = message as EnterMessage;
            if (em != null && _enterCallback != null)
            {
                _enterCallback(Response.Create(_robot, em));
                return true;
            }

            return false;
        }
    }
}
