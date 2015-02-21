using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using MMBot;

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

            //always cleanup if needed
            PackageDirCleaner.CleanUpPackages();


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

                RobotRunner.Run(options);
            }
        }
    }
}