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
        private CancellationTokenSource _cancellationTokenSource;

        public RobotContainer()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async void OnHardResetRequested(object sender, EventArgs e)
        {
            PrintCurrentDomain("OnHardResetRequested");
            //The reset is being requested from within the Robot's AppDomain.
            Console.WriteLine("---> Waiting for shutdown method (OnHardResetRequested)");
            //await Shutdown();
            Console.WriteLine("---> Shutdown method finished (OnHardResetRequested)");
            _domain.DoCallBack(_target);
        }

        private AppDomain _domain;
        private CrossAppDomainDelegate _target;

        public void Run(string[] args, AppDomain domain, CrossAppDomainDelegate target)
        {
            _domain = domain;
            _target = target;
            PrintCurrentDomain("Run");
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
                    Initializer.InitialiseCurrentDirectory();
                }
                else
                {
                    var robot = Initializer.StartBot(options);
                    CurrentlyRunningRobot = robot.Result;
                    CurrentlyRunningRobot.HardResetRequested += OnHardResetRequested;
                    IsRunning = true;
                    robot.Wait(_cancellationTokenSource.Token);
                }
            }
        }
        private void PrintCurrentDomain(string name)
        {
            Console.WriteLine(string.Format("{0}: {1}", name, AppDomain.CurrentDomain.FriendlyName));
        }

        public Robot CurrentlyRunningRobot { get; set; }

        public async void Shutdown()
        {
            PrintCurrentDomain("Shutdown (in RobotContainer)");
            try
            {
                //_cancellationTokenSource.Cancel();

                Console.WriteLine("---> Waiting on Robot Shutdown");
                await CurrentlyRunningRobot.Shutdown();
                Console.WriteLine("---> Robot has Shutdown");
            }
            catch (AggregateException ex)
            {
                
            }
            catch (TaskCanceledException ex)
            {
                //Don't do anything.
            }
            IsRunning = false;
        }

        public bool IsRunning { get; set; }
    }
}
