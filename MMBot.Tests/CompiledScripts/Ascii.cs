using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MMBot.Scripts;

namespace MMBot.Tests.CompiledScripts
{
    // Ported from https://github.com/rbwestmoreland/Jabbot/blob/master/Jabbot.Sprockets.Community/AsciiSprocket.cs

    public class Ascii : IMMBotScript
    {
        private const string Url = "http://asciime.heroku.com/generate_ascii?s={0}";

        public void Register(Robot robot)
        {
            robot.Respond(@"(ascii)( me)? (.*)", async msg =>
            {
                var query = msg.Match[3];

                await AsciiMeCore(msg, query);
            });
        }

        private static async Task AsciiMeCore(IResponse<TextMessage> msg, string query)
        {
            var res = await msg.Http(String.Format(Url, query))
                .Get();

            try
            {
                await res.Content.ReadAsStringAsync().ContinueWith(async readTask =>
                {
                    await msg.Send(readTask.Result);
                });
            }
            catch (Exception)
            {
                msg.Send("erm....issues, move along").Wait();
            }
        }

        public IEnumerable<string> GetHelp()
        {
            return new[] { "mmbot ascii me <query> - Returns ASCII art of the query text." };
        }
    }
}