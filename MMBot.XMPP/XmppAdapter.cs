using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.protocol.x.muc;
using Common.Logging;

namespace MMBot.XMPP
{
    public class XmppAdapter : Adapter
    {
        private string _host;
        private string _connectHost;
        private string _username;
        private string _password;
        private string[] _rooms;
        private string[] _logRooms;
        private int _port;
        private string _confServer;
        private XmppClientConnection _xmppConnection;
        private TaskCompletionSource<bool> _loginTcs;
        private TaskCompletionSource<bool> _reconnectTcs;

        private bool _isConfigured = false;
        private object _connectSync = new object();
        private const int CONNECT_TIMEOUT = 8000;
        private const int RECONNECT_TIMEOUT = 20000;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();

        public XmppAdapter(ILog logger, string adapterId)
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
            _host = Robot.GetConfigVariable("MMBOT_XMPP_HOST") ?? "gmail.com";
            _connectHost = Robot.GetConfigVariable("MMBOT_XMPP_CONNECT_HOST") ?? "talk.google.com";
            _username = Robot.GetConfigVariable("MMBOT_XMPP_USERNAME");
            _password = Robot.GetConfigVariable("MMBOT_XMPP_PASSWORD");
            _rooms = (Robot.GetConfigVariable("MMBOT_XMPP_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            _logRooms = (Robot.GetConfigVariable("MMBOT_XMPP_LOGROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            int.TryParse(Robot.GetConfigVariable("MMBOT_XMPP_PORT"), out _port);
            _confServer = Robot.GetConfigVariable("MMBOT_XMPP_CONFERENCE_SERVER");

            if (_host == null || _connectHost == null | _username == null || _password == null)
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The XMPP adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("  MMBOT_XMPP_HOST - Typically gmail.com or your google apps domain e.g. mydomain.com. Defaults to gmail.com");
                helpSb.AppendLine("  MMBOT_XMPP_CONNECT_HOST - in the case of GTalk this is always talk.google.com. Defaults to talk.google.com");
                helpSb.AppendLine("  MMBOT_XMPP_USERNAME - the part of mmbot's email address before the @");
                helpSb.AppendLine("  MMBOT_XMPP_PASSWORD - the password");
                helpSb.AppendLine("  MMBOT_XMPP_CONFERENCE_SERVER - a conference server to use when connecting to rooms");
                helpSb.AppendLine("  MMBOT_XMPP_ROOMS - A comma separated list of room names that mmbot should join");
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
            _loginTcs = new TaskCompletionSource<bool>();
            Task<bool> connect = _loginTcs.Task;

            if (_xmppConnection != null)
            {
                _xmppConnection.Close();
                _xmppConnection = null;
            }
            _xmppConnection = new XmppClientConnection
            {
                Server = _host,
                ConnectServer = _connectHost,
                AutoResolveConnectServer = true,
                Username = _username,
                Password = _password
            };
            if (_port > 0) _xmppConnection.Port = _port;

            _xmppConnection.KeepAlive = true;

            _xmppConnection.OnLogin += OnLogin;
            _xmppConnection.OnError += OnError;
            _xmppConnection.OnMessage += OnMessage;
            _xmppConnection.OnPresence += XmppConnectionOnOnPresence;
            _xmppConnection.OnRosterItem += OnClientRosterItem;
            _xmppConnection.OnXmppConnectionStateChanged += OnXmppConnectionStateChanged;

            Task.Factory.StartNew(() =>
            {
                _xmppConnection.Open();
                Thread.Sleep(CONNECT_TIMEOUT);
                _loginTcs.TrySetResult(false);
            });

            if (!connect.Result)
                throw new TimeoutException("XMPP adapter timed out while trying to login");
            else
            {
                MucManager muc = new MucManager(_xmppConnection);
                foreach (var room in _rooms)
                {
                    try
                    {
                        muc.JoinRoom(string.Format("{0}@{1}", room, _confServer), _username, _password, true);
                        Logger.Info(string.Format("Successfully joined room {0}", room));
                        Rooms.Add(string.Format("{0}@{1}", room, _confServer));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to join room - " + ex.Message);
                    }
                }

                foreach (var logroom in _logRooms)
                {
                    try
                    {
                        muc.JoinRoom(string.Format("{0}@{1}", logroom, _confServer), _username, _password, true);
                        Logger.Info(string.Format("Successfully joined room {0}", logroom));
                        LogRooms.Add(string.Format("{0}@{1}", logroom, _confServer));
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to join room - " + ex.Message);
                    }
                }
            }

            return _loginTcs.Task;
        }

        private void XmppConnectionOnOnPresence(object sender, Presence pres)
        {
            //TODO do something for autojoin
        }

        private void OnMessage(object sender, agsXMPP.protocol.client.Message message)
        {
            if (!String.IsNullOrEmpty(message.Body) && message.From.Resource != _username)
            {
                string user = string.Format("{0}@{1}/{2}", message.From.User, message.From.Server, message.From.Resource);

                var content = message.Body.Trim();

                if (message.From.Server != _confServer && !content.StartsWith(Robot.Alias ?? Robot.Name, StringComparison.OrdinalIgnoreCase))
                {
                    content = string.Concat(Robot.Alias ?? Robot.Name, " ", content);
                }

                Logger.Info(string.Format("[{0}] {1}: {2}", DateTime.Now, user, content));

                var userObj = Robot.GetUser(message.Id, user, message.From.Bare, Id);

                if (userObj.Name != _username)
                {
                    Task.Run(() =>
                    {
                        Robot.Receive(new TextMessage(userObj, content));
                    });
                }
            }
        }

        private void OnError(object sender, Exception ex)
        {
            Logger.Error("XMPP Error - " + ex.Message);
        }

        private void OnLogin(object sender)
        {
            Logger.Info("Logged into " + _xmppConnection.Server);
            _loginTcs.TrySetResult(true);
        }

        private void OnXmppConnectionStateChanged(object sender, XmppConnectionState state)
        {
            if (state == XmppConnectionState.Disconnected)
            {
                Logger.Warn("XMPP connection is disconnected");
                if (_loginTcs.Task.IsCompleted)
                {
                    lock (_connectSync)
                    {
                        AttemptReconnect();
                    }
                }
            }
            else if (state != XmppConnectionState.Connecting)
            {
                Logger.Info("XMPP connection changed - " + state.GetDescription());
            }
        }

        private void AttemptReconnect()
        {
            while (_xmppConnection == null || (_xmppConnection != null && !_xmppConnection.Authenticated) || _xmppConnection.XmppConnectionState == XmppConnectionState.Disconnected)
            {
                try
                {
                    Close();
                }
                catch { }

                try
                {
                    Run();
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to reconnect - " + ex.Message);
                }
                Thread.Sleep(RECONNECT_TIMEOUT);
            }
        }

        public override Task Close()
        {
            CancelPreviousLogin();

            Rooms.Clear();
            LogRooms.Clear();
            _xmppConnection.OnRosterItem -= OnClientRosterItem;
            _xmppConnection.OnLogin -= OnLogin;
            _xmppConnection.OnError -= OnError;
            _xmppConnection.OnMessage -= OnMessage;
            _xmppConnection.Close();
            _xmppConnection = null;

            return Task.FromResult(1);
        }

        private void CancelPreviousLogin()
        {
            if (_loginTcs != null)
            {
                _loginTcs.TrySetCanceled();
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

        public override Task Send(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (_confServer.HasValue() && envelope.User.Room.Contains(_confServer))
            {
                _xmppConnection.Send(new agsXMPP.protocol.client.Message(envelope.User.Room, MessageType.groupchat, string.Join(Environment.NewLine, messages)));
            }
            else
            {
                _xmppConnection.Send(new agsXMPP.protocol.client.Message(new Jid(envelope.User.Name), MessageType.chat, string.Join(Environment.NewLine, messages)));
            }

            return Task.FromResult(0);
        }
    }
}