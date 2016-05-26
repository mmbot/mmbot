using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Common.Logging;
using HtmlAgilityPack;
using Microsoft.Owin;
using MMBot.ScriptCS;
using Newtonsoft.Json.Linq;
using Roslyn.Compilers.CSharp;
using Roslyn.Scripting.CSharp;
using ScriptCs;
using ScriptCs.Contracts;
using ScriptCs.Hosting;

namespace MMBot.Scripts
{
    public class ScriptRunner : IMustBeInitializedWithRobot, IScriptRunner
    {
        private Robot _robot;
        private Common.Logging.ILog _logger;
        private string[] _defaultMMBotReferences = new[] {"Microsoft.Owin"};

        private readonly Dictionary<string, Action> _cleanup = new Dictionary<string, Action>();
        private readonly List<Type> _loadedScriptTypes = new List<Type>();
        private readonly Dictionary<string, string> scriptHashes = new Dictionary<string, string>();

        public ScriptRunner(Common.Logging.ILog logger)
        {
            CurrentScriptSource = null;
            _logger = logger;
        }

        public ScriptSource CurrentScriptSource { get; set; }

        public void Initialize(Robot robot)
        {
            _robot = robot;
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
                if (!string.IsNullOrWhiteSpace(name))
                    metadata.Name = name;

                metadata.Description = ParseNode(comments, "description");
                metadata.Configuration = ParseNode(comments, "configuration");
                metadata.Author = ParseNode(comments, "author");
                metadata.Notes = ParseNode(comments, "notes");

                var commands = comments.Descendants("commands").FirstOrDefault();
                if (commands != null && !string.IsNullOrWhiteSpace(commands.Value))
                    metadata.Commands = commands.Value.Split(';').Select(d => d.Trim()).ToList();

                _robot.AddMetadata(metadata);
            }
        }

        public void RunScript(IScript script)
        {
            if (script == null)
            {
                throw new ArgumentNullException("script");
            }

            var scriptFile = script as ScriptCsScriptFile;
            if (scriptFile != null)
            {
                _logger.Info(string.Format("Loading script {0}", script.DisplayName));
                RunScriptFile(scriptFile.Path);
                return;
            }

            var typedScript = script as TypedScript;
            if (typedScript != null)
            {
                RunTypedScript(script, typedScript);
                return;
            }

            _logger.Debug(string.Format("Skipped running script {0} as it was not a recognised ScriptCs script type", script.Name));
        }

        public void RegisterCleanup(Action cleanup)
        {
            if (CurrentScriptSource != null)
            {
                _cleanup[CurrentScriptSource.Name] = cleanup;
            }
        }

        public void Cleanup()
        {
            _cleanup.Keys.ToList().ForEach(CleanupScript);
            _cleanup.Clear();
        }

        public void CleanupScript(string name)
        {
            if (_cleanup.ContainsKey(name))
            {
                try
                {
                    _cleanup[name]();
                }
                catch (Exception e)
                {
                    _logger.Error("Error during cleanup", e);
                }
                finally
                {
                    _cleanup.Remove(name);
                }
            }
        }

        private bool RunScriptFile(string path)
        {
            string hash;
            using (var stream = File.OpenRead(path))
            {
                hash = Encoding.UTF8.GetString(System.Security.Cryptography.MD5.Create().ComputeHash(stream));
            }

            var scriptName = Path.GetFileNameWithoutExtension(path);

            string value;
            if (scriptName != null && scriptHashes.TryGetValue(scriptName, out value) && value == hash)
            {
                return false;
            }
            
            using (StartScriptProcessingSession(new ScriptSource(scriptName, path)))
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

                scriptServicesBuilder.Cache(false);

                scriptServicesBuilder.LoadModules("csx");
                var scriptServiceRoot = scriptServicesBuilder.Build();

                var defaultReferences = ScriptExecutor.DefaultReferences.ToArray();

                var fileSystem = new FileSystem();
                //where clause hack using the exact same code that the hack in scriptCS sues to filter their list of assemblies in ShouldLoadAssembly in RuntimeServices.cs
                var packageReferences = scriptServiceRoot.PackageAssemblyResolver.GetAssemblyNames(Environment.CurrentDirectory).Where(fileSystem.IsPathRooted);

                scriptServiceRoot.Executor.AddReferences(defaultReferences.Concat(NuGetPackageAssemblyResolver.FilterAssembliesToMostRecent(packageReferences)).ToArray());
                scriptServiceRoot.Executor.ImportNamespaces(
                    ScriptExecutor.DefaultNamespaces.Concat(new[]
                    {
                        "MMBot", "Newtonsoft.Json", "Newtonsoft.Json.Linq", "HtmlAgilityPack", "System.Xml", "System.Net",
                        "System.Net.Http"
                    }).ToArray());
                scriptServiceRoot.Executor.AddReference<Robot>();
                scriptServiceRoot.Executor.AddReference<Common.Logging.ILog>();
                scriptServiceRoot.Executor.AddReference<JArray>();
                scriptServiceRoot.Executor.AddReference<HtmlDocument>();
                scriptServiceRoot.Executor.AddReference<HttpResponseMessage>();
                scriptServiceRoot.Executor.AddReference<IScriptPackContext>();
                scriptServiceRoot.Executor.AddReference<OwinContext>();
                
                scriptServiceRoot.Executor.Initialize(new string[0], new IScriptPack[]
                {
                    new MMBot2ScriptPackInternal(_robot),
                });

                var result = scriptServiceRoot.Executor.Execute(path);

                if (!result.IsCompleteSubmission) 
                {
                    _logger.Error(string.Format("{0}: error compiling script - {1}", path, result.CompileExceptionInfo.SourceException.Message));
                }

                if (result.CompileExceptionInfo != null)
                {
                    _logger.Error(result.CompileExceptionInfo.SourceException.Message);
                    _logger.Debug(result.CompileExceptionInfo.SourceException);
                }

                if (result.ExecuteExceptionInfo != null)
                {
                    _logger.Error(result.ExecuteExceptionInfo.SourceException);
                }

                scriptHashes[CurrentScriptSource.Name] = hash;

                return result.IsCompleteSubmission && result.CompileExceptionInfo == null && result.ExecuteExceptionInfo == null;
            }
            
        }

        private void RunTypedScript(IScript script, TypedScript typedScript)
        {
            using ( StartScriptProcessingSession(new ScriptSource(typedScript.Type.Name, typedScript.Type.AssemblyQualifiedName))) {
                _logger.Info(string.Format("Loading script {0}", script.DisplayName));

                var scriptInstance = Activator.CreateInstance(typedScript.Type) as IMMBotScript;
                scriptInstance.Register(_robot);
                if (!_loadedScriptTypes.Contains(typedScript.Type))
                {
                    _loadedScriptTypes.Add(typedScript.Type);
                }
                _robot.AddHelp(scriptInstance.GetHelp().ToArray());
            }
        }

        private string ParseNode(XDocument comments, string nodeName)
        {
            var item = comments.Descendants(nodeName).FirstOrDefault();
            string results = "";
            if (item != null && !string.IsNullOrWhiteSpace(item.Value))
                results = string.Join(Environment.NewLine, item.Value.Trim().Split(';').Select(d => d.Trim()));
            return results;
        }

        public IDisposable StartScriptProcessingSession(ScriptSource scriptSource)
        {
            if (scriptSource == null)
            {
                throw new ArgumentNullException("scriptSource");
            }

            if (CurrentScriptSource != null)
            {
                throw new ScriptProcessingException("Cannot process multiple script sources at the same time");
            }
            CurrentScriptSource = scriptSource;

            CleanupScript(scriptSource.Name);

            _robot.Listeners.RemoveAll(l => l.Source != null && l.Source.Name == scriptSource.Name);

            return Disposable.Create(() => CurrentScriptSource = null);
        }

    }
}