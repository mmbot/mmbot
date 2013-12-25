
var robot = Require<Robot>();

private Dictionary<System.Timers.Timer, Message> _scheduledJobs = new Dictionary<System.Timers.Timer, Message>();

robot.Respond(@"(repeat every (\d*)(\w) )(.*)", msg =>
{
    var repeatTxt = msg.Match[1];
	var timeValue = int.Parse(msg.Match[2]);
	var timeType = msg.Match[3];
	var cmdText = robot.Name + " " + msg.Match[4];	

	Message m = new TextMessage(msg.Message.User, cmdText, null); 

	TimeSpan interval = GetTimeSpanFromRelative(timeValue, timeType); 
	if (interval.TotalMilliseconds == 0)
	{
		msg.Send("Could not understand time given");
		return;
	}

	var t = new System.Timers.Timer();
	t.Elapsed += (sender, e) => { HandleMessageTimerElapsed(m, robot); };
	t.Interval = interval.TotalMilliseconds;
	t.Enabled = true;

	_scheduledJobs.Add(t, m);
	msg.Send(string.Format("Ok, I'll run \"{0}\" on repeat at an interval of {1}", cmdText, timeValue.ToString() + timeType));

});

private static void HandleMessageTimerElapsed(Message m, Robot robot)
{
	robot.Receive(m);
}

private static TimeSpan GetTimeSpanFromRelative(int timeValue, string timeType)
{
	switch (timeType.ToLower())
	{
		case "d":
			return new TimeSpan(timeValue, 0, 0, 0, 0);
			break;
		case "h":
			return new TimeSpan(0, timeValue, 0, 0, 0);
			break;
		case "m":
			return new TimeSpan(0, 0, timeValue, 0, 0);
			break;
		case "s":
			return new TimeSpan(0, 0, 0, timeValue, 0);
			break;
		default:
			return new TimeSpan(0);
			break;
	}
}


robot.AddHelp("mmbot repeat every <time> - Repeat an mmbot command on a schedule (e.g. 4m, 1s, 24h).");
