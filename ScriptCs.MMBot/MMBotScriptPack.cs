using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScriptCs.Contracts;


namespace ScriptCs.MMBot
{
    public class MMBotScriptPack : IScriptPack<MMBotScriptPackContext>
    {
        public void Initialize(IScriptPackSession session)
        {
            Context = new MMBotScriptPackContext();
            session.AddReference("MMBot");
            session.AddReference("ScriptCs.Contracts");
            session.ImportNamespace("MMBot");
        }

        public IScriptPackContext GetContext()
        {
            return Context;
        }

        public void Terminate()
        {
            Context.Shutdown().Wait();
        }

        public MMBotScriptPackContext Context { get; set; }
    }
}

