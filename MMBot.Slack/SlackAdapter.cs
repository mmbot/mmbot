using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Owin;
using MMBot.Router;
using ServiceStack;
using ServiceStack.Text;
using SuperSocket.ClientEngine;
using WebSocket4Net;
using System.Text.RegularExpressions;

namespace MMBot.Slack
{
    public class SlackAdapter : Adapter
    {
        private string _token;
        private bool _isConfigured;

        private SlackAPI _api;
        private WebSocket _socket;
        private List<User> _users;
        private List<Channel> _rooms;
        private List<Im> _ims;
        private string _botUserId;
        private bool _reconnect = true;
        private string[] _commandTokens;
        private string[] _logRooms;

        public SlackAdapter(ILog logger, string adapterId)
            : base(logger, adapterId)
        {
        }

        public override void Initialize(Robot robot)
        {
            base.Initialize(robot);

            _token = robot.GetConfigVariable("MMBOT_SLACK_TOKEN");
            _commandTokens = (robot.GetConfigVariable("MMBOT_SLACK_COMMANDTOKENS") ?? string.Empty)
                .Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();
            _logRooms = (robot.GetConfigVariable("MMBOT_SLACK_LOGROOMS") ?? string.Empty)
                .Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToArray();

            if (string.IsNullOrWhiteSpace(_token))
            {
                var helpSb = new StringBuilder();
                helpSb.AppendLine("The Slack adapter is not configured correctly and hence will not be enabled.");
                helpSb.AppendLine("To configure the Slack adapter, please set the following configuration properties:");
                helpSb.AppendLine("  MMBOT_SLACK_TOKEN: This is the service token you are given when you add your Bot to your Team Services.");
                helpSb.AppendLine("  MMBOT_SLACK_COMMANDTOKENS: Optional. The comma delimited list of expected command tokens from the Slack commands hook. If none supplied then any token will be accepted.");
                helpSb.AppendLine("  MMBOT_SLACK_LOGROOMS: Optional. The comma delimited list of rooms to send log messages to.");
                helpSb.AppendLine("More info on these values and how to create the mmbot.ini file can be found at https://github.com/mmbot/mmbot/wiki/Configuring-mmbot");
                Logger.Warn(helpSb.ToString());
                _isConfigured = false;
                return;
            }

            _isConfigured = true;

            Logger.Info("The Slack adapter is connected");
        }

        public override Task Run()
        {
            if (!_isConfigured)
            {
                throw new AdapterNotConfiguredException();
            }
            Logger.Info(string.Format("Logging into Slack"));

            if (_api == null)
            {
                _api = new SlackAPI(_token);
            }

            if (!(Robot.Router is NullRouter))
                Robot.Router.Post("/Slack/slack-command", (Action<OwinContext>)HandleSlackCommand);

            return ConnectSocket();
        }

        private void HandleSlackCommand(OwinContext context)
        {
            try
            {
                Logger.Info("Incoming command message received from Slack");

                var form = context.Form();
                var token = form["token"];

                if (_commandTokens.Any() && !_commandTokens.Contains(token))
                {
                    Logger.Warn(string.Format("An invalid token was received from the Slack command hook. Please check that the token '{0}' was expected.", token));
                    return;
                }

                var channel = form["channel_id"];
                var text = form["text"];
                var user = form["user_id"];

                ReceiveMessage(channel, user, Robot.Alias ?? Robot.Name + " " + text);
            }
            catch (Exception e)
            {
                Logger.Error("Error receiving Slack message", e);
            }
        }

        private async Task ConnectSocket()
        {
            for (var i = 0; i < 10 && _socket == null; i++)
            {
                var start = _api.RtmStart();
                if (!start.Ok)
                {
                    Logger.WarnFormat("Unable to start connection with Slack Adapter ({0}). Retrying", start.Error);
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                // URL only valid for 30sec
                _socket = new WebSocket(start.Url);

                _botUserId = start.Self.Id;

                _users = start.Users.ToList();
                _rooms = start.Channels.ToList();
                _ims = start.Ims.ToList();

                Rooms.Clear();
                Rooms.AddAll(start.Channels.Select(c => c.Name));

                LogRooms.Clear();
                LogRooms.AddAll(Rooms.Intersect(_logRooms, StringComparer.InvariantCultureIgnoreCase));
            }

            if (_socket == null)
            {
                Logger.Error("Unable to create socket for Slack Adapter");
                return;
            }

            _socket.Closed += SocketOnClose;
            _socket.Error += SocketOnError;
            _socket.MessageReceived += SocketOnMessage;

            _socket.Open();

            Logger.Info("Slack socket connected");
        }

        private void SocketOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            Logger.ErrorFormat("Slack Socket Error - {0}", errorEventArgs.Exception, errorEventArgs.Exception.Message);
        }

        private async void SocketOnClose(object sender, EventArgs closeEventArgs)
        {
            _socket.Close();
            _socket = null;

            while (_reconnect && _socket == null)
            {
                await ConnectSocket();

                if (_socket == null)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                }
            }
        }

        private void SocketOnMessage(object sender, MessageReceivedEventArgs messageEventArgs)
        {
            var raw = JsonObject.Parse(messageEventArgs.Message);

            if (raw["type"] == "message")
            {
                if (raw.ContainsKey("subtype"))
                {
                    // Subtypes are things like channel join messages, etc.
                    return;
                }

                var channel = raw["channel"];
                var text = raw["text"];
                var user = raw["user"];

                text = RemoveFormatting(text);

                ReceiveMessage(channel, user, text);
            }

            if (raw["type"] == "team_join")
            {
                HandleTeamJoinMessage(raw);
            }

            if (raw["type"] == "channel_created")
            {
                HandleChannelCreatedMessage(raw);
            }

            if (raw["type"] == "channel_deleted")
            {
                HandleChannelDeletedMessage(raw);
            }

            if (raw["type"] == "channel_rename")
            {
                HandleChannelRenameMessage(raw);
            }
        }

        /// <summary>
        /// Reformat the message according to Slack's Message Formatting
        /// </summary>
        /// <param name="text">Raw text from slack</param>
        /// <returns>Text with formatting removed.</returns>
        private string RemoveFormatting(string text)
        {
            Regex regex = new Regex(@"<(?<type>[@#!])?(?<link>[^>|]+)(?:\|(?<label>[^>]+))?>");

            text = regex.Replace(text, m =>
            {

                switch (m.Groups["type"].Value)
                {
                    case "@":
                        if (m.Groups["label"].Success) return m.Groups["label"].Value;

                        var user = _users.SingleOrDefault(u => m.Groups["link"].Value == u.Id);
                        if (user != null) return user.Name;
                        break;
                    case "#":
                        if (m.Groups["label"].Success) return m.Groups["label"].Value;

                        var channel = _rooms.SingleOrDefault(r => m.Groups["link"].Value == r.Id);
                        if (channel != null) return channel.Name;
                        break;
                    case "!":
                        string[] links = {"channel","group","everyone","here"};
                        if(links.Contains(m.Groups["link"].Value))
                        {
                            return String.Format("@{0}", m.Groups["link"].Value);
                        }
                        break;
                    default:
                        string link = m.Groups["link"].Value.Replace("mailto:", "");
                        if (link == m.Groups["label"].Value)
                        {
                            return String.Format("{0} ({1})", m.Groups["label"].Value, link);
                        }
                        else
                        {
                            return m.Groups["link"].Value;
                        }
                        break;
                }

                return m.Value;

            });

            return text;

        }

        private void HandleTeamJoinMessage(JsonObject raw)
        {
            var newUser = raw.GetUnescaped("user").FromJson<User>();

            Logger.DebugFormat("User {0}({1} joined the team", newUser.Id, newUser.Name);

            if (!newUser.IsBot)
            {
                _users.Add(newUser);
            }
        }

        private void HandleChannelCreatedMessage(JsonObject raw)
        {
            var channel = raw.GetUnescaped("channel").FromJson<Channel>();

            Logger.DebugFormat("Channel {0}({1}) created", channel.Id, channel.Name);

            AddRoom(channel);
        }

        private void HandleChannelDeletedMessage(JsonObject raw)
        {
            var id = raw["channel"];
            var channel = _rooms.FirstOrDefault(c => StringComparer.InvariantCultureIgnoreCase.Equals(c.Id, id));
            if (channel == null)
            {
                return;
            }
            var name = channel.Name;

            Logger.DebugFormat("Channel {0}({1}) deleted", id, name);

            RemoveRoom(channel);
        }

        private void HandleChannelRenameMessage(JsonObject raw)
        {
            var channel = raw.GetUnescaped("channel").FromJson<Channel>();
            var oldChannel = _rooms.FirstOrDefault(c => StringComparer.InvariantCultureIgnoreCase.Equals(c.Id, channel.Id));
            if (oldChannel == null)
            {
                return;
            }
            var oldName = oldChannel.Name;

            Logger.DebugFormat("Channel {0} renamed from {1} to {2}", channel.Id, oldName, channel.Name);

            if (!string.IsNullOrEmpty(oldName))
            {
                RemoveRoom(oldChannel);
            }

            AddRoom(channel);
        }

        private void ReceiveMessage(string channelId, string userId, string text)
        {
            if (userId == _botUserId)
            {
                // Don't respond to self
                return;
            }

            var user = _users.FirstOrDefault(u => StringComparer.InvariantCultureIgnoreCase.Equals(u.Id, userId));
            if (user == null)
            {
                // Message probably came from an integration. Move on
                return;
            }

            var userObj = Robot.GetUser(userId, user.Name, channelId, Id);

            if (!text.StartsWithIgnoreCase(Robot.Alias ?? Robot.Name + " ") &&
                _ims.Any(im => StringComparer.InvariantCultureIgnoreCase.Equals(im.Id, channelId)))
            {
                text = (Robot.Alias ?? Robot.Name) + " " + text;
            }

            Robot.Receive(new TextMessage(userObj, text.Trim()));
        }

        private void RemoveRoom(Channel channel)
        {
            _rooms.Remove(channel);
            Rooms.Remove(channel.Name);
            LogRooms.Remove(channel.Name);
        }

        private void AddRoom(Channel channel)
        {
            _rooms.Add(channel);
            Rooms.Add(channel.Name);
            if (_logRooms.Contains(channel.Name, StringComparer.InvariantCultureIgnoreCase))
            {
                LogRooms.Add(channel.Name);
            }
        }

        private void EnsureBotInRoom(Channel channel)
        {
            if (channel.IsMember)
                return;

            var response = _api.ChannelsJoin(channel.Name);

            if (!response.Ok)
            {
                Logger.ErrorFormat("Could not join channel {0} ({1})", channel.Name, response.Error);
                return;
            }

            channel.IsMember = true;
        }

        private void EnsureBotInRoom(Im directMessage)
        {
            if (directMessage.IsOpen)
                return;

            var response = _api.ImOpen(directMessage.User);

            if (!response.Ok)
            {
                Logger.ErrorFormat("Could not join im channel {0} ({1})", directMessage.User, response.Error);
                return;
            }

            directMessage.IsOpen = true;
        }

        private void InternalSend(string destination, AdapterArguments adapterArgs, string[] messages)
        {
            var room = destination;

            var channel = _rooms.FirstOrDefault(r => StringComparer.InvariantCultureIgnoreCase.Equals(r.Id, room) ||
                StringComparer.InvariantCultureIgnoreCase.Equals(r.Name, room));

            if (channel != null)
            {
                room = channel.Id;
                //EnsureBotInRoom(channel);

                if (!channel.IsMember)
                {
                    // Currently bots cannot self enter a room.
                    // Instead we'll just log for now.
                    Logger.ErrorFormat("Bots cannot join rooms. Invite bot into room {0}({1})", channel.Id, channel.Name);
                    return;
                }
            }

            var im = _ims.FirstOrDefault(i => StringComparer.InvariantCultureIgnoreCase.Equals(i.Id, room) ||
                StringComparer.InvariantCultureIgnoreCase.Equals(i.User, room));

            if (im != null)
            {
                room = im.Id;
                EnsureBotInRoom(im);
            }

            var user = _users.FirstOrDefault(u => StringComparer.InvariantCultureIgnoreCase.Equals(u.Id, room) ||
                StringComparer.InvariantCultureIgnoreCase.Equals(u.Name, room));

            if (user != null)
            {
                var response = _api.ImOpen(user.Id);
                if (!response.Ok)
                {
                    Logger.ErrorFormat("Could not join im channel {0} ({1})", user.Id, response.Error);
                    return;
                }
                room = response.Channel;
                im = new Im() { Id = response.Channel, User = user.Id, IsOpen = true };
                _ims.Add(im);
            }

            Logger.DebugFormat("Trying to send message to room {0}({1})", room, destination);

            foreach (var message in messages)
            {
                SlackAPI.Send(_socket, room, message);
            }
        }

        public override Task Send(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (_socket == null)
            {
                return base.Send(envelope, adapterArgs, messages);
            }

            var room = envelope.Room ?? envelope.User.Room;

            InternalSend(room, adapterArgs, messages);

            return base.Send(envelope, adapterArgs, messages);
        }

        public override Task Reply(Envelope envelope, AdapterArguments adapterArgs, params string[] messages)
        {
            if (_socket == null)
            {
                return base.Reply(envelope, adapterArgs, messages);
            }

            var room = envelope.User.Id;

            InternalSend(room, adapterArgs, messages);

            return base.Reply(envelope, adapterArgs, messages);
        }

        public override Task Close()
        {
            _reconnect = false;
            if (_socket != null)
            {
                _socket.Close();
            }
            return Task.FromResult(0);
        }
    }
}