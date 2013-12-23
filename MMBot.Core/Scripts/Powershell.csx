var robot = Require<Robot>();


robot.Respond(@"(ps|powershell)( me)? (*)", msg =>
{
    var command = msg.Match[3];

    msg.Http(String.Format(Url, query))
        .Get((err, res) =>  {
    
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
});

robot.AddHelp("mmbot powershell me <command> - Executes a powershell command.");
