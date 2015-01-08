using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.AspNet.SignalR.Client;
using MMBot.Jabbr.JabbrClient;

namespace MMBot.Jabbr
{
    public class JabbrAdapter : Adapter
    {
        private JabbRClient _client;

        // TODO: Move to environment variables / config
        private string _host;

        private string _nick;
        private string _password;
        private string[] _rooms;
        private string[] _logRooms;
        private bool _isConfigured = false;

        public JabbrAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
        }

        private void Configure()
        {
            _host = Robot.GetConfigVariable("MMBOT_JABBR_HOST") ?? "https://jabbr.net";
            _nick = Robot.GetConfigVariable("MMBOT_JABBR_NICK");
            _password = Robot.GetConfigVariable("MMBOT_JABBR_PASSWORD");
            _rooms = (Robot.GetConfigVariable("MMBOT_JABBR_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            _logRooms = (Robot.GetConfigVariable("MMBOT_JABBR_LOGROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            if (_host == null || _nick == null | _password == null || !_rooms.Any())
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The Jabbr adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the Jabbr adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_JABBR_HOST: The host name. Defaults to https://jabbr.net");
                helpSb.AppendLine("  MMBOT_JABBR_NICK: The login name of the bot account on Jabbr, e.g. mmbot");
                helpSb.AppendLine("  MMBOT_JABBR_PASSWORD: The password of the bot account on Jabbr");
                helpSb.AppendLine("  MMBOT_JABBR_ROOMS: A comma separated list of room names that mmbot should join");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
            }
            else
            {
                _isConfigured = true;
            }
        }

        public override void Initialize(Robot robot)
        {
            base.Initialize(robot);
            Configure();
        }

        private void SetupJabbrClient()
        {
            if (_client != null)
            {
                return;
            }

            _client = new JabbRClient(_host)
            {
                AutoReconnect = true
            };

            _client.MessageReceived += ClientOnMessageReceived;

            _client.UserJoined += OnUserJoined;

            _client.UserLeft += OnUserLeft;

            _client.PrivateMessage += OnPrivateMessage;

            _client.TopicChanged += OnTopicChanged;
        }

        void OnTopicChanged(string room, string topic, string who)
        {
            Logger.Info(string.Format("{0} has changed the topic for {1} to {2}", who, room, topic));

            var user = Robot.GetUser(who, who, room, Id);
            Task.Run(() => Robot.Receive(new TopicMessage(user, topic)));
        }

        private void OnPrivateMessage(string @from, string to, string message)
        {
            Logger.Info(string.Format("*PRIVATE* {0} -> {1} ", @from, message));

            var user = Robot.GetUser(@from, @from, null, Id);

            if (user.Name != _nick)
            {
                Task.Run(() =>
                Robot.Receive(new TextMessage(user, string.Format("{0} {1}", to, message))));
            }
        }

        private void OnUserLeft(User user, string room)
        {
            Logger.Info(string.Format("{0} left {1}", user.Name, room));

            Task.Run(() => Robot.Receive(new LeaveMessage(user)));
        }

        private void OnUserJoined(User user, string room, bool isOwner)
        {
            Logger.Info(string.Format("{0} joined {1}", user.Name, room));
            Task.Run(() => Robot.Receive(new EnterMessage(user)));
        }

        private void OnClientStateChanged(StateChange state)
        {
            if (state.NewState == ConnectionState.Disconnected)
            {
                Logger.Warn("Jabbr client is disconnected");
            }
            else
            {
                Logger.Info(string.Format("Jabbr client is {0}", state.NewState));
            }
        }

        private void ClientOnMessageReceived(JabbrClient.Models.Message message, string room)
        {
            Logger.Info(string.Format("[{0}] {1}: {2}", message.When, message.User.Name, message.Content));

            // TODO: implement user lookup
            //user = self.robot.brain.userForName msg.name
            //unless user?
            //    id = (new Date().getTime() / 1000).toString().replace('.','')
            //    user = self.robot.brain.userForId id
            //    user.name = msg.name

            var user = Robot.GetUser(message.User.Name, message.User.Name, room, Id);

            //TODO: Filter out messages from mmbot itself using the current nick
            if (user.Name != _nick)
            {
                Task.Run(() =>
                Robot.Receive(new TextMessage(user, message.Content)));
            }
        }

        public override async Task Run()
        {
            if (!_isConfigured)
            {
                throw new AdapterNotConfiguredException();
            }
            Logger.Info(string.Format("Logging into JabbR..."));

            SetupJabbrClient();

            var result = await _client.Connect(_nick, _password);

            _client.StateChanged += OnClientStateChanged;

            Logger.Info(string.Format("Logged on successfully. {0} is currently in the following rooms:", _nick));
            foreach (var room in result.Rooms)
            {
                Logger.Info(string.Format(" - " + room.Name + (room.Private ? " (private)" : string.Empty) + (_logRooms.Contains(room.Name) ? " (logging)" : string.Empty)));
                Rooms.Add(room.Name);
                if (_logRooms.Contains(room.Name))
                    LogRooms.Add(room.Name);
            }

            foreach (var room in _rooms.Union(_logRooms).Distinct().Where(room => !result.Rooms.Select(r => r.Name).Contains(room)))
            {
                try
                {
                    await _client.JoinRoom(room);
                    Rooms.Add(room);
                    Logger.Info(string.Format("Successfully joined room {0}", room));
                }
                catch (Exception e)
                {
                    Logger.Info(string.Format("Could not join room {0}: {1}", room, e.Message));
                }
            }

            foreach (var logRoom in _logRooms)
            {
                if (!LogRooms.Contains(logRoom)) LogRooms.Add(logRoom);
            }
        }

        public override Task Close()
        {
            _client.Disconnect();
            _client.MessageReceived -= ClientOnMessageReceived;
            _client.UserJoined -= OnUserJoined;
            _client.UserLeft -= OnUserLeft;
            _client.PrivateMessage -= OnPrivateMessage;
            return TaskAsyncHelper.Empty;
        }

        public override Task Topic(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (envelope != null && envelope.Room != null)
            {
                var message = string.Join(" ", messages);
                return _client.Send(string.Format("/topic {0}", message.Substring(0, Math.Min(80, message.Length))), envelope.Room);
            }

            return Task.FromResult(0);
        }

        public override async Task Topic(string roomName, AdapterArguments adapterArgs, params string[] messages)
        {
            var room = await _client.GetRoomInfo(roomName);
            if (room != null)
            {
                var message = string.Join(" ", messages);
                await _client.Send(string.Format("/topic {0}", message.Substring(0, Math.Min(80, message.Length))), room.Name);
            }
        }

        public override async Task Send(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (messages == null)
            {
                return;
            }

            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                if (!string.IsNullOrEmpty(envelope.User.Room))
                {
                    await _client.Send(message, envelope.User.Room);
                }
                else
                {
                    await _client.SendPrivateMessage(envelope.User.Name, message);
                }
            }
        }

        public override async Task Reply(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                await _client.Send(string.Format("@{0} {1}", envelope.User.Name, message), envelope.User.Room);
            }
        }
    }
}