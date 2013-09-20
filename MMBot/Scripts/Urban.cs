using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MMBot.Scripts
{
    // Ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/urban.coffee

    public class Urban : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"what ?is ([^\?]*)[\?]*", async msg =>
            {
                string query = msg.Match[1];

                var res =
                    await msg.Http(string.Format("http://api.urbandictionary.com/v0/define?term={0}", query)).GetJson();
                if (res.list.Count == 0)
                {
                    await msg.Send("I don't know what \"" + query + "\" is");
                    return;
                }

                var entry = res.list[0];
                await msg.Send((string)entry.definition);

                //var sounds = res.sounds;
                //if (sounds != null && sounds.Count != 0)
                //{
                //    await msg.Send(string.Join(" ", ((JArray) sounds).Select(s => s.ToString())));
                //}
            });


            robot.Respond(@"(urban)( define)?( example)?( me)? (.*)", async msg =>
            {
                string query = msg.Match[5];

                var res =
                    await msg.Http(string.Format("http://api.urbandictionary.com/v0/define?term={0}", query)).GetJson();
                if (res.list.Count == 0)
                {
                    await msg.Send("\"" + query + "\" not found");
                    return;
                }
                var entry = res.list[0];
                if (!string.IsNullOrWhiteSpace(msg.Match[3]))
                {
                    await msg.Send((string)entry.example);
                }
                else
                {
                    await msg.Send((string)entry.definition);
                }
                //var sounds = res.sounds;
                //if (sounds != null && sounds.Count != 0)
                //{
                //    await msg.Send(string.Join(" ", ((JArray)sounds).Select(s => s.ToString())));
                //}
            });
        }

        

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "hubot what is <term>?         - Searches Urban Dictionary and returns definition",
                "hubot urban me <term>         - Searches Urban Dictionary and returns definition",
                "hubot urban define me <term>  - Searches Urban Dictionary and returns definition",
                "hubot urban example me <term> - Searches Urban Dictionary and returns example"
            };
        }
    }
}
