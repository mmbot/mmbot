using System.IO;
using MMBot.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.ScriptIt
{
    public class ScriptsScripts : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"scriptthis (.*):(.*)", async msg =>
            {
                string name = msg.Match[1].Trim();
                string script = msg.Match[2].Trim();

                //Save script to file
                var filePath = Path.Combine(Environment.CurrentDirectory, Path.Combine("scripts", string.Format("{0}.csx", name)));
                File.WriteAllText(filePath, script);
                //try to load file
                try
                {
                    robot.LoadScriptFile("test1", filePath);
                    msg.Send(string.Format("Successfully added script: {0}", name));
                }
                catch (Exception ex)
                {
                    msg.Send(ex.Message);
                }
                //???
                //profit
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot scriptthis <script> - creates a new script for magic"
            };
        }
    }
}
