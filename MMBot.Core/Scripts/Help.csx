/**
* <description>
*     Provides help commands
* </description>
*
* <commands>
*     mmbot help - Displays all of the help commands that mmbot knows about.;
*     mmbot help &lt;query&gt; - Displays all help commands that match &lt;query&gt;.;
*     mmbot list scripts - Displays a list of all the loaded script files;
*     mmbot man &lt;query&gt; - Displays the details for the script that matches &lt;query&gt;.;
* </commands>
* 
* <author>
*     PeteGoo
* </author>
*/

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

robot.Respond(@"list scripts$", msg =>
{
	if (!robot.ScriptData.Any())
	{
		msg.Send("No script information is available");
	}
	else
	{
		msg.Send("I have the following scripts:\n\n" + string.Join(Environment.NewLine, robot.ScriptData.Select(s => s.Name).OrderBy(n => n)));
	}
});

robot.Respond(@"(man|explain) (.*)", msg =>
{
	var scriptName = msg.Match[2];
	var scriptData = robot.ScriptData.Where(d => d.Name.Equals(scriptName, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
	if (scriptData != null)
	{
		string detailsFormat = @"
Name:
  {0}

Description:
  {1}

Configuration:
  {2}

Commands:
  {3}

Notes:
  {4}

Author:
  {5}
";
		msg.Send(string.Format(detailsFormat,
			scriptData.Name,
			scriptData.Description,
			scriptData.Configuration,
			string.Join(Environment.NewLine + "  ", scriptData.Commands),
			scriptData.Notes,
			scriptData.Author));
	}
	else
	{
		msg.Send(string.Format("No script has the name of {0}", scriptName));
	}
});
