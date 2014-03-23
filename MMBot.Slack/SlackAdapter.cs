using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;

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

        public SlackAdapter(Robot robot, ILog logger, string adapterId) : base(robot, logger, adapterId)
        {
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
                helpSb.AppendLine("  MMBOT_SLACK_TOKEN: This is the service token you are given when you add MMBot to your Team Services.");
                helpSb.AppendLine("  MMBOT_SLACK_BOTNAME: Optional. What your Hubot is called on Slack. If you entered slack-hubot here, you would address your bot like slack-hubot: help. Otherwise, defaults to mmbot");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
                return;
            }
            _isConfigured = true;
        }
        
        public async override Task Run()
        {
            
        }

        public async override Task Close()
        {
            
        }
    }
}
