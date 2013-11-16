using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MMBot;
using MMBot.Adapters;
using Common.Logging;
using LoggerConfigurator = MMBot.LoggerConfigurator;

namespace mmbot
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse Arguments
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

            StartBot(options);
        }

        private static void StartBot(Options options)
        {
            if (options.Test && (options.ScriptFiles == null || !options.ScriptFiles.Any()))
            {
                Console.WriteLine("You need to specify at least one script file to test.");
                return;
            }

            var logger = LoggerConfigurator.GetConsoleLogger(options.Verbose ? LogLevel.Debug : LogLevel.Info);

            var nugetResolver = new NuGetPackageAssemblyResolver(logger);
            
            AppDomain.CurrentDomain.AssemblyResolve += nugetResolver.OnAssemblyResolve;

            var adapters = new Type[0];

            if (!options.Test)
            {
                adapters = LoadAdapters(nugetResolver, logger);
            }

            var robot = Robot.Create("mmbot", GetConfiguration(options), logger, adapters.Concat(new []{typeof(ConsoleAdapter)}).ToArray());

            if (options.Test)
            {
                robot.AutoLoadScripts = false;
                options.ScriptFiles.ForEach(robot.LoadScriptFile);
            }
            else
            {
                robot.LoadScripts(nugetResolver.GetCompiledScriptsFromPackages());
            }

            robot.Run().ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    Console.WriteLine((options.Test ? "The test console is ready. " : "mmbot is running. ") + "Press CTRL+C at any time to exit" );
                }
            });

            while (true)
            {
                // sit and spin?
                Thread.Sleep(2000);
            }

        }

        private static Type[] LoadAdapters(NuGetPackageAssemblyResolver nugetResolver, ILog logger)
        {
            var adapters = nugetResolver.GetCompiledAdaptersFromPackages().ToArray();

            if (!adapters.Any())
            {
                logger.Warn("Could not find any adapters. Loading the default console adapter only");
            }
            return adapters;
        }

        public static Dictionary<string, string> GetConfiguration(Options options)
        {
            if (!options.SkipConfiguration && File.Exists("mmbot.ini"))
            {
                var config = new ConfigurationFileParser(Path.GetFullPath("mmbot.ini"));
                return config.GetConfiguration();
            }

            return new Dictionary<string, string>();
        }

        public const string IntroText = @"
                      _           _   
                     | |         | |  
  _ __ ___  _ __ ___ | |__   ___ | |_ 
 | '_ ` _ \| '_ ` _ \| '_ \ / _ \| __|
 | | | | | | | | | | | |_) | (_) | |_ 
 |_| |_| |_|_| |_| |_|_.__/ \___/ \__|
                                      
 >>> mmbot chat robot

 http://github.com/petegoo/mmbot
";
    }
}
