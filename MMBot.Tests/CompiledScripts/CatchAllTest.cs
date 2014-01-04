using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMBot.Scripts;

namespace MMBot.Tests.CompiledScripts
{
    class CatchAllTest : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.CatchAll(msg => msg.Send(string.Format("Caught msg {0} from {1}", msg.Message.Text, msg.Message.User.Name)));
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "anything"
            };
        }
    }
}
