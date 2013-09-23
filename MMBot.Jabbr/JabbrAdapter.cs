using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabbR.Client;

namespace MMBot.Jabbr
{
    public class JabbrAdapter : Adapter
    {
        private JabbRClient _client;

        // TODO: Move to environment variables / config
        private static string _host;
        private static string[] _rooms;
        private static string _nick;
        private static string _password;

        private static bool _isConfigured = false;

        private void Configure()
        {
            _host = _robot.GetConfigVariable("HUBOT_JABBR_HOST");
            _nick = _robot.GetConfigVariable("HUBOT_JABBR_NICK");
            _password = _robot.GetConfigVariable("HUBOT_JABBR_PASSWORD");
            _rooms = (_robot.GetConfigVariable("HUBOT_JABBR_ROOMS") ?? string.Empty)
                .Trim()
                .Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            _isConfigured = _host != null;
        }

        public JabbrAdapter(Robot robot) : base(robot)
        {
            Configure();
        }

        private void SetupJabbrClient()
        {
            if (_client != null)
            {
                return;
            }

            _client = new JabbRClient(_host);
            _client.MessageReceived += ClientOnMessageReceived;

            _client.UserJoined += (user, room, isOwner) => { Console.WriteLine("{0} joined {1}", user.Name, room); };

            _client.UserLeft += (user, room) => { Console.WriteLine("{0} left {1}", user.Name, room); };

            _client.PrivateMessage += (from, to, message) => { Console.WriteLine("*PRIVATE* {0} -> {1} ", @from, message); };
        }

        private void ClientOnMessageReceived(JabbR.Client.Models.Message message, string room)
        {
            Console.WriteLine("[{0}] {1}: {2}", message.When, message.User.Name, message.Content);

            // TODO: implement user lookup
            //user = self.robot.brain.userForName msg.name
            //unless user?
            //    id = (new Date().getTime() / 1000).toString().replace('.','')
            //    user = self.robot.brain.userForId id
            //    user.name = msg.name

            var user = new User(message.User.Name, message.User.Name, new string[0], room);

            //TODO: Filter out messages from mmbot itself using the current nick
            if(user.Name != _nick)
            {
                _robot.Receive(new TextMessage(user, message.Content, message.Id));
            }
        }

        public override async Task Run()
        {
            if (!_isConfigured)
            {
                throw new AdapterNotConfiguredException();
            }
            Console.WriteLine("Logging into JabbR...");

            SetupJabbrClient();

            var result = await _client.Connect(_nick, _password);

            Console.WriteLine("Logged on successfully. {0} is currently in the following rooms:", _nick);
            foreach (var room in result.Rooms)
            {
                Console.WriteLine(" - " + room.Name + (room.Private ? " (private)" : string.Empty));
            }

            foreach (var room in _rooms.Where(room => !result.Rooms.Select(r => r.Name).Contains(room)))
            {
                try
                {
                    await _client.JoinRoom(room);
                    Console.WriteLine("Successfully joined room {0}", room);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Could not join room {0}: {1}", room, e.Message);
                }
            }
        }

        public override Task Close()
        {
            _client.Disconnect();
            return TaskAsyncHelper.Empty;
        }

        public override async Task Topic(Envelope envelope, params string[] messages)
        {
            await base.Topic(envelope, messages);
            
            if (envelope != null && envelope.Room != null)
            {
                var message = string.Join(" ", messages);
                await _client.Send(string.Format("/topic {0}", message.Substring(0, Math.Min(80, message.Length))), envelope.Room);
            }
        }

        public override async Task Topic(string roomName, params string[] messages)
        {
            await base.Topic(roomName, messages);

            var room = await _client.GetRoomInfo(roomName);
            if(room != null)
            {
                var message = string.Join(" ", messages);
                await _client.Send(string.Format("/topic {0}", message.Substring(0, Math.Min(80, message.Length))), room.Name);
            }
        }

        public override async Task Send(Envelope envelope, params string[] messages)
        {
            await base.Send(envelope, messages);

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

        public override async Task Reply(Envelope envelope, params string[] messages)
        {
            await base.Reply(envelope, messages);

            foreach (var message in messages.Where(message => !string.IsNullOrWhiteSpace(message)))
            {
                await _client.Send(string.Format("@{0} {1}", envelope.User.Name, message), envelope.User.Room);
            }
        }
    }
}
