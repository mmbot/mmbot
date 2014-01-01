/**
* <description>
*     Creates an anchievement image with user's gravatar pic and achievement text
* </description>
*
* <configuration>
*
* </configuration>
*
* <commands>
*     mmbot achievement &lt;achievement&gt; [achiever's gravatar email];
*     mmbot award &lt;achievement&gt; [achiever's gravatar email];
* </commands>
* 
* <notes>
*     Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/achievement_unlocked.coffee
* </notes>
* 
* <author>
*     dkarzon
* </author>
*/

var robot = Require<Robot>();

private const string Url = "http://achievement-unlocked.heroku.com/xbox/{0}.png";

robot.Respond(@"(achievement|award) (.+?)(\s*[^@\s]+@[^@\s]+)?$", msg =>
{
    var caption = msg.Match[2];
    var email = msg.Match[3];

    AchievementCore(msg, caption, email);
});

private static void AchievementCore(IResponse<TextMessage> msg, string caption, string email)
{
    var url = String.Format(Url, Uri.EscapeUriString(caption));

    if (!string.IsNullOrWhiteSpace(email))
    {
        url += string.Format("?email={0}.png", email.Trim());
    }

    msg.Send(url);
}