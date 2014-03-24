using System;
using System.Linq;
using System.Threading;
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

                Initializer.StartBot(options).Wait();

                while (true)
                {
                    // sit and spin?
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
