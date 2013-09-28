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
        private static string _baseUri = "http://api.giphy.com/v1";
        public void Register(Robot robot)
        {
            var apiKey = GetApiKey(robot);

            

            robot.Respond(@"(gif|giphy)( me)? (.*)", async msg =>
            {
                var query = msg.Match[3];

                await GifMeCore(msg, query, apiKey);
            });
        }

        private static string GetApiKey(Robot robot)
        {
            return robot.GetConfigVariable("MMBOT_GIPHY_APIKEY") ?? "dc6zaTOxFJmzC";

        }

        public static async Task GifMe(Robot robot, string query, IResponse<TextMessage> msg )
        {
            await GifMeCore(msg, query, GetApiKey(robot));
        }

        private static async Task GifMeCore(IResponse<TextMessage> msg, string query, string apiKey)
        {
            var res = await msg.Http(_baseUri + "/gifs/search")
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
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"mmbot gif me <query> - Returns an animated gif matching the requested search term."};
        }
    }
}
