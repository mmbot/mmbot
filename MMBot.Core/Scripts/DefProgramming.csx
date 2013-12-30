/**
* <description>
*     Displays a random quote from def programming
* </description>
*
* <configuration>
*
* </configuration>
*
* <commands>
*     mmbot def programming - returns a random programming quote;
* </commands>
* 
* <notes>
*     Requires HtmlAgilityPack
*     ported from https://github.com/github/hubot-scripts/blob/master/src/scripts/defprogramming.coffee*     
* </notes>
* 
* <author>
*     jamessantiago
* </author>
*/

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
