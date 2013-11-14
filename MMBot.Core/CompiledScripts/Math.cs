using System;
using System.Collections.Generic;
using MMBot.Scripts;

namespace MMBot.CompiledScripts
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
                            q = msg.Match[3]
                        })
                    .Headers(new Dictionary<string, string>
                        {
                            {"Accept-Language", "en-us,en;q=0.5"},
                            {"Accept-Charset", "utf-8"},
                            {"User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:2.0.1) Gecko/20100101 Firefox/4.0.1"}
                        })
                    .GetJson();

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
                "mmbot math me <expression> - Calculate the given expression.",
                "mmbot convert me <expression> to <units> - Convert expression to given units."
            };
        }
    }
}
