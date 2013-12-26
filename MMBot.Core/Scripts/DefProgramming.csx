//ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/defprogramming.coffee

using HtmlAgilityPack;

var robot = Require<Robot>();

robot.Respond(@"def programming", msg =>
{
    msg.Http("http://www.defprogramming.com/random")
        .GetHtml((err, res, htmlDoc) => {
            if (err != null)
            {
                msg.Send("Could not retrieve");
            }
            else
            {
                try {
                    var quote = htmlDoc.DocumentNode.SelectNodes("//cite/a/p").First().FirstChild.InnerText;
                    msg.Send(quote);
                }
                catch
                {
                    msg.Send("Could not retrieve");
                }
            }
            
    });
});

robot.AddHelp(
    "mmbot def programming - returns a random programming quote"
);