using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public interface IScriptStore
    {
        IEnumerable<IScript> GetAllScripts();
        
        Task<IScript> SaveScript(string name, string contents);

        IObservable<IScript> ScriptUpdated { get; }

        IScript GetScriptByPath(string path);

        IScript GetScriptByName(string name);
    }
}