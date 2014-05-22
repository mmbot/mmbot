using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Common.Logging;
using Roslyn.Compilers.CSharp;
using ScriptCs;

namespace MMBot.Scripts
{
    public class LocalScriptStore : IScriptStore
    {
        private readonly FileSystem _fileSystem;
        private readonly IRobotPluginLocator _pluginLocator;
        private Subject<IScript> _scriptUpdated;
        private ILog _log;
        private IDisposable _filewatchSubscription;
        private ConcurrentDictionary<string, string> _loadedScriptFiles = new ConcurrentDictionary<string, string>();
        private FileSystemWatcher _fileSystemWatcher;

        public LocalScriptStore(LoggerConfigurator logConfig, FileSystem fileSystem, IRobotPluginLocator pluginLocator)
        {
            _fileSystem = fileSystem;
            _pluginLocator = pluginLocator;
            _scriptUpdated = new Subject<IScript>();
            _log = logConfig.GetLogger();
        }

        public void StartWatching()
        {
            if(!Directory.Exists(ScriptsPath))
            {
                return;
            }

            if (_filewatchSubscription != null)
            {
                _filewatchSubscription.Dispose();
            }

            _fileSystemWatcher = new FileSystemWatcher(ScriptsPath);

            _filewatchSubscription = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                h => _fileSystemWatcher.Created += h,
                h => _fileSystemWatcher.Created -= h)
                .Select(x => x.EventArgs.FullPath)
                .Merge(            
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => _fileSystemWatcher.Changed += h,
                        h => _fileSystemWatcher.Changed -= h)
                        .Select(x => x.EventArgs.FullPath))
                .Merge(
                    Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                        h => _fileSystemWatcher.Renamed += h,
                        h => _fileSystemWatcher.Renamed -= h)
                        .Select(x => x.EventArgs.FullPath))
                        .Do(path => _log.Info(string.Format("Detected change in script file '{0}'", path)))
                .Where(path => _loadedScriptFiles.Keys.Contains(path))
                .GroupBy(i => i)
                .SelectMany(g => g.Throttle(TimeSpan.FromMilliseconds(500)))
                .Select(GetScriptByPath)
                
                .Subscribe(_scriptUpdated);

            _fileSystemWatcher.Changed += (sender, args) => _log.Info("Log messagwe 1");

            _fileSystemWatcher.Filter = "*.csx";

            _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite
                                            | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            _fileSystemWatcher.EnableRaisingEvents = true;
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
                    "There is no scripts folder. Have you forgotten to run 'mmbot --init' to initialize the current running directory?");
            }

            var enumerateFiles = _fileSystem.EnumerateFiles(ScriptsPath, "*.csx").ToArray();

            enumerateFiles.ForEach(path => _loadedScriptFiles.AddOrUpdate(path, s => s, (s, s1) => s));

            return enumerateFiles.Select(scriptFile => new ScriptCsScriptFile
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

            _loadedScriptFiles.AddOrUpdate(path, s => s, (s, s1) => s);
            
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