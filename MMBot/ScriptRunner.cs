using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using Common.Logging;
using log4net.Repository.Hierarchy;
using MMBot.ScriptCS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptCs;
using ScriptCs.Contracts;
using LogLevel = ScriptCs.Contracts.LogLevel;

namespace MMBot
{
    public class ScriptRunner
    {
        private readonly Robot _robot;
        private ILog _logger;

        public ScriptRunner(Robot robot, ILog logger)
        {
            _robot = robot;
            _logger = logger;
        }

        public void Initialize()
        {

        }

        
        public bool RunScriptFile(string path)
        {
            var console = new ScriptConsole();

            var scriptServicesBuilder = new ScriptServicesBuilder(console, _logger);

            scriptServicesBuilder.InMemory(true);

            
            scriptServicesBuilder.LoadModules("csx", new string[0]);
            var scriptServiceRoot = scriptServicesBuilder.Build();

            var defaultReferences = ScriptExecutor.DefaultReferences.ToArray();

            var packageReferences = scriptServiceRoot.PackageAssemblyResolver.GetAssemblyNames(Environment.CurrentDirectory);
            
            scriptServiceRoot.Executor.AddReferences(defaultReferences.Concat(packageReferences).ToArray());
            scriptServiceRoot.Executor.ImportNamespaces(ScriptExecutor.DefaultNamespaces.Concat(new[] { "MMBot", "Newtonsoft.Json", "Newtonsoft.Json.Linq", "System.Xml", "System.Net", "System.Net.Http" }).ToArray());
            scriptServiceRoot.Executor.AddReference<Robot>();
            scriptServiceRoot.Executor.AddReference<ILog>();
            scriptServiceRoot.Executor.AddReference<JArray>();
            scriptServiceRoot.Executor.AddReference<HttpResponseMessage>();
            scriptServiceRoot.Executor.AddReference<IScriptPackContext>();
            
            scriptServiceRoot.Executor.Initialize(new string[0], new IScriptPack[]
            {
                new MMBot2ScriptPackInternal(_robot), 
            });

            var result = scriptServiceRoot.Executor.Execute(path);
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