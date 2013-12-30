/**
* <description>
*     Interfaces with the Giphy API
* </description>
*
* <configuration>
*     MMBOT_GIPHY_APIKEY
* </configuration>
*
* <commands>
*     mmbot gif me &lt;query&gt; - Returns an animated gif matching the requested search term.
* </commands>
* 
* <notes>
*     Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/giphy.coffee
* </notes>
* 
* <author>
*     PeteGoo
* </author>
*/

var robot = Require<Robot>();

private static string _baseUri = "http://api.giphy.com/v1";

var apiKey = GetApiKey(robot);

robot.Respond(@"(gif|giphy)( me)? (.*)", msg =>
{
    var query = msg.Match[3];

    GifMeCore(msg, query, apiKey);
});

private static string GetApiKey(Robot robot)
{
    return robot.GetConfigVariable("MMBOT_GIPHY_APIKEY") ?? "dc6zaTOxFJmzC";
}

private static void GifMeCore(IResponse<TextMessage> msg, string query, string apiKey)
{
    msg.Http(_baseUri + "/gifs/search")
        .Query(new
        {
            q = query,
            api_key = apiKey
        })
        .GetJson((err, res, body) => {

        try
        {
            var images = body["data"].ToArray();
            if (images.Count() > 0)
            {
                var image = msg.Random(images);
                msg.Send((string)image["images"]["original"]["url"]);
            }
        }
        catch (Exception)
        {
            msg.Send("erm....issues, move along");
        }
    });
}


