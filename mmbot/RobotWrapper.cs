using System;
using System.Threading;
using MMBot;

namespace mmbot
{
    public class RobotWrapper : MarshalByRefObject
    {
        static Robot _robot;
        private AutoResetEvent _resetEvent;

        public void Start(Options options)
        {
            _robot = Initializer.StartBot(options).Result;

            if (_robot == null)
            {
                // Something went wrong. Abort
                Environment.Exit(-1);
            }

            _resetEvent = new AutoResetEvent(false);
            _robot.ResetRequested += (sender, args) => _resetEvent.Set();
            _resetEvent.WaitOne();
        }

        public void Stop()
        {
            _robot.Shutdown()
                .Wait(TimeSpan.FromSeconds(10));

            _resetEvent.Set();
        }
    }
}