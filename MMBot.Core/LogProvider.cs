using System;
using System.Reactive.Disposables;
using ScriptCs.Contracts;
using ILog = Common.Logging.ILog;

namespace MMBot
{
	public class LogProvider : ILogProvider  {
		readonly ILog _log;

		public LogProvider(ILog log) {
			_log = log;
		}

		public Logger GetLogger(string name)
		{
			return (level, messageFunc, exception, parameters) =>
			{
				switch (level)
				{
					case LogLevel.Trace:
						if(_log.IsTraceEnabled)
						{
							_log.Trace(messageFunc(), exception);
						}
						break;
					case LogLevel.Debug:
						if (_log.IsDebugEnabled) {
							_log.Debug(messageFunc(), exception);
						}
						break;
					case LogLevel.Info:
						if (_log.IsInfoEnabled) {
							_log.Info(messageFunc(), exception);
						}
						break;
					case LogLevel.Warn:
						if (_log.IsWarnEnabled) {
							_log.Warn(messageFunc(), exception);
						}
						break;
					case LogLevel.Error:
						if (_log.IsErrorEnabled) {
							_log.Error(messageFunc(), exception);
						}
						break;
					case LogLevel.Fatal:
						if (_log.IsFatalEnabled) {
							_log.Fatal(messageFunc(), exception);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(level), level, null);
				}
				return true;
			};

			
		}

		public IDisposable OpenNestedContext(string message)
		{
			return Disposable.Empty;
		}

		public IDisposable OpenMappedContext(string key, string value)
		{
			return Disposable.Empty;
		}
	}
}