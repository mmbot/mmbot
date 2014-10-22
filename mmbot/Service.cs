using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using MMBot;

namespace mmbot
{
    partial class Service : ServiceBase
    {
        private Options _options;
        private Thread _thread;
        private Robot _robot;

        public Service(Options options)
        {
            _options = options;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            if (_options.LastParserState != null && _options.LastParserState.Errors.Any())
            {
                return;
            }

            _thread = new Thread(MMBotWorkerThread);
            _thread.Name = "MMBot Worker Thread";
            _thread.IsBackground = true;
            _thread.Start();
        }

        protected override void OnStop()
        {
            if (_robot == null)
            {
                return;
            }
            _robot.Shutdown().Wait(TimeSpan.FromSeconds(10));
        }

        void MMBotWorkerThread()
        {
            while (true)
            {
                _robot = Initializer.StartBot(_options).Result;

                var resetEvent = new AutoResetEvent(false);
                _robot.ResetRequested += (sender, args) => resetEvent.Set();
                resetEvent.WaitOne();
            }
        }
    }
}