using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common.Logging;
using ScriptCs;

namespace MMBot.Scripts
{
    public class LocalScriptStore : IScriptStore
    {
        private readonly FileSystem _fileSystem;
        private readonly IRobotPluginLocator _pluginLocator;
        private Subject<IScript> _scriptUpdated;
        private ILog _log;

        public LocalScriptStore(LoggerConfigurator logConfig, FileSystem fileSystem, IRobotPluginLocator pluginLocator)
        {
            _fileSystem = fileSystem;
            _pluginLocator = pluginLocator;
            _scriptUpdated = new Subject<IScript>();
            _log = logConfig.GetLogger();
        }

        private string ScriptsPath
        {
            get { return Path.Combine(_fileSystem.CurrentDirectory, "scripts"); }
        }

        public IEnumerable<IScript> GetAllScripts()
        {
            if (!Directory.Exists(ScriptsPath))
            {
                _log.Warn(
                    "There is no scripts folder. Have you forgotten to run 'mmbot --init' to initialise the current running directory?");
            }

            return _fileSystem.EnumerateFiles(ScriptsPath, "*.csx").Select(scriptFile => new ScriptCsScriptFile
            {
                Name = Path.GetFileNameWithoutExtension(scriptFile),
                Path = scriptFile
            })
            .Concat(_pluginLocator.GetPluginScripts());
        }

        public Task<IScript> SaveScript(string name, string contents)
        {
            
            var path = Path.Combine(ScriptsPath, string.Concat(name, ".csx"));
            return Task.Run(() =>
            {
                _fileSystem.WriteToFile(path, contents);
                return new ScriptCsScriptFile()
                {
                    Name = Path.GetFileNameWithoutExtension(name),
                    Path = path
                } as IScript;
            });
        }

        public IObservable<IScript> ScriptUpdated
        {
            get { return _scriptUpdated; }
        }

        public IScript GetScriptByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(string.Format("Could not find script file {0}", path), path);
            }

            var extension = Path.GetExtension(path);
            
            if (!string.Equals(extension, ".csx", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Unknown script file extension {0}");
            }

            return new ScriptCsScriptFile
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            };
        }

        public IScript GetScriptByName(string name)
        {
            return GetScriptByPath(Path.Combine(ScriptsPath, string.Concat(name, name.EndsWith(".csx") ? string.Empty : ".csx")));
        }
    }
}