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
        private static string[] _rooms;
        private static string _nick;
        private static string _password;

        private static bool _isConfigured = false;
        
        private XmppClientConnection _client = null;
        private string _username;
        private string _confhost;
        private string _roomNick;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();


        public HipChatAdapter(Robot robot, ILog logger) : base(robot, logger)
        {
            Configure();
        }

        private void Configure() {
            _host = Robot.GetConfigVariable("MMBOT_HIPCHAT_HOST");
            _confhost = Robot.GetConfigVariable("MMBOT_HIPCHAT_CONFHOST");
            _nick = Robot.GetConfigVariable("MMBOT_HIPCHAT_NICK");
            _roomNick = Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMNICK");
            _username = Robot.GetConfigVariable("MMBOT_HIPCHAT_USERNAME");
            _password = Robot.GetConfigVariable("MMBOT_HIPCHAT_PASSWORD");
            _rooms = (Robot.GetConfigVariable("MMBOT_HIPCHAT_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            _isConfigured = _host != null;
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
            
            _client = new XmppClientConnection(_host);
            _client.AutoResolveConnectServer = false;
            _client.OnLogin += OnClientLogin;
            _client.OnMessage += OnClientMessage;
            _client.OnError += OnClientError;
            _client.OnAuthError += OnClientAuthError;
            _client.Resource = "bot";
            _client.UseStartTLS = true;

            Logger.Info(string.Format("Connecting to {0}", _host));
            _client.Open(_username, _password);
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

                if (user == _roomNick)
                    return;

                
                Logger.Info(string.Format("[{0}] {1}: {2}", DateTime.Now, user, message.Body.Trim()));

                var userObj = new User(message.Id, user, new string[0], message.From.Bare);

                if (userObj.Name != _nick)
                {
                    Task.Run(() =>
                        Robot.Receive(new TextMessage(userObj, message.Body.Trim(), message.Id)));
                }
            }
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);

            if (messages == null || !messages.Any()) return;

            foreach (var message in messages)
            {
                var to = new Jid(envelope.User.Room);
                _client.Send(new agsXMPP.protocol.client.Message(to, string.Equals(to.Server, _confhost) ? MessageType.groupchat : MessageType.chat, message));
            }
        }

        private void OnClientLogin(object sender)
        {
            var mucManager = new MucManager(_client);

            foreach (string room in _rooms)
            {
                var jid = new Jid(room + "@" + _confhost);
                mucManager.JoinRoom(jid, _roomNick);
                Logger.Info(string.Format("Joined Room '{0}'", room));
            }
        }

        private void OnClientRosterItem(object sender, RosterItem item)
        {
            if (!_roster.ContainsKey(item.Jid.User))
            {
                _roster.Add(item.Jid.User, item.Name);
                Logger.Info(string.Format("User '{0}' logged in", item.Name));
            }
        }

        private void OnClientRosterStart(object sender)
        {

        }

        public override async Task Topic(Envelope envelope, params string[] messages)
        {
            if(envelope != null && envelope.User != null)
            {
                await Topic(envelope.User.Room, messages);
            }
        }

        public override async Task Topic(string roomName, params string[] messages)
        {
            var mucManager = new MucManager(_client);
            mucManager.ChangeSubject(new Jid(roomName), string.Join(" ", messages));
        }
        

        public override async Task Close()
        {
            _client.Close();
            _client = null;
        }


    }
}
