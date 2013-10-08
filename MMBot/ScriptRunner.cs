using System.IO;
using System.Linq;
using Common.Logging;
using log4net.Repository.Hierarchy;
using MMBot.ScriptCS;
using ScriptCs;
using ScriptCs.Contracts;
using LogLevel = ScriptCs.Contracts.LogLevel;

namespace MMBot
{
    public class ScriptRunner
    {
        private readonly Robot _robot;
        private ScriptServices _scriptServiceRoot;
        private ILog _logger;

        public ScriptRunner(Robot robot, ILog logger)
        {
            _robot = robot;
            _logger = logger;
        }

        public void Initialize()
        {
            var console = new ScriptConsole();

            var scriptServicesBuilder = new ScriptServicesBuilder(console, _logger);

            scriptServicesBuilder.LoadModules("csx", new string[0]);
            _scriptServiceRoot = scriptServicesBuilder.Build();

            _scriptServiceRoot.Executor.AddReferences(ScriptExecutor.DefaultReferences.ToArray());
            _scriptServiceRoot.Executor.ImportNamespaces(ScriptExecutor.DefaultNamespaces.Concat(new[] { "MMBot" }).ToArray());
            _scriptServiceRoot.Executor.AddReference<Robot>();
            _scriptServiceRoot.Executor.AddReference<IScriptPackContext>();

            _scriptServiceRoot.Executor.Initialize(new string[0], new IScriptPack[]
            {
                new MMBot2ScriptPackInternal(_robot), 
            });
        }

        public bool RunScriptFile(string path)
        {
            var result = _scriptServiceRoot.Executor.Execute(path);
            if (result.CompileExceptionInfo != null)
            {
                _logger.Error(result.CompileExceptionInfo.SourceException.Message);
                _logger.Debug(result.CompileExceptionInfo.SourceException);
            }

            if (result.ExecuteExceptionInfo != null)
            {
                _logger.Error(result.ExecuteExceptionInfo.SourceException);
            }

            return result.CompileExceptionInfo == null && result.ExecuteExceptionInfo == null;
        }


    }
}