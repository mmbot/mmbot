using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using MMBot;
using MMBot.Adapters;
using Common.Logging;
using System.ServiceProcess;
using LoggerConfigurator = MMBot.LoggerConfigurator;

namespace mmbot
{
    class Program
    {

        static void Main(string[] args)
        {
            var options = new Options();
            CommandLine.Parser.Default.ParseArguments(args, options);

            if (options.RunAsService)
            {
                ServiceBase.Run(new ServiceBase[] { new Service(options) });
            }
            else
            {
                CommandLine.Parser.Default.ParseArguments(args, options);

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
}
