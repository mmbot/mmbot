// Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/github-status.coffee

var robot = Require<Robot>();

robot.Respond("github status$", msg => 
  msg.Http("https://status.github.com/api/status.json")
    .GetJson((err, res, body) => {
      var date = (DateTime)body["last_updated"];
      msg.Send(string.Format("Status: {0} ({1})", body["status"], GetRelativeTimeString(date)));
    }));


robot.Respond("github status last$", (msg) =>
  msg.Http("https://status.github.com/api/last-message.json")
    .GetJson((err, res, body) => {
      var date = (DateTime)body["created_on"];
      msg.Send(string.Format("Status: {0}\n" +
               "Message: {1}\n" +
               "Date: {2}", 
               (string)body["status"],
               (string)body["body"],
                GetRelativeTimeString(date)));
     }));

robot.Respond(@"github status messages$", msg => 
    msg.Http("https://status.github.com/api/messages.json")
    .GetJson((err, res, body) => {
        msg.Send(string.Join(Environment.NewLine, body.Select(message => string.Format("[{0}] {1} ({2})", (string)message["status"], (string)message["body"], GetRelativeTimeString((DateTime)message["created_on"])))));
      }));

robot.AddHelp("hubot github status - Returns the current system status and timestamp.",
              "hubot github status last - Returns the last human communication, status, and timestamp.",
              "hubot github status messages - Returns the most recent human communications with status and timestamp.");


private static string GetRelativeTimeString(DateTime d)
{
    // 1.
    // Get time span elapsed since the date.
    TimeSpan s = ((d.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now).Subtract(d);

    // 2.
    // Get total number of days elapsed.
    int dayDiff = (int)s.TotalDays;

    // 3.
    // Get total number of seconds elapsed.
    int secDiff = (int)s.TotalSeconds;

    // 4.
    // Don't allow out of range values.
    if (dayDiff < 0 || dayDiff >= 31)
    {
        return null;
    }

    // 5.
    // Handle same-day times.
    if (dayDiff == 0)
    {
        // A.
        // Less than one minute ago.
        if (secDiff < 60)
        {
            return "just now";
        }
        // B.
        // Less than 2 minutes ago.
        if (secDiff < 120)
        {
            return "1 minute ago";
        }
        // C.
        // Less than one hour ago.
        if (secDiff < 3600)
        {
            return string.Format("{0} minutes ago",
                Math.Floor((double)secDiff / 60));
        }
        // D.
        // Less than 2 hours ago.
        if (secDiff < 7200)
        {
            return "1 hour ago";
        }
        // E.
        // Less than one day ago.
        if (secDiff < 86400)
        {
            return string.Format("{0} hours ago",
                Math.Floor((double)secDiff / 3600));
        }
    }
    // 6.
    // Handle previous days.
    if (dayDiff == 1)
    {
        return "yesterday";
    }
    if (dayDiff < 7)
    {
        return string.Format("{0} days ago",
        dayDiff);
    }
    if (dayDiff < 31)
    {
        return string.Format("{0} weeks ago",
        Math.Ceiling((double)dayDiff / 7));
    }
    return null;
} 