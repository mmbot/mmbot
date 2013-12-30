using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using Common.Logging;
using log4net.Repository.Hierarchy;
using Microsoft.Owin;
using MMBot.ScriptCS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptCs;
using ScriptCs.Contracts;
using Roslyn.Compilers.CSharp;
using LogLevel = ScriptCs.Contracts.LogLevel;

namespace MMBot
{
    public class ScriptRunner
    {
        private readonly Robot _robot;
        private ILog _logger;
        private string[] _defaultMMBotReferences = new[] {"Microsoft.Owin"};

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
            try
            {
                ParseScriptComments(path);
            }
            catch (Exception ex)
            {
                _logger.Warn(string.Format("Could not parse comments: {0}", ex.Message));
            }

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
            scriptServiceRoot.Executor.AddReference<OwinContext>();
            
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

        public void ParseScriptComments(string path)
        {
            var tree = SyntaxTree.ParseFile(path, ParseOptions.Default.WithParseDocumentationComments(true));

            var compilation = Compilation.Create("comments", syntaxTrees: new[] { tree });
            var classSymbol = compilation.GlobalNamespace.GetMembers().FirstOrDefault();
            if (classSymbol != null)
            {
                var doc = classSymbol.GetDocumentationComment();
                XDocument comments = XDocument.Parse(string.Format("{0}{1}{2}", "<root>", doc.FullXmlFragmentOpt, "</root>"));                    
                ScriptMetadata metadata = new ScriptMetadata();
                var name = Path.GetFileNameWithoutExtension(path);
                var description = comments.Descendants("description").FirstOrDefault();
                var configuration = comments.Descendants("configuration").FirstOrDefault();
                var author = comments.Descendants("author").FirstOrDefault();
                var notes = comments.Descendants("notes").FirstOrDefault();
                var commands = comments.Descendants("commands").FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(name))
                    metadata.Name = name;
                if (description != null && !string.IsNullOrWhiteSpace(description.Value))
                    metadata.Description = description.Value.Trim();
                if (configuration != null && !string.IsNullOrWhiteSpace(configuration.Value))
                    metadata.Configuration = configuration.Value.Trim();
                if (author != null && !string.IsNullOrWhiteSpace(author.Value))
                    metadata.Author = author.Value.Trim();
                if (notes != null && !string.IsNullOrWhiteSpace(notes.Value))
                    metadata.Notes = notes.Value.Trim();
                if (commands != null && !string.IsNullOrWhiteSpace(commands.Value))
                    metadata.Commands = commands.Value.Split(';').Select(d => d.Trim()).ToList();

                _robot.AddMetadata(metadata);
            }

        }


    }
}