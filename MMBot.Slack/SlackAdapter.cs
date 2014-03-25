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

        public SlackAdapter(ILog logger, string adapterId) : base(logger, adapterId)
        {
            
        }


        public override void Initialize(Robot robot)
        {
            if (Robot.Router is NullRouter)
            {
                Logger.Warn("The Slack adapter currently requires a Router to be configured. Please setup a router e.g. MMBot.Nancy.");
                return;
            }

            base.Initialize(robot);

            _team = robot.GetConfigVariable("MMBOT_SLACK_TEAM");
            _token = robot.GetConfigVariable("MMBOT_SLACK_TOKEN");
            _slackBotName = robot.GetConfigVariable("MMBOT_SLACK_BOTNAME") ?? robot.Name;
            Enum.TryParse(robot.GetConfigVariable("MMBOT_SLACK_CHANNELMODE") ?? "blacklist", true, out _channelMode);
            _channels = (Robot.GetConfigVariable("MMBOT_SLACK_CHANNELS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            _linkNames = bool.Parse(Robot.GetConfigVariable("MMBOT_SLACK_LINK_NAMES") ?? "false");

            if (string.IsNullOrWhiteSpace(_team) || string.IsNullOrWhiteSpace(_token))
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The Slack adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the Slack adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_SLACK_TEAM: This is your team's Slack subdomain. For example, if your team is https://myteam.slack.com/, you would enter myteam here");
                helpSb.AppendLine("  MMBOT_SLACK_TOKEN: This is the service token you are given when you add Hubot to your Team Services.");
                helpSb.AppendLine("  MMBOT_SLACK_BOTNAME: Optional. What your mmbot is called on Slack. If you entered slack-hubot here, you would address your bot like slack-hubot: help. Otherwise, defaults to mmbot");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
                return;
            }
            _isConfigured = true;
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
                var escapedMessage = WebUtility.HtmlEncode(message);
                var args = JsonConvert.SerializeObject(new
                {
                    username = Robot.Name,
                    channel =
                        string.IsNullOrEmpty(envelope.User.Room)
                            ? envelope.User.Name
                            : _channelMapping[envelope.User.Room],
                    text = escapedMessage,
                    link_names = _linkNames ? 1 : 0
                });

                await Post("/services/hooks/hubot", args);
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
            var client = new HttpClient
            {
                BaseAddress = new Uri(string.Format("https://{0}.slack.com", _team)),
                
            };
            await
                client.PostAsync(new Uri(string.Format("{0}?token={1}", url, _token), UriKind.Relative),
                    new StringContent(args));
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
