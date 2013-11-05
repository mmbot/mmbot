using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using agsXMPP;
using agsXMPP.protocol.client;
using agsXMPP.protocol.iq.roster;
using Common.Logging;

namespace MMBot.XMPP
{
    public class XmppAdapter : Adapter
    {
        private string _host;
        private string _connectHost;
        private string _username;
        private string _password;
        private bool _isConfigured;
        private XmppClientConnection _xmppConnection;
        private readonly Dictionary<string, string> _roster = new Dictionary<string, string>();
        private TaskCompletionSource<bool> _loginTcs;
        private string _resource;

        public XmppAdapter(Robot robot, ILog logger, string adapterId) : base(robot, logger, adapterId)
        {
            Configure();
        }

        private void Configure()
        {
            _host = Robot.GetConfigVariable("MMBOT_XMPP_HOST");
            _connectHost = Robot.GetConfigVariable("MMBOT_XMPP_CONNECT_HOST");
            _username = Robot.GetConfigVariable("MMBOT_XMPP_USERNAME");
            _password = Robot.GetConfigVariable("MMBOT_XMPP_PASSWORD");
            _resource = Robot.GetConfigVariable("MMBOT_XMPP_RESOURCE");
            _isConfigured = _host != null;
        }

        public override Task Run()
        {
            _xmppConnection = new XmppClientConnection
            {
                Server = _host,
                ConnectServer = _connectHost,
                AutoResolveConnectServer = true,
                Username = _username,
                Password = _password,
                //Resource = _resource,
                //UseStartTLS = true,
                Port = 5222
                //UseSSL = false
            };

            _xmppConnection.OnLogin += OnLogin;
            _xmppConnection.OnError += OnError;
            _xmppConnection.OnMessage += OnMessage;
            _xmppConnection.OnRosterItem += OnClientRosterItem;

            CancelPreviousLogin();

            _loginTcs = new TaskCompletionSource<bool>();

            _xmppConnection.Open();

            //return _loginTcs == null ? Task.FromResult(false) :  _loginTcs.Task;
            return Task.FromResult(false);
        }

        private void OnMessage(object sender, agsXMPP.protocol.client.Message message)
        {
            if (!String.IsNullOrEmpty(message.Body))
            {
                Console.WriteLine("Message : {0} - from {1}", message.Body, message.From);

                string user = string.Format("{0}@{1}/{2}", message.From.User, message.From.Server, message.From.Resource);

                Logger.Info(string.Format("[{0}] {1}: {2}", DateTime.Now, user, message.Body.Trim()));

                var userObj = new User(message.Id, user, new string[0], message.From.Bare, Id);

                if (userObj.Name != _username)
                {
                    Task.Run(() =>
                        Robot.Receive(new TextMessage(userObj, message.Body.Trim(), message.Id)));
                }
            }
        }

        private void OnError(object sender, Exception ex)
        {
            //if (_loginTcs != null)
            //{
            //    _loginTcs.SetException(ex);
            //    _loginTcs = null;
            //}
        }

        private void OnLogin(object sender)
        {
            if (_loginTcs != null)
            {
                _loginTcs.TrySetResult(true);
                _loginTcs = null;
            }
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
            if (_loginTcs != null)
            {
                _loginTcs.SetCanceled();
                _loginTcs = null;
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

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);
            _xmppConnection.Send(new agsXMPP.protocol.client.Message(new Jid(envelope.User.Name), MessageType.chat, string.Join(Environment.NewLine, messages)));
        }
    }
}
