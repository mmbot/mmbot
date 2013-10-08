using System;
using System.Globalization;
using System.Text;
using Common.Logging.Log4Net;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using ICommonLog = Common.Logging.ILog;
using Common.Logging;

namespace MMBot
{
    public class LoggerConfigurator
    {
        private const string ThreadPattern = " Thread[%thread]";
        private const string Pattern = "%-5level{threadLevel}: %message%newline";
        private const string LoggerName = "scriptcs";

        private readonly LogLevel _logLevel;

        private ICommonLog _logger;

        public static Common.Logging.ILog GetConsoleLogger(LogLevel logLevel)
        {
            var configurator = new LoggerConfigurator(logLevel);
            configurator.ConfigureForConsole();
            return configurator.GetLogger();
        }

        public LoggerConfigurator(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public void ConfigureForConsole()
        {
            ConfigureAppender(new log4net.Appender.ConsoleAppender());
        }

        public void ConfigureAppender(AppenderSkeleton appender)
        {
            var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            var logger = log4net.LogManager.GetLogger(LoggerName);

            appender.Layout = new PatternLayout(GetLogPattern(_logLevel));
            appender.Threshold = hierarchy.LevelMap[_logLevel.ToString().ToUpper(CultureInfo.CurrentCulture)];
            hierarchy.Root.AddAppender(appender);
            hierarchy.Root.Level = Level.All;
            hierarchy.Configured = true;

            _logger = new CodeConfigurableLog4NetLogger(logger);
        }

        public ICommonLog GetLogger()
        {
            return _logger;
        }

        private static string GetLogPattern(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                case LogLevel.Info:
                    return Pattern.Replace("{threadLevel}", string.Empty);
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return Pattern.Replace("{threadLevel}", ThreadPattern);
                default:
                    throw new ArgumentOutOfRangeException("logLevel");
            }
        }

        private class CodeConfigurableLog4NetLogger : Log4NetLogger
        {
            protected internal CodeConfigurableLog4NetLogger(ILoggerWrapper log)
                : base(log)
            {
            }
        }
    }

}