/**
* <description>
*     Sets up announcements when github events occur like a push or an issue is created
* </description>
*
* <commands>
*     mmbot set repo alert (push|issues|pull request) on owner/repo - Sets up an alert to announce in the room when an event happens on github
*     mmbot remove repo alert (push|issues|pull request|*) on owner/repo - Removes a github alert
*     mmbot list [all] repo alerts - Lists all the github repo alerts that have been setup. all will list thos for all rooms
* </commands>
* 
* <notes>
*    Uses the router. Needs to have the router correctly configured. For information on event types see http://developer.github.com/v3/activity/events/types/
*    You must install the Octokit package for this script to run (type "nuget install Octokit -o packages" from your installation directory).
* </notes>
* 
* <author>
*     petegoo
* </author>
*
* <configuration>
*    MMBOT_ROUTER_PORT
*    MMBOT_ROUTER_HOSTNAME
*    MMBOT_ROUTER_ENABLED
*    MMBOT_GITHUB_USERNAME
*    MMBOT_GITHUB_PASSWORD
* </configuration>
*/

using Octokit;
using Octokit.Internal;
using System.Net;
using System.Net.Http.Headers;

var robot = Require<Robot>();

var githubUserName = robot.GetConfigVariable("MMBOT_GITHUB_USERNAME");
var githubPassword = robot.GetConfigVariable("MMBOT_GITHUB_PASSWORD");
var routerHostName = robot.GetConfigVariable("MMBOT_ROUTER_HOSTNAME");
var routerPort = robot.GetConfigVariable("MMBOT_ROUTER_PORT");

var hookUrl = string.Format("http://{0}:{1}/github/webhook", routerHostName, routerPort);

var client = routerHostName == null 
	       ? new GitHubClient(new ProductHeaderValue("mmbot"))
	       : new GitHubClient(new ProductHeaderValue("mmbot"), new InMemoryCredentialStore(new Credentials(githubUserName, githubPassword)));

var subscriptions = LoadSubscriptions();

// Setup the Router
robot.Router.Post("/github/webhook/", context => {
	try{
		robot.Logger.Info("Got a github webhook call!!");
		var payload = context.Form()["payload"].ToJson();
		foreach(var sub in subscriptions.Where(s => string.Equals(s.Id, payload["id"]) && s.Events.Contains(context.Request.Headers["X-GitHub-Event"], StringComparer.InvariantCultureIgnoreCase))) {
			PrintCommits(payload, sub.AdapterId, sub.Room);
		}	
	}
	catch(Exception ex) {
		robot.Logger.Error("Error receiving github webhook", ex);
	}
});

// Setup the commands
robot.Respond(@"set repo alert (push|issues|pull_request) on ([^/]+)/([^/]+)", msg => {
	SetupWebServiceHook(msg, msg.Match[1], msg.Match[2], msg.Match[3]);
});

robot.Respond(@"remove repo alert (push|issues|pull_request|\*) (on|from) ([^/]+)/([^/]+)", msg => {
	RemoveWebServiceHook(msg, msg.Match[1], msg.Match[3], msg.Match[4]);
});

robot.Respond(@"list( all)? repo alerts", msg => {
	bool all = !string.IsNullOrEmpty(msg.Match[1]);

	var filteredSubs = subscriptions.Where(sub => all || (string.Equals(sub.AdapterId, msg.Message.User.AdapterId, StringComparison.InvariantCultureIgnoreCase) && string.Equals(msg.Message.User.Room, sub.Room, StringComparison.InvariantCultureIgnoreCase)));

	var report = string.Join(Environment.NewLine, 
		filteredSubs.Select(sub => string.Format("announce {0} events on {1}/{2} to {3}/{4}", 
			string.Join(", ", sub.Events),
			sub.Owner,
			sub.Repo,
			sub.AdapterId,
			sub.Room
		)));

	if(string.IsNullOrEmpty(report)){
		msg.Send(all ? "There are no alerts" : "There are no matching alerts");
	}
	else{
		msg.Send(report);
	}	
});

private void SetupWebServiceHook(MMBot.IResponse<TextMessage> msg, string eventName, string owner, string repo) {
	try {		

		var hook = GetHook(msg, owner, repo);

        if(hook != null) {
        	// We have an existing hook so update it if necessary
    		if(hook["events"].Any(ev => ev.ToString().ToLower() == eventName.ToLower())) {
    			msg.Send("The event subscription already exists");
    			return;
    		}
        	
        	((JArray)hook["events"]).Add(new JValue(eventName));

        	UpdateHook(msg, owner, repo, hook);

        	var sub = GetSavedSubscription(hook["id"].ToString());

        	if(sub == null){
        		AddSubscription(msg, hook["id"].ToString(), owner, repo, hook["events"].Select(e => e.ToString()).ToArray());
        	}
        	else {
    			sub.AddEvent(eventName);
    			SaveSubscriptions();
        	}

        	return;
        }

    	// No hook exists so create one
        var createHookResult = client.Connection.PostAsync<object>(
        	GetApiUrl(owner, repo),
        	new {
        		name = "web",
	        	config = new {
	    			url = hookUrl,
	    			content_type = "form"
        		},
        		events = new []{
    				eventName
    			}
        	},
        	"application/json",
        	"application/json"
        	).Result;

        	if(createHookResult.StatusCode != HttpStatusCode.Created){
        		throw new Exception(string.Format("Create hook returned a status code of {0}", createHookResult.StatusCode.ToString()));
        	}
        	else {
        		hook = createHookResult.Body.ToJson();
        		AddSubscription(msg, hook["id"].ToString(), owner, repo, hook["events"].Select(e => e.ToString()).ToArray());

    			msg.Send("The event subscription has been created");
        	}
    }
    catch(Exception ex){
		msg.Send(string.Format("Could not setup service hook. You can still do it manually by going to the repo settings and entering a 'WebHook URL' of {0}\r\nError:{1}", hookUrl, ex.Message));
    }
}

private void RemoveWebServiceHook(MMBot.IResponse<TextMessage> msg, string eventName, string owner, string repo) {
	try {

		var hook = GetHook(msg, owner, repo);

		if(hook == null){
			msg.Send("Could not find an existing web hook subscription. Check the repo settings on github.");
			return;
		}

		var registeredEvents = ((JArray)hook["events"]).Select(e => e.ToString());

		if(eventName.ToLower() == "*" || (registeredEvents.Count() == 1 && registeredEvents.First().ToLower() == eventName.ToLower())){
			// Remove the subscription altogether
			var deleteHookResult = client.Connection.DeleteAsync(GetApiUrl(owner, repo, hook["id"].ToString())).Result;

			if(deleteHookResult != HttpStatusCode.OK && deleteHookResult != HttpStatusCode.NoContent){
				throw new Exception(string.Format("Delete hook returned a status code of {0}", deleteHookResult.ToString()));
			}
        	else {
        		RemoveSavedSubscription(hook["id"].ToString());
    			msg.Send("The event subscription has been removed");
        	}
        	return;
		}

		((JArray)hook["events"]).Remove(new JValue(eventName));

		UpdateHook(msg, owner, repo, hook);

		var sub = GetSavedSubscription(hook["id"].ToString());

    	if(sub == null){
    		AddSubscription(msg, hook["id"].ToString(), owner, repo, hook["events"].Select(e => e.ToString()).ToArray());
    	}
    	else {
			sub.RemoveEvent(eventName);
			SaveSubscriptions();
    	}
    }
    catch(Exception ex){
		msg.Send(string.Format("Could not remove service hook. Try again later \r\nError:{1}", hookUrl, ex.Message));
    }
}

private JToken GetHook(MMBot.IResponse<TextMessage> msg, string owner, string repo){

    var hooksResult = client.Connection.GetAsync<string>(GetApiUrl(owner, repo)).Result;

    if(hooksResult.StatusCode == HttpStatusCode.NotFound){
		msg.Send(string.Format("Could not locate the repo {0}/{1}. Please check the name and try again", owner, repo));
		throw new Exception(string.Format("No such repo {0}/{1}.", owner, repo));
    }

    var hooks = hooksResult.Body.ToJson();

    var hook = hooks.FirstOrDefault(i => i["name"].ToString().ToLower() == "web" && i["config"]["url"].ToString().ToLower() == hookUrl);

    return hook;
}

private void UpdateHook(MMBot.IResponse<TextMessage> msg, string owner, string repo, JToken hook){

	var editHookResult = client.Connection.PatchAsync<object>(
	GetApiUrl(owner, repo, hook["id"].ToString()),
	hook.ToString()).Result;

	if(editHookResult.StatusCode != HttpStatusCode.OK){
		throw new Exception(string.Format("Update hook returned a status code of {0}", editHookResult.StatusCode.ToString()));
	}
	else {
		msg.Send("The event subscription has been updated");
	}
	return;
}

private Uri GetApiUrl(string owner, string repo, string id = null){
	return id == null 
		? new Uri(string.Format("/repos/{0}/{1}/hooks", owner, repo), UriKind.Relative)
		: new Uri(string.Format("/repos/{0}/{1}/hooks/{2}", owner, repo, id), UriKind.Relative);
}

private void PrintCommits(JToken payload, string adapterId, string room) {
	if(!payload["commits"].Any())
		return;

	var sb = new StringBuilder();

	sb.AppendLine(payload["compare"].ToString());
	sb.AppendLine(string.Format("The following commits were pushed to {0}/{1}", payload["repository"]["owner"]["name"], payload["repository"]["name"]));

	foreach(var commit in payload["commits"]){
		string message = commit["message"].ToString().Replace(Environment.NewLine, " ");
		sb.AppendLine(string.Format("{0} {1} {2}", 
			commit["id"].ToString().Substring(0, 7), 
			commit["author"]["name"].ToString(), 
			message.Substring(0, Math.Min(30, message.Length))));
	}

	var report = sb.ToString();

	robot.Logger.Info(report);

	robot.Speak(adapterId, room, report);	
}

private void AddSubscription(MMBot.IResponse<TextMessage> msg, string id, string owner, string repo, params string[] eventNames) {
	var sub = new GithubHookSubscription {
		Id = id,
		Owner = owner,
		Repo = repo,
		Events = eventNames,
		AdapterId = msg.Message.User.AdapterId,
		Room = msg.Message.User.Room
	};

	subscriptions.Add(sub);
	SaveSubscriptions();
}

private GithubHookSubscription GetSavedSubscription(string id) {
	return subscriptions.FirstOrDefault(sub => sub.Id == id);
}

private void RemoveSavedSubscription(string id){
	subscriptions.RemoveAll(s => s.Id == id);
	SaveSubscriptions();
}

private List<GithubHookSubscription> LoadSubscriptions() {
	return robot.Brain.Get<List<GithubHookSubscription>>("GithubNotifications").Result ?? new List<GithubHookSubscription>();
}

private void SaveSubscriptions() {
	robot.Brain.Set("GithubNotifications", subscriptions);
}

public class GithubHookSubscription {
	public string Id { get;set; }
	public string Owner { get; set; }
	public string Repo { get; set; }
	public string[] Events { get; set; }
	public string AdapterId { get; set; }
	public string Room { get; set; }

	public void AddEvent(string eventName) {
		Events = Events.Concat(new[]{eventName}).Distinct().ToArray();
	}

	public void RemoveEvent(string eventName) {
		Events = Events.Except(new[]{eventName}).Distinct().ToArray();
	}
}