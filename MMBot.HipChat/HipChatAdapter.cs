using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using HipChat;

namespace MMBot.HipChat
{
    public class HipChatAdapter : Adapter
    {
        private static string _subdomain;
        private static string[] _rooms;
        private static string _nick;
        private static string _password;

        private static bool _isConfigured = false;
        private string _token;
        private HipChatClient _client;


        public HipChatAdapter(Robot robot, ILog logger) : base(robot, logger)
        {
            Configure();
        }

        private void Configure() {
            _subdomain = Robot.GetConfigVariable("MMBOT_HIPCHAT_SUBDOMAIN");
            _token = Robot.GetConfigVariable("MMBOT_HIPCHAT_TOKEN");
            _nick = Robot.GetConfigVariable("MMBOT_HIPCHAT_NICK");
            _password = Robot.GetConfigVariable("MMBOT_HIPCHAT_PASSWORD");
            _rooms = (Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            _isConfigured = _subdomain != null;
        }

        public override async Task Run()
        {
            if (!_isConfigured) {
                throw new AdapterNotConfiguredException();
            }
            Logger.Info(string.Format("Logging into HipChat..."));

            SetupHipChatClient();
        }

        private void SetupHipChatClient()
        {
            if (_client != null) {
                return;
            }
            
        }

        public override Task Close()
        {
            throw new NotImplementedException();
        }
    }
}
