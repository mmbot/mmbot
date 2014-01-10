/**
* <description>
*     Queries the MMBot Scripts github repository and loads scripts
* </description>
*
* <commands>
*     mmbot scripts (query) - lists scripts in the MMBot Scripts repository filtered by (query)
* </commands>
* 
* <author>
*     jamessantiago
* </author>
*/

var robot = Require<Robot>();

robot.Respond("scripts ?(.*)", (msg) =>
  msg.Http("http://petegoo.github.io/mmbot.scripts/catalog.json")
    .GetJson((err, res, body) => {
		if(err != null)
    	{
    		msg.Send("Could not retrieve");
    	}
    	else
    	{
    		var query = msg.Match[1];
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
				var details = string.Format(detailsFormat, name, description, configuration, commands, notes, author);
				if (query.HasValue() && details.ToLower().Contains(query))
				{
					msg.Send(details);
				}
				else if (!query.HasValue())
				{
					msg.Send(details);
				}
    		}
    	}
     })
);