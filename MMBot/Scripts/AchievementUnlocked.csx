// Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/achievement_unlocked.coffee

var robot = Require<Robot>();

private const string Url = "http://achievement-unlocked.heroku.com/xbox/#{0}.png";

robot.Respond(@"achievement (.+?)(\s*[^@\s]+@[^@\s]+)?\s*$/i", async msg =>
{
    var caption = msg.Match[2];
    var email = msg.Match[3];

    await AchievementCore(msg, caption, email);
});

robot.Respond(@"award (.+?)(\s*[^@\s]+@[^@\s]+)?\s*$/i", async msg =>
{
    var caption = msg.Match[2];
    var email = msg.Match[3];

    await AchievementCore(msg, caption, email);
});

private static async Task AchievementCore(IResponse<TextMessage> msg, string caption, string email)
{
    var url = String.Format(Url, caption);

    if (!string.IsNullOrWhiteSpace(email))
    {
        url += string.Format("?email=#{0}.png", email);
    }
    else
    {
                
    }

    msg.Send(url);
}

robot.AddHelp(
        "mmbot achievement <achievement> [achiever's gravatar email]",
        "mmbot award <achievement> [achiever's gravatar email]"
);