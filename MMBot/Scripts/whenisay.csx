var robot = Require<Robot>();

robot.Respond(@"when i say (.*) you say (.*)", msg => {
	var matcher = msg.Match[1];
	var response = msg.Match[2];

	robot.Respond(matcher, msg2 => msg2.Send(response));

	var savedResponses = robot.Brain.Get<Dictionary<string, string>>("WhenISay").Result ?? new Dictionary<string, string>(); 
	
	savedResponses[matcher] = response;

	robot.Brain.Set("WhenISay", savedResponses);

	msg.Send(msg.Random(new[]{"Got it boss!", "No worries", "I'll try!", "Affirmative", "Let's do it!"}));
});

// Setup the previously saved responses when the script loads
var previouslySavedResponses = robot.Brain.Get<Dictionary<string, string>>("WhenISay").Result ?? new Dictionary<string, string>(); 

foreach(var kvp in previouslySavedResponses){
	robot.Respond(kvp.Key, msg2 => msg2.Send(kvp.Value));
}

robot.Respond(@"what did i tell you to say\??", msg => {
	var savedResponses = robot.Brain.Get<Dictionary<string, string>>("WhenISay").Result ?? new Dictionary<string, string>(); 

	int count = 1;
	var sb = new StringBuilder();

	if(!savedResponses.Any()){
		msg.Send("You haven't told me to say anything yet.");
	}

	foreach(var kvp in savedResponses){
		sb.AppendLine(string.Format("{0}: {1} => {2}", count, kvp.Key,kvp.Value));
		count++;
	}
	msg.Send(sb.ToString());
});

robot.Respond(@"forget what I told you to say (\d)", msg => {
	var savedResponses = robot.Brain.Get<Dictionary<string, string>>("WhenISay").Result ?? new Dictionary<string, string>(); 

	int i = int.Parse(msg.Match[1]);

	if(i > savedResponses.Count()){
		msg.Send("Don't have " + i + " saved responses");
		return;
	}

	var regex = savedResponses.Keys.ElementAt(i-1);
	savedResponses.Remove(regex);

	robot.RemoveListener(regex);

	robot.Brain.Set("WhenISay", savedResponses);
	msg.Send(msg.Random(new[]{"forgotten boss", "forget what ;)", "consider it done"}));


});

robot.AddHelp(
	"mmbot when i say <pattern> you say <response> - tell mmbot to prepare a canned response to a message matching the pattern",
	"mmbot what did i tell you to say - ask mmbot what are the prepared canned responses that it has been told",
	"mmbot forget what did i told you to say <index> - ask mmbot to forget a prepared canned response using it's number"
);