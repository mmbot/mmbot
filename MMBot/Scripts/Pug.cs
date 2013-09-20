using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Pug : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"pug me", async msg =>
            {
                var res = await msg.Http("http://pugme.herokuapp.com/random").Get();
                await msg.Send((string)res.pug);
            });

            robot.Respond(@"pug bomb( (\d+))?", async msg =>
            {
                var count = msg.Match[0].Groups.Count > 2 ? msg.Match[0].Groups[2].Value : "5";
                var res = await msg.Http("http://pugme.herokuapp.com/bomb?count=" + count).Get();
                foreach(var pug in res.pugs)
                {
                    await msg.Send((string)pug);
                }
            });

            robot.Respond(@"how many pugs are there", async msg =>
            {
                var res = await msg.Http("http://pugme.herokuapp.com/count").Get();
                await msg.Send(string.Format("There are {0} pugs.", res.pug_count));
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot pug me - Receive a pug",
                "mmbot pug bomb N - get N pugs"
            };
        }
    }
}
