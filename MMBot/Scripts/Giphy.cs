using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    // Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/giphy.coffee

    public class Giphy : IMMBotScript
    {
        public void Register(Robot robot)
        {
            var apiKey = robot.GetConfigVariable("MMBOT_GIPHY_APIKEY") ?? "dc6zaTOxFJmzC";

            var baseUri = "http://api.giphy.com/v1";

            robot.Respond(@"(gif|giphy)( me)? (.*)", async msg =>
            {
                var query = msg.Match[3];

                var res = await msg.Http(baseUri + "/gifs/search")
                    .Query(new
                    {
                        q = query,
                        api_key = apiKey
                    })
                    .GetJson();

                try
                {
                    var images = res.data;
                    if (images.Count > 0)
                    {
                        dynamic image = msg.Random(images);
                        await msg.Send((string)image.images.original.url);
                    }
                }
                catch (Exception)
                {
                    msg.Send("erm....issues, move along");
                }
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"hubot gif me <query> - Returns an animated gif matching the requested search term."};
        }
    }
}
