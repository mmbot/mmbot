using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MMBot;
using MMBot.Adapters;
using Common.Logging;

namespace mmbot
{
    class Program
    {
        static void Main(string[] args)
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

            if (options.Test)
            {
                StartTestBot(options);
            }
            else
            {
                StartBot(options);
            }
        }

        private static void StartBot(Options options)
        {
            //TODO: discover adapters and scripts for loading rather than rely on explicit code to load them
        }

        private static void StartTestBot(Options options)
        {
            if (options.ScriptFiles == null || !options.ScriptFiles.Any())
            {
                Console.WriteLine("You need to specify at least one script file to test.");
                return;
            }

            // Load the test console experience
            var robot = Robot.Create<ConsoleAdapter>("mmbot", GetConfiguration(options),
                LoggerConfigurator.GetConsoleLogger(options.Verbose ? LogLevel.All : LogLevel.Info));

            robot.AutoLoadScripts = false;

            options.ScriptFiles.ForEach(robot.LoadScriptFile);

            robot.Run().ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    Console.WriteLine("The test console is ready. Press CTRL+C at any time to exit");
                }
            });

            while (true)
            {
                // sit and spin?
            }
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
