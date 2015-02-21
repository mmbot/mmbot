using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using MMBot;

namespace mmbot
{
    partial class Service : ServiceBase
    {
        readonly Options _options;
        Thread _thread;
        bool _shutdownRequested;
        ManualResetEvent _robotIsStopped;
        bool _isStopped;

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
            _robotIsStopped = new ManualResetEvent(false);

            _thread = new Thread(MmBotWorkerThread)
            {
                Name = "MMBot Worker Thread",
                IsBackground = true
            };

            _thread.Start();
        }

        protected override void OnStop()
        {
            RobotRunner.Stop();

            _robotIsStopped.WaitOne(TimeSpan.FromSeconds(20));

            if (!_isStopped)
            {
                // robot didn't shutdown gracefully so we just kill the thread        
                _thread.Abort();
            }
        }

        void MmBotWorkerThread()
        {
            RobotRunner.Run(_options);

            _isStopped = true; 

            _robotIsStopped.Set();
        }
    }
}