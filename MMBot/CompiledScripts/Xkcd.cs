using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MMBot.Scripts;

namespace MMBot.CompiledScripts
{
    // Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/xkcd.coffee
    public class Xkcd : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"xkcd(\s+latest)?$", async msg =>
            {
                var res = await msg.Http("http://xkcd.com/info.0.json").Get();
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    await msg.Send("Comic not found");
                    return;
                }

                var body = await res.Json();
                await msg.Send((string)body.title, (string)body.img, (string)body.alt);
            });


            robot.Respond(@"xkcd\s+(\d+)", async msg =>
            {
                var num = msg.Match[1];

                await FetchComic(msg, num);
            });

            robot.Respond(@"xkcd\s+random", async msg =>
            {
                var res = await msg.Http("http://xkcd.com/info.0.json").Get();
                if (res.StatusCode == HttpStatusCode.NotFound)
                {
                    await msg.Send("Comic not found");
                    return;
                }

                var body = await res.Json();
                var max = int.Parse((string) body.num);
                var num = _random.Next(max);
                await FetchComic(msg, num.ToString());
            });

            
        }

        private static async Task FetchComic(IResponse<TextMessage> msg, string num)
        {
            var res = await msg.Http(string.Format("http://xkcd.com/{0}/info.0.json", num)).Get();
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                await msg.Send(string.Format("Comic {0} not found", num));
                return;
            }

            var body = await res.Json();
            await msg.Send((string) body.title, (string) body.img, (string) body.alt);
        }

        private static Random _random = new Random(DateTime.Now.Millisecond);

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot xkcd [latest]- The latest XKCD comic",
                "mmbot xkcd <num> - XKCD comic <num>",
                "mmbot xkcd random - fetch a random XKCD comic"
            };
        }
    }
}
