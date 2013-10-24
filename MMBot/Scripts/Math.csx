
var robot = Require<Robot>();

robot.Respond(@"(calc|calculate|calculator|convert|math|maths)( me)? (.*)", msg =>
	{
	    msg
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
        .GetJson((err, res, body) => 
        {
        	if(err != null)
        	{
        		msg.Send("Could not compute");
        	}
        	else 
        	{
        		msg.Send((string)body["rhs"] ?? "Could not compute");
        	}
        });
	});

robot.AddHelp(
    "mmbot math me <expression> - Calculate the given expression.",
	"mmbot convert me <expression> to <units> - Convert expression to given units."
	);