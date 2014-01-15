using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using agsXMPP.sasl;
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
        private bool _isConfigured;
        private XmppClientConnection _xmppConnection;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();
        private TaskCompletionSource<bool> _loginTcs;

        public XmppAdapter(Robot robot, ILog logger, string adapterId) : base(robot, logger, adapterId)
        {
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

            
            if (_host == null || _connectHost == null | _username == null  || _password == null)
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The XMPP adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("  MMBOT_XMPP_HOST - Typically gmail.com or your google apps domain e.g. mydomain.com. Defaults to gmail.com");
                helpSb.AppendLine("  MMBOT_XMPP_CONNECT_HOST - in the case of GTalk this is always talk.google.com. Defaults to talk.google.com");
                helpSb.AppendLine("  MMBOT_XMPP_USERNAME - the part of mmbot's email address before the @");
                helpSb.AppendLine("  MMBOT_XMPP_PASSWORD - the password");
                helpSb.AppendLine("  MMBOT_XMPP_ROOMS: A comma separated list of room names that mmbot should join");
                helpSb.AppendLine( "More info on these values and how to create the mmbot.ini file can be found at https://github.com/PeteGoo/mmbot/wiki/Configuring-mmbot");
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
                Thread.Sleep(8000);
                _loginTcs.TrySetResult(false);
            });

            if (!connect.Result)
                throw new TimeoutException("XMPP adapter timed out while trying to login");

            return _loginTcs.Task;
            //return _loginTcs == null ? Task.FromResult(false) :  _loginTcs.Task;
            //return Task.FromResult(true);
        }


        private void XmppConnectionOnOnPresence(object sender, Presence pres)
        {
            //TODO do something for autojoin
        }


        private void OnMessage(object sender, agsXMPP.protocol.client.Message message)
        {
            if (!String.IsNullOrEmpty(message.Body))
            {
                Console.WriteLine("Message : {0} - from {1}", message.Body, message.From);

                string user = string.Format("{0}@{1}/{2}", message.From.User, message.From.Server, message.From.Resource);

                var content = message.Body.Trim();

                // Prefix with the alias if we do not have one. Presume that XMPP is chatting just to us
                if (!content.StartsWith(Robot.Alias ?? Robot.Name, StringComparison.OrdinalIgnoreCase))
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
            //if (_loginTcs != null)
            //{
            //    _loginTcs.SetException(ex);
            //    _loginTcs = null;
            //}
        }

        private void OnLogin(object sender)
        {
            Logger.Info("Logged into " + _xmppConnection.Server);
            _loginTcs.TrySetResult(true);
        }

        private void OnXmppConnectionStateChanged(object sender, XmppConnectionState state)
        {
            Logger.Info("XMPP connection changed - " + state.GetDescription());
        }

        public override Task Close()
        {
            CancelPreviousLogin();

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
            _loginTcs.TrySetCanceled();
        }

        private void OnClientRosterItem(object sender, RosterItem item)
        {
            
            if (!_roster.ContainsKey(item.Jid.User))
            {
                _roster.Add(item.Jid.User, item.Name);
                Logger.Info(string.Format("User '{0}' logged in", item.Name));
            }
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);
            _xmppConnection.Send(new agsXMPP.protocol.client.Message(new Jid(envelope.User.Name), MessageType.chat, string.Join(Environment.NewLine, messages)));
        }
    }
}
