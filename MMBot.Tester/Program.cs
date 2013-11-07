using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new Dictionary<string, string>();

            var robot = Robot.Create<ConsoleAdapter>("mmbot", config, LoggerConfigurator.GetConsoleLogger(LogLevel.Info));

            robot.AutoLoadScripts = false;

            robot.LoadScriptFile(@"E:\Code\mmbot\MMBot\Scripts\ping.csx");

            robot.LoadScriptFile(@"E:\Code\mmbot\MMBot\Scripts\whenisay.csx");

            robot.Run();

            

            while (true)
            {
                //something?
            }
        }
    }
}
