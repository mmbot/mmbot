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

var previouslySavedResponses = robot.Brain.Get<Dictionary<string, string>>("WhenISay").Result ?? new Dictionary<string, string>(); 

foreach(var kvp in previouslySavedResponses){
	robot.Respond(kvp.Key, msg2 => msg2.Send(kvp.Value));
}