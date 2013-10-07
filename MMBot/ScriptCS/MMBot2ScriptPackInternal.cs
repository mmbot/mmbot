using System.ComponentModel.Composition;
using System.Threading.Tasks;
using ScriptCs.Contracts;

namespace MMBot.ScriptCS
{
    [PartNotDiscoverable]
    public class MMBot2ScriptPackInternal : IScriptPack<Robot>
    {
        private Robot _robot;

        public MMBot2ScriptPackInternal(Robot robot)
        {
            _robot = robot;
        }

        public MMBot2ScriptPackInternal()
        {
            _robot = new MMBotScriptPackContext();
        }

        public void Initialize(IScriptPackSession session)
        {

        }

        public IScriptPackContext GetContext()
        {
            return _robot;
        }

        public void Terminate()
        {

        }

        Robot IScriptPack<Robot>.Context
        {
            get { return _robot; }
            set { _robot = value; }
        }
    }

    public class MMBotScriptPackContext : Robot, IScriptPackContext
    {
        internal MMBotScriptPackContext()
            : base()
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