using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace mmbot
{
    partial class Service : ServiceBase
    {
        private Options _options;
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _thread;

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

        }

        public void MMBotWorkerThread()
        {

            Initializer.StartBot(_options).Wait();

            while (true)
            {
                // sit and spin?
                Thread.Sleep(2000);
            }
        }
    }
}
