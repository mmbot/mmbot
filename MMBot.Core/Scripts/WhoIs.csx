/**
* <description>
*    Defines a person
* </description>
*
* <commands>
*    who am I - returns what you are known as;
*    who is &lt;user&gt; - returns what a user is known as
*    &lt;user&gt; is &lt;definition&gt; - defines a person
* </commands>
* 
* <notes>
*    Similar to https://github.com/github/hubot/blob/master/src/scripts/roles.coffee
* </notes>
* 
* <author>
*    jamessantiago
* </author>
*/

var robot = Require<Robot>();

robot.Respond(@"whoami|who am i$", msg =>
{
	var user = msg.Message.User.Name;
    msg.Send(string.Format("You, {0}, are {1}", user, GetUserDefinition(user)));
});

robot.Respond(@"who is (.*)", msg =>
{
	var user = msg.Match[1];
	msg.Send(string.Format("{0} is {1}", user, GetUserDefinition(user)));
});

robot.Respond(@"([\w.\-_]+)(?<!who|what|why|where|when|how) is ([""'\w: \-_]+)[.!]*$", msg =>
{
	var user = msg.Match[1];
	var definition = msg.Match[2];
	var oldDefinition = GetUserDefinition(user);
	SetUserDefinition(user, definition);
	msg.Send(string.Format("Ok, {0} is no longer {1} and is now {2}", user, oldDefinition, definition));
});

robot.Respond(@"(I am|I'm) ([""'\w: \-_]+)[.!]*$", msg =>
{
	var user = msg.Message.User.Name;
	var definition = msg.Match[2];
	var oldDefinition = GetUserDefinition(user);
	SetUserDefinition(user, definition);
	msg.Send(string.Format("Ok {0}, you are no longer {1} and are now {2}", user, oldDefinition, definition));
});


private string GetUserDefinition(string user)
{
	user = user.ToLower();
	var persons = robot.Brain.Get<Dictionary<string, string>>("PersonDefinition").Result ?? new Dictionary<string, string>();
	return persons.ContainsKey(user) ? persons[user] : "not known to me";
}

private void SetUserDefinition(string user, string definition)
{
	user = user.ToLower();
	var persons = robot.Brain.Get<Dictionary<string, string>>("PersonDefinition").Result ?? new Dictionary<string, string>();
	persons[user] = definition;
	robot.Brain.Set("PersonDefinition", persons);
}