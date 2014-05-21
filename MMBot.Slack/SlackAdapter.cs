using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Owin;
using MMBot.Router;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MMBot.Slack
{
    public class SlackAdapter : Adapter
    {
        private enum ChannelModes
        {
            Blacklist,
            Whitelist
        }

        private string _team;
        private string _token;
        private string _slackBotName;
        private bool _isConfigured;
        private ChannelModes _channelMode = ChannelModes.Blacklist;
        private string[] _channels;
        private bool _linkNames;
        private IDictionary<string, string> _channelMapping = new Dictionary<string, string>();
        private bool _sendViaPostMessageApi;
        private string _icon;
        private string _userToken;

        public SlackAdapter(ILog logger, string adapterId) : base(logger, adapterId)
        {
            
        }


        public override void Initialize(Robot robot)
        {
            base.Initialize(robot);

            if (Robot.Router is NullRouter)
            {
                Logger.Warn("The Slack adapter currently requires a Router to be configured. Please setup a router e.g. MMBot.Nancy.");
                return;
            }

            _team = robot.GetConfigVariable("MMBOT_SLACK_TEAM");
            _token = robot.GetConfigVariable("MMBOT_SLACK_TOKEN");
            _slackBotName = robot.GetConfigVariable("MMBOT_SLACK_BOTNAME") ?? robot.Name;
            _sendViaPostMessageApi = bool.Parse(robot.GetConfigVariable("MMBOT_SLACK_USEPOSTMESSAGE") ?? "false");
            _userToken = robot.GetConfigVariable("MMBOT_SLACK_USERTOKEN");
            _icon = robot.GetConfigVariable("MMBOT_SLACK_ICON") ?? "https://raw.githubusercontent.com/mmbot/mmbot/master/Docs/Images/mmbot.logo.48x48.png";
            Enum.TryParse(robot.GetConfigVariable("MMBOT_SLACK_CHANNELMODE") ?? "blacklist", true, out _channelMode);
            _channels = (Robot.GetConfigVariable("MMBOT_SLACK_CHANNELS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            _linkNames = bool.Parse(Robot.GetConfigVariable("MMBOT_SLACK_LINK_NAMES") ?? "false");

            var invalid = false;
            if (_sendViaPostMessageApi && string.IsNullOrEmpty(_userToken))
            {
                Logger.Error("Slack Adapter - if you enable USEPOSTMESSAGE you must provide a USERTOKEN. Get this from the api documentation page");
                invalid = true;
            }

            if (string.IsNullOrWhiteSpace(_team) || string.IsNullOrWhiteSpace(_token) || invalid)
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The Slack adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the Slack adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_SLACK_TEAM: This is your team's Slack subdomain. For example, if your team is https://myteam.slack.com/, you would enter myteam here");
                helpSb.AppendLine("  MMBOT_SLACK_TOKEN: This is the service token you are given when you add Hubot to your Team Services.");
                helpSb.AppendLine("  MMBOT_SLACK_BOTNAME: Optional. What your mmbot is called on Slack. If you entered slack-hubot here, you would address your bot like slack-hubot: help. Otherwise, defaults to mmbot");
                helpSb.AppendLine("  MMBOT_SLACK_USEPOSTMESSAGE: Optional. If true, respond in Slack using the post message api instead of the Hubot adapter. This is more reliable in some ways and works with slack commands etc. Default is false");
                helpSb.AppendLine("  MMBOT_SLACK_USERTOKEN: Optional. Required if USEPOSTMESSAGE is enabled. You must get a user token from the API documentation pages");
                helpSb.AppendLine("  MMBOT_SLACK_ICON: Optional. The URL of the icon to use when USEPOSTMESSAGE is enabled");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
                return;
            }
         
            _isConfigured = true;

            Logger.Info("The Slack adapter is connected");
        }

        public async override Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);
            

            if (messages == null)
            {
                return;
            }

            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                if (_sendViaPostMessageApi)
                {
                    await PostMessageViaPostMessageApi(envelope, message);
                }
                else
                {
                    await PostMessageViaHubot(envelope, message);
                }
            }
        }

        private async Task PostMessageViaHubot(Envelope envelope, string message)
        {
            var args = JsonConvert.SerializeObject(new
            {
                username = Robot.Name,
                channel =
                    string.IsNullOrEmpty(envelope.User.Room)
                        ? envelope.User.Name
                        : _channelMapping.GetValueOrDefault(envelope.User.Room, envelope.User.Room),
                text = message,
                link_names = _linkNames ? 1 : 0
            });

            await Post("/services/hooks/hubot", args);
        }

        private async Task PostMessageViaPostMessageApi(Envelope envelope, string message)
        {
            try
            {
                var channel =
                    string.IsNullOrEmpty(envelope.User.Room) || envelope.User.Room.ToLower() == "#directmessage"
                        ? envelope.User.Name
                        : _channelMapping.GetValueOrDefault(envelope.User.Room, envelope.User.Room);

                var client = new HttpClient
                {
                    BaseAddress = new Uri(string.Format("https://{0}.slack.com", _team)),

                };
                var response = await
                    client.PostAsync(new Uri(string.Format("api/chat.postMessage?token={0}&channel={1}&text={2}&username={3}&parse=full&link_names=1&unfurl_links=1&icon_url={4}&pretty=1", 
                        _userToken,
                        WebUtility.UrlEncode(channel),
                        WebUtility.UrlEncode(message),
                        WebUtility.UrlEncode(Robot.Name),
                        WebUtility.UrlEncode(_icon)
                        ), UriKind.Relative),
                        new StringContent(string.Empty));
                
                var content = await response.Content.ReadAsStringAsync();
                var json = JToken.Parse(content);
                if (!json["ok"].Value<bool>()) {
                    throw new Exception(string.Format("Error returned from slack '{0}'", json["error"].Value<string>()));
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Slack adapter - error sending message via postMessage: {0}", e.Message), e);
            }
        }

        public async override Task Reply(Envelope envelope, params string[] messages)
        {
            foreach(var message in messages)
            {
                await Send(envelope, string.Format("{0}:{1}", envelope.User.Name, message));
            }
        }
        
        private async Task Post(string url, string args)
        {
            try
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(string.Format("https://{0}.slack.com", _team)),
                
                };
                await
                    client.PostAsync(new Uri(string.Format("{0}?token={1}", url, _token), UriKind.Relative),
                        new StringContent(args));
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("Slack adapter - error sending message: {0}", e.Message), e);
            }
        }

        public async override Task Run()
        {
            if (!_isConfigured)
            {
                throw new AdapterNotConfiguredException();
            }

            Robot.Router.Post("/Slack/hubot/slack-webhook", async context =>
            {
                try
                {
                    Logger.Info("Incoming message received from Slack");

                    var form = (await context.FormAsync());
                    var hubotMsg = form["text"];
                    var roomName = form["channel_name"];

                    if (!string.IsNullOrWhiteSpace(hubotMsg) &&
                        ((_channelMode == ChannelModes.Blacklist &&
                          !_channels.Contains(roomName, StringComparer.InvariantCultureIgnoreCase)) ||
                         (_channelMode == ChannelModes.Whitelist &&
                          _channels.Contains(roomName, StringComparer.InvariantCultureIgnoreCase))))
                    {
                        hubotMsg = WebUtility.HtmlDecode(hubotMsg);
                    }
            

                    var author = GetAuthor(form);
                    // author = self.robot.brain.userForId author.id, author
                    _channelMapping[author.Room] = form["channel_id"];

                    if(!string.IsNullOrWhiteSpace(hubotMsg) && author != null) {
                        // Pass to the robot
                        Receive(new TextMessage(author, hubotMsg));

                        // Just send back an empty reply, since our actual reply,
                        // if any, will be async above
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error receiving Slack message", e);
                }
            });

            Robot.Router.Post("/Slack/slack-command", async context =>
            {
                try
                {
                    Logger.Info("Incoming command message received from Slack");

                    var form = (await context.FormAsync());
                    var hubotMsg = form["text"];
                    var roomName = form["channel_name"];
                    var command = form["command"];

                    if (!string.IsNullOrWhiteSpace(hubotMsg) &&
                        ((_channelMode == ChannelModes.Blacklist &&
                          !_channels.Contains(roomName, StringComparer.InvariantCultureIgnoreCase)) ||
                         (_channelMode == ChannelModes.Whitelist &&
                          _channels.Contains(roomName, StringComparer.InvariantCultureIgnoreCase))))
                    {
                        hubotMsg = WebUtility.HtmlDecode(hubotMsg);
                    }


                    var author = GetAuthor(form);
                    // author = self.robot.brain.userForId author.id, author
                    _channelMapping[author.Room] = form["channel_id"];

                    if (!string.IsNullOrWhiteSpace(hubotMsg) && author != null)
                    {
                        // Pass to the robot
                        Receive(new TextMessage(author, Robot.Alias ?? Robot.Name + " " + hubotMsg));

                        // Just send back an empty reply, since our actual reply,
                        // if any, will be async above
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Error receiving Slack message", e);
                }
            });
        }

        private User GetAuthor(IFormCollection form)
        {
            
            return new User(
                form["user_id"],
                form["user_name"],
                Robot.GetUserRoles(form["user_name"]),
                form["channel_name"],
                Id);
        }

        public async override Task Close()
        {
            
        }
    }
}
