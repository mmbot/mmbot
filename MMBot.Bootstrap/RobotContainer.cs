using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MMBot.Bootstrap.Interfaces;

namespace MMBot.Bootstrap
{
    public class RobotContainer : MarshalByRefObject, IRobotContainer
    {
        private AppDomain _resetCallbackDomain;
        private CrossAppDomainDelegate _resetCallback;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private bool _isRunning = true;
        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public RobotContainer()
        {
        }

        private void OnHardResetRequested(object sender, EventArgs e)
        {
            //The reset is being requested from within the Robot's AppDomain, make a callback into the default AppDomain
            //requesting that it shut us down.
            _resetCallbackDomain.DoCallBack(_resetCallback);
        }

        public void Run(string[] args, AppDomain domain, CrossAppDomainDelegate target)
        {
            _resetCallbackDomain = domain;
            _resetCallback = target;
            var options = new Options();

            CommandLine.Parser.Default.ParseArguments(args, options);

            if (options.ShowHelp)
            {
                return;
            }

            if (options.RunAsService)
            {
                ServiceBase.Run(new ServiceBase[] { new Service(options) });
            }
            else
            {
                if (options.LastParserState != null && options.LastParserState.Errors.Any())
                {
                    return;
                }

                if (options.Parameters != null && options.Parameters.Any())
                {
                    options.Parameters.ForEach(Console.WriteLine);
                }

                if (options.Init)
                {
                    Initializer.InitializeCurrentDirectory();
                }
                else
                {
                    var robot = Initializer.StartBot(options);
                    CurrentlyRunningRobot = robot.Result;
                    CurrentlyRunningRobot.HardResetRequested += OnHardResetRequested;
                    _isRunning = true;
                    robot.Wait(_cancellationTokenSource.Token);
                }
            }
        }

        public Robot CurrentlyRunningRobot { get; set; }

        public async void Shutdown()
        {
            await CurrentlyRunningRobot.Shutdown();;
            _isRunning = false;
        }
    }
}
