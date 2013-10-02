using ScriptCs.Contracts;

namespace ScriptCs.MMBot
{
    public class MMBotScriptPack : IScriptPack<MMBotScriptPackContext>
    {
        public void Initialize(IScriptPackSession session)
        {
            Context = new MMBotScriptPackContext();
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