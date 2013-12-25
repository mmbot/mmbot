
var robot = Require<Robot>();

public class MessageJob
{
	public Message Message;
	public string Schedule;
	public System.Timers.Timer JobTimer;
}

private List<MessageJob> _scheduledJobs = new List<MessageJob>();

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

	_scheduledJobs.Add(new MessageJob(){
		Message = m,
		Schedule = "on repeat at an interval of " + timeValue.ToString() + timeType,
		JobTimer = t
	});
	msg.Send(string.Format("Ok, I'll run \"{0}\" on repeat at an interval of {1}", cmdText, timeValue.ToString() + timeType));

});

robot.Respond(@"(schedule for (\d*)(\w) )(.*)", msg =>
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
	t.AutoReset = false;
	t.Enabled = true;

	_scheduledJobs.Add(new MessageJob(){
		Message = m,
		Schedule = "at " + DateTime.Now.AddMilliseconds(interval.TotalMilliseconds).ToString(),
		JobTimer = t
	});
	msg.Send(string.Format("Ok, I'll run \"{0}\" in the next {1}", cmdText, timeValue.ToString() + timeType));

});

robot.Respond(@"list schedule(s)?(d jobs)?", msg => 
{
	foreach (var job in _scheduledJobs)
	{
		msg.Send(string.Format("\"{0}\" scheduled to execute {1}", ((TextMessage)job.Message).Text, job.Schedule));
	}
});

robot.Respond(@"(stop|kill)( job)? (.*)", msg =>
{
	if (msg.Match[3].ToLower() == "all")
	{
		foreach (var j in _scheduledJobs)
		{
			j.JobTimer.Enabled = false;
		}
		_scheduledJobs.Clear();
		msg.Send("Killed all jobs");
	}
	var job = _scheduledJobs.Where(d => ((TextMessage)d.Message).Text.ToLower() == msg.Match[3].ToLower()).FirstOrDefault();
	if (job == null)
	{
		msg.Send("could not find that");
	}
	else
	{
		job.JobTimer.Enabled = false;
		_scheduledJobs.Remove(job);
		msg.Send("Killed the job");
	}
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


robot.AddHelp("mmbot repeat every <time> <command>- Repeat an mmbot command on a schedule (e.g. 4m, 1s, 24h).");
robot.AddHelp("mmbot schedule for <time> <command>- Schedule an mmbot command.");
robot.AddHelp("mmbot list schedule- Lists all scheduled jobs.");
robot.AddHelp("mmbot stop job <job|all>- kills a scheduled job, all for all jobs.");
