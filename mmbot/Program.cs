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
            if (Environment.UserInteractive)
            {
                var options = new Options();
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
                    Initializer.StartBot(options);
                }
            }
            else
            {
                var options = new Options();
                CommandLine.Parser.Default.ParseArguments(args, options);
                ServiceBase.Run(new ServiceBase[] { new Service(options) });
            }            

        }

    }
}
