using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using mmbot;
using MMBot.Adapters;

namespace MMBot.WorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        private Robot _robot;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("MMBot.WorkerRole entry point called", "Information");

            while (true)
            {
                Thread.Sleep(10000);
                Trace.TraceInformation("Working", "Information");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            var options = new Options();
            

            if (options.Parameters != null && options.Parameters.Any())
            {
                options.Parameters.ForEach(p => Trace.WriteLine(p));
            }

            if (options.Init)
            {
                Initializer.InitialiseCurrentDirectory();
            }
            else
            {
                _robot = Initializer.StartBot(options).Result;
            }
            
            Trace.WriteLine(Initializer.IntroText);

            return base.OnStart();
        }
    }
}
