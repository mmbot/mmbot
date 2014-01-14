/**
* <description>
*     Queries the MMBot Scripts github repository and loads scripts
* </description>
*
* <commands>
*     mmbot scripts (query) [detailed] - lists scripts in the MMBot Scripts repository filtered by (query)
*     mmbot download script (name) - downloads a script by (name) from the MMBot Scripts repository
* </commands>
* 
* <author>
*     jamessantiago
* </author>
*/

var robot = Require<Robot>();

robot.Respond("download script (.*)", (msg) => {

  msg.Http("http://mmbot.github.io/mmbot.scripts/catalog.json").GetJson((err, res, body) => {
  	  	var scriptName = "";
  		var link = "";
		if(err != null)
    	{
    		msg.Send("Could not retrieve");
    	}
    	else
    	{
    		var script = msg.Match[1];
    		var scriptData = body.Where(d => ((string)d["name"]).ToLower().Trim() == script.ToLower().Trim()).FirstOrDefault();
			if (scriptData != null)
			{
				scriptName = script;
				link = (string)scriptData["link"];
			}
    	}

    	if (link.HasValue())
		{
			msg.Http(link).GetString((ex, resp, data) => {
				string filePath = Path.Combine(Environment.CurrentDirectory, Path.Combine("scripts", string.Format("{0}.{1}", scriptName, "csx")));
				File.WriteAllText(filePath, data);
				robot.LoadScriptFile(scriptName, filePath);
				msg.Send(string.Format("Added script: {0}", scriptName));
			});
		}
		else
		{
			msg.Send(string.Format("Could not find a script named {0}", msg.Match[1]));
		}

	});


	
});



robot.Respond(@"scripts ?([\w\d_-]*)( detailed)?", (msg) =>
  msg.Http("http://mmbot.github.io/mmbot.scripts/catalog.json")
    .GetJson((err, res, body) => {
		if(err != null)
    	{
    		msg.Send("Could not retrieve");
    	}
    	else
    	{
    		var query = msg.Match[1];
    		var detailed = msg.Match[2].HasValue();
    		if (query.ToLower().Trim() == "detailed") {query = null; detailed = true;}
    		bool scriptSent = false;
    		foreach (var script in body)
    		{
    			var name = (string)script["name"];
    			var description = (string)script["description"];
    			var commands = (string)script["commands"];
    			var configuration = (string)script["configuration"];
    			var notes = (string)script["notes"];
    			var author = (string)script["author"];

string detailsFormat = @"
Name:
  {0}

Description:
  {1}

Configuration:
  {2}

Commands:
  {3}

Notes:
  {4}

Author:
  {5}
";
				var details = "";
				if (detailed)
					details = string.Format(detailsFormat, name, description, configuration, commands, notes, author);
				else
					details = string.Format("{0} - {1}", name, description);

				if (query.HasValue() && details.ToLower().Contains(query))
				{
					msg.Send(details);
					scriptSent = true;
				}
				else if (!query.HasValue())
				{
					msg.Send(details);
					scriptSent = true;
				}				
    		}
    		if (!scriptSent)
    		{
    			msg.Send("Could not find a script matching that query");
    		}
    	}
     })
);