using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMBot;
using ScriptCs.Contracts;

namespace ScriptCs.MMBot
{
    public class MMBotScriptPackContext : Robot, IScriptPackContext
    {
        internal MMBotScriptPackContext() : base()
        {
        }

        public override async Task Run()
        {
            await base.Run();
        }

        public void LoadAllScripts()
        {

        }
    }
}
