using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.XPath;

namespace MMBot.Scripts
{

    public class Cats : IMMBotScript
    {
        private const string Url = "http://thecatapi.com/api/images/get?format=xml&results_per_page={0}&api_key=MTAzNjQ";

        public void Register(Robot robot)
        {
            robot.Respond(@"(cat|cats)( me)? (.*)?", async msg =>
            {
                int number = 1;
                try
                {
                    number = Int32.Parse(msg.Match[3]);
                }
                catch (Exception) { }
                if (number == 0)
                {
                    number = 1;
                }

                await CatMeCore(msg, number);
            });
        }

        private static async Task CatMeCore(IResponse<TextMessage> msg, int number)
        {
            var xDoc = await msg.Http(string.Format(Url, number))
                .GetXml();

            try
            {
                var urls = xDoc.SelectNodes("//url");
                foreach (XmlNode url in urls)
                {
                    msg.Send(url.InnerText);
                }
            }
            catch (Exception)
            {
                msg.Send("erm....issues, move along");
            }
        }

        public IEnumerable<string> GetHelp()
        {
            return new[]
            {
                "mmbot cat me <number> - Returns a number of cat pictures.",
                "mmbot cat - Returns a cat picture."
            };
        }
    }
}
