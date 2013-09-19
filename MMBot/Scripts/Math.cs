using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMBot.Scripts
{
    public class Math : IMMBotScript
    {
        public void Register(Robot robot)
        {
            robot.Respond(@"(calc|calculate|calculator|convert|math|maths)( me)? (.*)", async msg =>
            {
                dynamic res = await msg
                    .Http("https://www.google.com/ig/calculator")
                    .Query(new
                        {
                            hl = "en",
                            q = msg.Match[0].Groups[3].Value
                        })
                    .Headers(new Dictionary<string, string>
                        {
                            {"Accept-Language", "en-us,en;q=0.5"},
                            {"Accept-Charset", "utf-8"},
                            {"User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:2.0.1) Gecko/20100101 Firefox/4.0.1"}
                        })
                    .Get();

                try
                {
                    await msg.Send((string)res.rhs ?? "Could not compute");
                    return;
                }
                catch (Exception)
                { }
                await msg.Send("Could not compute");
            });
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "hubot math me <expression> - Calculate the given expression.",
                "hubot convert me <expression> to <units> - Convert expression to given units."
            };
        }
    }
}
