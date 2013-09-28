using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Youtube : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"(youtube|yt)( me)? (.*)", async msg =>
            {
                var query = msg.Match[3];
                var res = await msg.Http("http://gdata.youtube.com/feeds/api/videos")
                    .Query(new Dictionary<string, string>
                    {
                        {"orderBy", "relevance"},
                        {"max-results", "15"},
                        {"alt", "json"},
                        {"q", query}
                    })
                    .GetJson();

                var videos = res.feed.entry;

                if (videos == null)
                {
                    await msg.Send(string.Format("No video results for \"{0}\"", query));
                    return;
                }

                dynamic video = msg.Random(videos);
                foreach (var link in video.link)
                {
                    if ((string) link.rel == "alternate" || (string) link.type == "text/html")
                    {
                        await msg.Send((string) link.href);
                    }
                }
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] {"mmbot youtube me <query> - Searches YouTube for the query and returns the video embed link."};
        }
    }
}
