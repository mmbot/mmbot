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

namespace mmbot
{
    public static class Initializer
    {
        public static void InitialiseCurrentDirectory()
        {
            var path = Environment.CurrentDirectory;
            var installationFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            if (installationFolder == null || !Directory.Exists(Path.Combine(installationFolder, "scripts")))
            {
                Console.WriteLine("The installation directory cannot be determined or does not contain a scripts sub-directory.");
                return;
            }
            if (string.Equals(path, installationFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("The current directory is the installation directory. The init command is designed to initialise a different location with the necessary base scripts etc. by copying them from the installation folder.");
                return;
            }

            var scriptsFolder = Path.Combine(path, "scripts");
            if (!Directory.Exists(scriptsFolder))
            {
                Directory.CreateDirectory(scriptsFolder);
            }

            Directory.GetFiles(Path.Combine(installationFolder, "scripts")).ForEach(f => File.Copy(f, Path.Combine(scriptsFolder, Path.GetFileName(f)), true));

            if (File.Exists(Path.Combine(installationFolder, "mmbot.template.ini")))
            {
                File.Copy(Path.Combine(installationFolder, "mmbot.template.ini"), Path.Combine(path, "mmbot.ini"));
            }

            Console.WriteLine("The current directory has been initialised");
            Console.WriteLine();
            Console.WriteLine("Remember to delete any scripts you don't want from the scripts folder and configure the mmbot.ini file");
            Console.WriteLine();
            Console.WriteLine("You can now install an adapter like jabbr to connect to your chat room using");
            Console.WriteLine("  nuget install mmbot.jabbr -o packages");
            Console.WriteLine("  or");
            Console.WriteLine("  scriptcs install mmbot.jabbr");
            Console.WriteLine();
        }

        public static void StartBot(Options options)
        {

            if (options.Test && (options.ScriptFiles == null || !options.ScriptFiles.Any()))
            {
                Console.WriteLine("You need to specify at least one script file to test.");
                return;
            }

            var logger = LoggerConfigurator.GetConsoleLogger(options.Verbose ? LogLevel.Debug : LogLevel.Info);

            if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
            {
                if (!Directory.Exists(options.WorkingDirectory))
                {
                    logger.Warn("Could not find specified directory.  Defaulting to current directory");
                }
                else
                {
                    Directory.SetCurrentDirectory(options.WorkingDirectory);
                }
            }

            var nugetResolver = new NuGetPackageAssemblyResolver(logger);
            
            AppDomain.CurrentDomain.AssemblyResolve += nugetResolver.OnAssemblyResolve;

            var adapters = new Type[0];

            if (!options.Test)
            {
                adapters = LoadAdapters(nugetResolver, logger);
            }

            var robot = Robot.Create("mmbot", GetConfiguration(options), logger, adapters.Concat(new []{typeof(ConsoleAdapter)}).ToArray());
            robot.Name = robot.GetConfigVariable("MMBOT_DEFAULT_NAME") ?? "mmbot";

            if (options.Test)
            {
                robot.AutoLoadScripts = false;
                options.ScriptFiles.ForEach(robot.LoadScriptFile);
            }
            else
            {
                robot.LoadScripts(nugetResolver.GetCompiledScriptsFromPackages());
                if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "scripts")))
                {
                    logger.Warn("There is no scripts folder. Have you forgotten to run 'mmbot init' to initialise the current running directory?");
                }
            }

            robot.Run().ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    Console.WriteLine(IntroText);
                    Console.WriteLine((options.Test ? "The test console is ready. " : "mmbot is running. ") + "Press CTRL+C at any time to exit" );
                }
            });

            while (true)
            {
                // sit and spin?
                Thread.Sleep(2000);
            }

        }

        internal static Type[] LoadAdapters(NuGetPackageAssemblyResolver nugetResolver, ILog logger)
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
