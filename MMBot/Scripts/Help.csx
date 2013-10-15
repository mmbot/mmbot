using System.Text.RegularExpressions;

var robot = Require<Robot>();

static readonly Regex NameReplacementRegex  = new Regex(@"\b(mmbot|hubot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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


robot.AddHelp(
    "mmbot help - Displays all of the help commands that mmbot knows about.",
    "mmbot help <query> - Displays all help commands that match <query>."
);

