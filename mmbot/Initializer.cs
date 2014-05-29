using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MMBot;
using Common.Logging;

namespace mmbot
{
    public static class Initializer
    {
        public static async Task<Robot> StartBot(Options options)
        {
            if (options.Test && (options.ScriptFiles == null || !options.ScriptFiles.Any()))
            {
                Console.WriteLine("You need to specify at least one script file to test.");
                return null;
            }

            var logConfig = CreateLogConfig(options);
            ConfigurePath(options, logConfig.GetLogger());

            var builder = new RobotBuilder(logConfig).WithConfiguration(GetConfiguration(options));

            if (!string.IsNullOrWhiteSpace(options.Name))
            {
                builder.WithName(options.Name);
            }

            if (options.Test)
            {
                builder.DisableScriptDiscovery();
            }

            if (!string.IsNullOrEmpty(options.WorkingDirectory))
            {
                builder.UseWorkingDirectory(options.WorkingDirectory);
            }

            if (options.Watch)
            {
                builder.EnableScriptWatcher();
            }

            Robot robot = null;

            try
            {
                robot = builder.Build();
                if(robot == null)
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                logConfig.GetLogger().Fatal("Could not build robot. Try installing the latest version of any mmbot packages (mmbot.jabbr, mmbot.slack etc) if there was a breaking change.", e);
            }

            await robot.Run().ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    Console.WriteLine(IntroText);
                    Console.WriteLine((options.Test ? "The test console is ready. " : "mmbot is running. ") + "Press CTRL+C at any time to exit");
                }
            });
            return robot;
        }

        public static void InitializeCurrentDirectory()
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
                Console.WriteLine("The current directory is the installation directory. The init command is designed to initialize a different location with the necessary base scripts etc. by copying them from the installation folder.");
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
                if (File.Exists(Path.Combine(path, "mmbot.ini")))
                {
                    Console.WriteLine("mmbot.ini already exists in current directory.");
                    Console.WriteLine();
                }
                else
                {
                    File.Copy(Path.Combine(installationFolder, "mmbot.template.ini"), Path.Combine(path, "mmbot.ini"));
                }
            }

            Console.WriteLine("The current directory has been initialized");
            Console.WriteLine();
            Console.WriteLine("Remember to delete any scripts you don't want from the scripts folder and configure the mmbot.ini file");
            Console.WriteLine();
            Console.WriteLine("You can now install an adapter like jabbr to connect to your chat room using");
            Console.WriteLine("  nuget install mmbot.jabbr -o packages");
            Console.WriteLine("  or");
            Console.WriteLine("  scriptcs install mmbot.jabbr");
            Console.WriteLine();
        }

        public static void ConfigurePath(Options options, ILog logger)
        {
            if (!string.IsNullOrWhiteSpace(options.WorkingDirectory))
            {
                if (!Directory.Exists(options.WorkingDirectory))
                {
                    logger.Warn(string.Format("Could not find specified working directory {0}. Defaulting to current directory", options.WorkingDirectory));
                }
                else
                {
                    Directory.SetCurrentDirectory(options.WorkingDirectory);
                }
            }
        }

        public static LoggerConfigurator CreateLogConfig(Options options)
        {
            var logConfig = new LoggerConfigurator(options.Verbose ? LogLevel.Debug : LogLevel.Info);
            if (Environment.UserInteractive)
            {
                logConfig.ConfigureForConsole();
            }
            else
            {
                logConfig.AddTraceListener();
            }

            var logger = logConfig.GetLogger();

            if (!string.IsNullOrWhiteSpace(options.LogFile))
            {
                if (Directory.Exists(Path.GetDirectoryName(options.LogFile)))
                    logConfig.ConfigureForFile(options.LogFile);
                else
                    logger.Warn(string.Format("Failed to load log file.  Path for {0} does not exist.", options.LogFile));
            }
            return logConfig;
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

 http://github.com/mmbot/mmbot
";
    }
}
