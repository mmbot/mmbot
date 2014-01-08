using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Common.Logging.Log4Net;
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
        private const string LoggerName = "mmbot";

        private readonly LogLevel _logLevel;

        private ICommonLog _logger;
        private object _logLock = new object();

        public LoggerConfigurator(LogLevel logLevel)
        {
            _logLevel = logLevel;
        }

        public void ConfigureForConsole()
        {
            AddAppender(new log4net.Appender.ConsoleAppender());
        }

        public void ConfigureForRobot(Robot robot)
        {
            AddAppender(new RobotLogAppender(robot));
        }

        public void AddTraceListener()
        {
            AddAppender(new TraceAppender());
        }

        public void ConfigureForFile(string logFile)
        {
            if (!File.Exists(logFile)) File.Create(logFile).Dispose();
            var appender = new log4net.Appender.FileAppender(null, logFile, true);
            appender.File = logFile;
            AddAppender(appender);
        }

        public void AddAppender(AppenderSkeleton appender)
        {
            if (_logger == null)
                LoadLogger();

            var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            appender.Layout = new PatternLayout(GetLogPattern(_logLevel));
            appender.Threshold = hierarchy.LevelMap[_logLevel.ToString().ToUpper(CultureInfo.CurrentCulture)];
            hierarchy.Root.AddAppender(appender);
        }

        public IEnumerable<string> GetAppenders()
        {
            var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
            foreach (var appender in hierarchy.Root.Appenders)
                yield return appender.GetType().ToString();
        }

        public void LoadLogger()
        {
            lock (_logLock)
            {
                var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
                var logger = log4net.LogManager.GetLogger(LoggerName);
                hierarchy.Root.Level = Level.All;
                hierarchy.Configured = true;
                _logger = new CodeConfigurableLog4NetLogger(logger);
            }
        }

        public ICommonLog GetLogger()
        {
            if (_logger == null) LoadLogger();
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