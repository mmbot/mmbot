using System.Collections.Generic;

namespace MMBot.Scripts
{
    public interface IMMBotScript
    {
        void Register(Robot robot);

        IEnumerable<string> GetHelp();
    }
}