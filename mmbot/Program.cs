using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Core;
using MMBot;
using System.ServiceProcess;

namespace mmbot
{
    class Program
    {

        static void Main(string[] args)
        {
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
                    return;
                }

                SetupRobot(options);

                //Initializer.StartBot(options).Wait();

                while (true)
                {
                    // sit and spin?
                    Thread.Sleep(2000);
                }
            }
        }

        private static void SetupRobot(Options options)
        {
            var childAppDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString("N"));
            var wrapper = childAppDomain.CreateInstanceAndUnwrap(typeof (RobotWrapper).Assembly.FullName,
                typeof (RobotWrapper).FullName) as RobotWrapper;

            wrapper.ResetRequested += (sender, args) =>
            {
                Reset(sender, args);
                SetupRobot(options);
            };

            wrapper.Start(options).Wait();
        }

        private static void Reset(object o, EventArgs args)
        {
            var wrapper = o as RobotWrapper;

            LogManager.GetRepository().Shutdown();
            wrapper.ResetRequested -= Reset;
        }
    }

    [Serializable]
    public class RobotWrapper
    {
        private Options _options;
        public event EventHandler<EventArgs> ResetRequested;

        public Options Options
        {
            get { return _options; }
        }

        protected virtual void OnResetRequested()
        {
            var handler = ResetRequested;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public async Task Start(Options options)
        {
            _options = options;
            var robot = await Initializer.StartBot(options);
            robot.ResetRequested += (sender, args) => OnResetRequested();
        }

        
    }
}
