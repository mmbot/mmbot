using System.Collections.Generic;
using MMBot.Scripts;

namespace MMBot.Tests
{
    public class StubEchoScript : IMMBotScript
    {

        public void Register(Robot robot)
        {
            robot.Respond("(.*)", msg => msg.Send(msg.Match[1]));
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"type anything"};
        }
    }
}