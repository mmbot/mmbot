using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace MMBot.Scripts
{
    public interface IMMBotScript
    {
        void Register(Robot robot);
        
        IEnumerable<string> GetHelp();
    }
}