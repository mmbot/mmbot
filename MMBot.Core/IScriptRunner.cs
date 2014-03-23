using System;

namespace MMBot
{
    public interface IScriptRunner
    {
        void ParseScriptComments(string path);

        void RunScript(IScript script);

        void RegisterCleanup(Action cleanup);

        void Cleanup();

        void CleanupScript(string name);

        ScriptSource CurrentScriptSource { get; }
    }
}