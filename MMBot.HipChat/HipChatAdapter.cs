using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;
using agsXMPP.Xml.Dom;
using Common.Logging;

namespace MMBot.HipChat
{
    public class HipChatAdapter : Adapter
    {
        private static string _host;
        private string _confhost;

        private static string _email;
        private static string _password;
        private static string _authToken;
        private static string _rooms;

        private static bool _isConfigured = false;

        private XmppClientConnection _client = null;
        private HipChatAPI _api = null;

        private HipchatViewUserResponse _botUser;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();
        private readonly Dictionary<string, int> _roomMap = new Dictionary<string, int>();
        private readonly Dictionary<string, string> _nicks = new Dictionary<string, string>();
        private readonly List<string> _roomList = new List<string>();

        public HipChatAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
        }

        public override void Initialize(Robot robot)
        {
            base.Initialize(robot);
            Configure();
        }

        private void Configure()
        {
            _host = Robot.GetConfigVariable("MMBOT_HIPCHAT_HOST") ?? "chat.hipchat.com";
            _confhost = Robot.GetConfigVariable("MMBOT_HIPCHAT_CONFHOST") ?? "conf.hipchat.com";
            _email = Robot.GetConfigVariable("MMBOT_HIPCHAT_EMAIL");
            _password = Robot.GetConfigVariable("MMBOT_HIPCHAT_PASSWORD");
            _authToken = Robot.GetConfigVariable("MMBOT_HIPCHAT_AUTHTOKEN");
            _rooms = Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMS");

            if (!string.IsNullOrWhiteSpace(_rooms))
            {
                _roomList.AddRange(_rooms.Trim().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.ToUpper().Trim()));
            }

            if (_email == null || _password == null || _authToken == null)
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The HipCat adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the HipChat adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_HIPCHAT_HOST: The host name defaults to chat.hipchat.com");
                helpSb.AppendLine("  MMBOT_HIPCHAT_CONFHOST: The host name defaults to conf.hipchat.com");
                helpSb.AppendLine("  MMBOT_HIPCHAT_EMAIL: The email of the bot account on HipChat, e.g. mmbot@Bot.net");
                helpSb.AppendLine("  MMBOT_HIPCHAT_PASSWORD: The password of the bot account on HipChat");
                helpSb.AppendLine("  MMBOT_HIPCHAT_AUTHTOKEN: The auth token for the HipChat APIv2");
                helpSb.AppendLine("  MMBOT_HIPCHAT_ROOM_NAMES: The list of specific rooms to enter (comma-delimited), e.g. room1,room2,room3. If not specified, the bot will enter all rooms.");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
            }
            else
            {
                _isConfigured = true;
            }
        }

        public override Task Run()
        {
            if (!_isConfigured)
            {
                throw new AdapterNotConfiguredException();
            }
            Logger.Info(string.Format("Logging into HipChat..."));

            SetupHipChatClient();

            return Task.FromResult(0);
        }

        private void SetupHipChatClient()
        {
            if (_client != null)
            {
                return;
            }

            _client = new XmppClientConnection(_host);
            _client.AutoResolveConnectServer = false;
            _client.OnLogin += OnClientLogin;
            _client.OnMessage += OnClientMessage;
            _client.OnError += OnClientError;
            _client.OnAuthError += OnClientAuthError;
            _client.Resource = "bot";
            _client.UseStartTLS = true;

            _api = new HipChatAPI(_authToken);

            _botUser = _api.ViewUser(_email);

            Logger.Info(string.Format("Connecting to {0}", _host));
            _client.Open(_botUser.XmppJid.Split('@')[0], _password);
            Logger.Info(string.Format("Connected to {0}", _host));

            _client.OnRosterStart += OnClientRosterStart;
            _client.OnRosterItem += OnClientRosterItem;
        }

        private void OnClientAuthError(object sender, Element e)
        {
            Logger.Error("Error authenticating in HipChat client");
        }

        private void OnClientError(object sender, Exception ex)
        {
            Logger.Error("Error in HipChat client", ex);
        }

        private void OnClientMessage(object sender, agsXMPP.protocol.client.Message message)
        {
            if (!String.IsNullOrEmpty(message.Body))
            {
                Console.WriteLine("Message : {0} - from {1}", message.Body, message.From);

                string user;

                if (message.Type != MessageType.groupchat)
                {
                    if (!_roster.TryGetValue(message.From.User, out user))
                    {
                        user = "Unknown User";
                    }
                }
                else
                {
                    user = message.From.Resource;
                }

                if (user == _botUser.Name)
                    return;

                Logger.Info(string.Format("[{0}] {1}: {2}", DateTime.Now, user, message.Body.Trim()));

                var userObj = Robot.GetUser(_nicks.ContainsKey(user) ? _nicks[user] : message.Id, user, message.From.Bare, Id);

                if (userObj.Name != _botUser.MentionName)
                {
                    Task.Run(() =>
                        Robot.Receive(new TextMessage(userObj, message.Body.Trim())));
                }
            }
        }

        public override Task Send(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (messages == null || !messages.Any()) return Task.FromResult(0);

            int roomId;
            if (!_roomMap.TryGetValue(envelope.User.Room, out roomId))
            {
                // There's no public room with the envelopes key, so send this
                // as a private message to the user instead.
                return Reply(envelope, adapterArgs, messages);
            }

            var color = adapterArgs.Color ?? "";
            color = color.ToLowerInvariant();

            if (color != "yellow" &&
                color != "green" &&
                color != "red" &&
                color != "purple" &&
                color != "gray" &&
                color != "random")
                color = "yellow";

            foreach (var message in messages)
            {
                _api.SendRoomNotification(roomId, color, message);
            }

            return Task.FromResult(0);
        }

        public override Task Reply(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (messages == null || !messages.Any()) return Task.FromResult(0);

            var userId = "@" + _nicks[envelope.User.Name];

            foreach (var message in messages)
            {
                _api.PrivateMessageUser(userId, message);
            }

            return Task.FromResult(0);
        }

        public override Task Emote(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (messages == null || !messages.Any()) return Task.FromResult(0);

            foreach (var message in messages.Select(m => "/me " + m))
            {
                var to = new Jid(envelope.User.Room);
                _client.Send(new agsXMPP.protocol.client.Message(to, string.Equals(to.Server, _confhost) ? MessageType.groupchat : MessageType.chat, message));
            }

            return Task.FromResult(0);
        }

        private void OnClientLogin(object sender)
        {
            var mucManager = new MucManager(_client);

            var rooms = _api.GetAllRooms();
            foreach (var room in rooms.Items.Where(r => _roomList.Count == 0 || r.Name != null && _roomList.Contains(r.Name.ToUpper())))
            {
                var roomInfo = _api.GetRoom(room.Id);

                _roomMap.Add(roomInfo.XmppJid, room.Id);

                var jid = new Jid(roomInfo.XmppJid);
                mucManager.JoinRoom(jid, _botUser.Name);
                Rooms.Add(room.Name);
                LogRooms.Add(room.Name);
                Logger.Info(string.Format("Joined Room '{0}'", room.Name));
            }
        }

        private void OnClientRosterItem(object sender, RosterItem item)
        {
            
            if (!_roster.ContainsKey(item.Jid.User))
            {
                _roster.Add(item.Jid.User, item.Name);
            }
            
            //Would like to make this keyed off something unique
            if (!_nicks.ContainsKey(item.Name))
            {
                _nicks.Add(item.Name, item.GetAttribute("mention_name"));
            }
            else
            {
                Logger.Warn(string.Format("User '{0}' already has a nickname '{1}': Jid User: {2} ...",
                    item.Name,
                    _nicks[item.Name],
                    item.Jid.User));
            }
            
            Logger.Info(string.Format("User '{0}' with nickname '{1}' logged in", item.Name, item.GetAttribute("mention_name")));
        }

        private void OnClientRosterStart(object sender)
        {
        }

        public override Task Topic(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (envelope != null && envelope.User != null)
            {
                return Topic(envelope.User.Room, adapterArgs, messages);
            }

            return Task.FromResult(0);
        }

        public override Task Topic(string roomName, AdapterArguments adapterArgs, params string[] messages)
        {
            var mucManager = new MucManager(_client);
            mucManager.ChangeSubject(new Jid(roomName), string.Join(" ", messages));

            return Task.FromResult(0);
        }

        public override Task Close()
        {
            _client.Close();
            _client = null;

            return Task.FromResult(0);
        }
    }
}
