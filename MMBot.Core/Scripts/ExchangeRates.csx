/**
* <description>
*     Creates an anchievement image with user's gravatar pic and achievement text
* </description>
*
* <commands>
*     mmbot rate from &lt;currency&gt; to &lt;currency&gt; - Gets current exchange rate between two currencies.;
* </commands>
* 
* <notes>
*    Currencies are defined by their ISO code: http://en.wikipedia.org/wiki/ISO_4217
* </notes>
* 
* <author>
*     jamessantiago
* </author>
*/

var robot = Robot>();

robot.Respond(@"rate from (...) to (...)", msg =>
	{
	    msg
	    .Http("http://rate-exchange.appspot.com/currency")
        .Query(new
        {
            from = msg.Match[1],
            to = msg.Match[2]
        })
        .Headers(new Dictionary<string, string>
        {
            {"Accept-Language", "en-us,en;q=0.5"},
            {"Accept-Charset", "utf-8"},
            {"User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:2.0.1) Gecko/20100101 Firefox/4.0.1"}
        })
        .GetJson((err, res, body) => 
        {
        	if(err != null)
        	{
        		msg.Send("Could not retrieve");
        	}
        	else 
        	{
        		var rate = (string)body["rate"];
        		msg.Send(rate != null ? string.Format("1 {0} equals {1} {2}", msg.Match[1], rate, msg.Match[2]) : "Could not retrieve");
        	}
        });
	});