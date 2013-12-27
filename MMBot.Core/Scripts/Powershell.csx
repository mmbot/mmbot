//Notes:
//Requires the MMBot.Powershell nuget package
//Output objects must either support a ToString method or be a string to display properly

using MMBot.Powershell;

var robot = Require<Robot>();

robot.Respond(@"(ps|powershell) (.*)", msg =>
{
    var command = msg.Match[2];
    try
    {
        foreach (string result in robot.ExecutePowershellCommand(command))
	{
	    msg.Send(result);
	}	
    }
    catch (Exception)
    {
        msg.Send("erm....issues, move along");
    }
    
});

robot.AddHelp("mmbot powershell <command> - Executes a powershell command.");
