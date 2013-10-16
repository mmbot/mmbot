using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MMBot.Scripts;

namespace MMBot.CompiledScripts
{
    public class GoogleImages : IMMBotScript
    {
        Random _random = new Random(DateTime.Now.Millisecond);
        Regex _httpRegex = new Regex(@"^https?:\/\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Register(Robot robot)
        {
            robot.Respond(@"(image|img)( me)? (.*)", msg => ImageMe(msg, msg.Match[3], url => msg.Send(url)));

            robot.Respond(@"animate( me)? (.*)", msg => ImageMe(msg, msg.Match[2], url => msg.Send(url), true));

            robot.Respond(@"(?:mo?u)?sta(?:s|c)he?(?: me)? (.*)", async msg =>
            {
                var type = _random.Next(2);
                var mustachify = string.Format("http://mustachify.me/{0}?src=", type);
                var imagery = msg.Match[1];
                if (_httpRegex.IsMatch(imagery))
                {
                    await msg.Send(mustachify + imagery);
                }
                else
                {
                    await ImageMe(msg, imagery, url => msg.Send(mustachify + url), false, true);
                }
                
            });
        }
        
        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot image me <query> - The Original. Queries Google Images for <query> and returns a random top result.",
                "mmbot animate me <query> - The same thing as `image me`, except adds a few parameters to try to return an animated GIF instead.",
                "mmbot mustache me <url> - Adds a mustache to the specified URL.",
                "mmbot mustache me <query> - Searches Google Images for the specified query and mustaches it."
            };
        }

        private async Task ImageMe(IResponse<TextMessage> msg, string query, Action<string> cb, bool animated = false, bool faces = false )
        {
            var res = await msg.Http("http://ajax.googleapis.com/ajax/services/search/images")
                .Query(new
                {
                    v = "1.0",
                    rsz = "8",
                    q = query,
                    safe = "active",
                    imgtype = faces ? "face" : animated ? "animated" : null
                })
                .GetJson();

            dynamic images = res;
            try
            {
                images = images.responseData.results;
                if (images.Count > 0)
                {
                    var image = msg.Random(images);
                    cb(string.Format("{0}#.png", image.unescapedUrl));
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
