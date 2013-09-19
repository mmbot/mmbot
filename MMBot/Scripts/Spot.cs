using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Services;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Spot : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"spot me winning", msg =>
            {
                msg.Send("http://open.spotify.com/track/77NNZQSqzLNqh2A9JhLRkg");
                msg.Message.Done = true;
            });

            robot.Respond(@"spot me (.*)$", async msg =>
            {
                var q = msg.Match[0].Groups[1].Value;
                var res = await msg.Http("http://ws.spotify.com/search/1/track.json")
                    .Query(new {q})
                    .Get();

                foreach(var t in res.tracks)
                {
                    try
                    {
                        if (t.album.availability.territories.ToString() == "worldwide" || t.album.availability.territories.ToString().IndexOf("NZ") > -1)
                        {
                            msg.Send(string.Format("http://open.spotify.com/track/{0}",
                                t.href.ToString().Replace("spotify:track:", string.Empty)));
                            msg.Message.Done = true;
                            return;
                        }
                    }
                    catch (Exception)
                    {
                        
                    }
                }
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot spot me winning - Show the best track ever on spotify",
                "mmbot spot me <query> - Show the top spotify track result for my query"
            };
        }
    }
}
