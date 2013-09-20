using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Help : IMMBotScript
    {
        private static readonly Regex NameReplacementRegex  = new Regex(@"\b(mmbot|hubot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Register(Robot robot)
        {
            robot.Respond(@"help\s*(.*)?$", msg =>
            {
                IEnumerable<string> help = robot.HelpCommands;

                var filter = msg.Match[1];

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    help = help.Where(h => h.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) > -1).ToArray();
                    if (!help.Any())
                    {
                        msg.Send(string.Format("No available commands match {0}", filter));
                    }
                }

                var alias = robot.Alias ?? robot.Name;

                help = help.Select(h => NameReplacementRegex.Replace(h, alias));

                msg.Send(string.Join(Environment.NewLine, help.OrderBy(h => h)));
                msg.Message.Done = true;
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot help - Displays all of the help commands that mmbot knows about.",
                "mmbot help <query> - Displays all help commands that match <query>."
            };
        }
    }
}
