
var robot = Require<Robot>();


robot.Respond(@"(ps|powershell) (.*)", msg =>
{
    var command = msg.Match[2];
	msg.Send("executing " + command);
    try
    {
        var output = command.ExecutePowershellCommand();
		msg.Send(output);
    }
    catch (Exception)
    {
        msg.Send("erm....issues, move along");
    }
    
});

robot.AddHelp("mmbot powershell <command> - Executes a powershell command.");
