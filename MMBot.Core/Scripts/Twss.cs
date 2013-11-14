using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Twss : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@".*(big|small|long|hard|soft|mouth|face|good|fast|slow|in there|on there|in that|on that|wet|dry|on the|in the|suck|blow|jaw|all in|fit that|fit it|hurts|hot|huge|balls|stuck)",
                msg => msg.Send("THAT'S WHAT SHE SAID!"));
        }

        public IEnumerable<string> GetHelp()
        {
            throw new NotImplementedException();
        }
    }
}
