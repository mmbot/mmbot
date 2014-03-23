using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using MMBot;

namespace mmbot
{
    [Serializable]
    public class Bootstrapper : MarshalByRefObject
    {
        private static AppDomain RobotAppDomain;
        private static string[] Args;
        private Bootstrapper _robotDomainBootstrapper;
        public Robot CurrentlyRunningRobot;
        public Bootstrapper OwningBootstrapper;

        public Bootstrapper()
        {

        }

        public void Bootstrap(string[] args)
        {
            Args = args;
            InitializeRobotDomain();
            RunInRobotDomain();
        }

        private void DestroyRobotDomain()
        {
            AppDomain.Unload(RobotAppDomain);
        }

        private static int initCount = 0;

        private static void InitializeRobotDomain()
        {
            initCount++;
            RobotAppDomain = AppDomain.CreateDomain("MMBotDomain" + initCount);
        }

        private void RunInRobotDomain()
        {
            PrintCurrentDomain("RunInRobotDomain");
            _robotDomainBootstrapper =
                (Bootstrapper)
                    RobotAppDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName,
                        "mmbot.Bootstrapper");
            _robotDomainBootstrapper.OwningBootstrapper = this;
            _robotDomainBootstrapper.Initialize(Args);
        }        

        private void Initialize(string[] args)
        {
            PrintCurrentDomain("Initialize");
            var options = new Options();

            Initializer.HardResetRequested += OnHardResetRequested;

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
                    robot.Wait();
                }
            }
        }

        private void DoHardReset()
        {
            PrintCurrentDomain("DoHardReset");
            DestroyRobotDomain();
            InitializeRobotDomain();
            RunInRobotDomain();
        }

        private async void ShutdownRobot()
        {
            PrintCurrentDomain("ShutdownRobot");
            await CurrentlyRunningRobot.Shutdown();
        }

        private void OnHardResetRequested(object sender, EventArgs e)
        {
            PrintCurrentDomain("OnHardResetRequested");
            ShutdownRobot();
            //The reset is being requested from within the Robot's AppDomain.
            OwningBootstrapper.DoHardReset();
        }

        private void PrintCurrentDomain(string name)
        {
            Debug.WriteLine(string.Format("{0}: {1}", name, AppDomain.CurrentDomain.FriendlyName));
        }
    }
}
