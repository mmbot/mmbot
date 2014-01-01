/**
* <description>
*    Manages user and role authentication for use by other scripts
* </description>
*
* <configuration>
*    MMBOT_AUTH_ADMINS : a comma seperated list of admins
* </configuration>
*
* <commands>
*    mmbot give &lt;user&gt; the &lt;role&gt; role - assigns user to role
*    mmbot remove &lt;user&gt; from the &lt;role&gt; - removes user from role;
*    mmbot list roles for &lt;user&gt; - lists roles that the user has
*    mmbot list admins - lists the admin users
*    mmbot dump roles - dumps all user role assignments
* </commands>
* 
* <notes>
*    Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/auth.coffee;
*    Check if user is in a role: Robot.IsInRole(string username, string role);
*    Check if user is an admin: Robot.IsAdmin(string username);
* </notes>
* 
* <author>
*    jamessantiago
* </author>
*/

var robot = Require<Robot>();

robot.Respond(@"(give|add) (\S+) (to )?the ([-_ \w]+) role$", msg =>
{
	var user = msg.Match[2];
	var role = msg.Match[4];

	if (robot.IsInRole(user, role))
	{
		msg.Send(string.Format("{0} is already in the {1} role", user, role));
	}
	else
	{
		robot.AddUserToRole(user, role);
		msg.Send(string.Format("Got it, {0} is now in the {1} role", user, role));
    }
});

robot.Respond(@"remove (\S+) from the ([-_ \w]+) role$", msg =>
{
	var user = msg.Match[1];
	var role = msg.Match[2];

	if(!robot.IsInRole(user, role))
    {
		msg.Send(string.Format("{0} is not in the {1] role", user, role));
	}
	else
	{
		robot.RemoveUserFromRole(user, role);
		msg.Send(string.Format("Got it, {0} is no longer in the {1} role", user, role));
	}
});

robot.Respond(@"list roles for (.*)", msg =>
{
	var user = msg.Match[1];

	var roles = robot.GetUserRoles(user);
	if (roles.Length == 0)
	{
		msg.Send(string.Format("Could not find any roles for {0}", user));
	}
	else
	{
		msg.Send(string.Format("{0} has the following roles:\n{1}", user, string.Join(Environment.NewLine, roles)));
	}
});

robot.Respond(@"list admins$", msg =>
{
	msg.Send(string.Format("I have the following admins:\n{0}", string.Join(Environment.NewLine, robot.Admins)));
});

robot.Respond(@"dump roles$", msg =>
{
	var roleStore = robot.Brain.Get<Dictionary<string, string>>("UserRoleStore").Result ?? new Dictionary<string, string>();
	foreach (var role in roleStore.Values.Distinct())
	{
		var users = roleStore.Where(d => d.Value == role).Select(d => d.Key).OrderBy(d => d);
		msg.Send(string.Format("{0} found in the {1} role:\n{2}", users.Count().Pluralize("user"), role, string.Join(Environment.NewLine + "  ", users)));
    }
});