using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;
using MMBot.Jabbr.JabbrClient.Models;
using Newtonsoft.Json.Linq;
using Message = MMBot.Jabbr.JabbrClient.Models.Message;

namespace MMBot.Jabbr.JabbrClient
{

    public static class ClientEvents
    {
        public static readonly string AddMessage = "addMessage";
        public static readonly string AddMessageContent = "addMessageContent";
        public static readonly string AddUser = "addUser";
        public static readonly string Leave = "leave";
        public static readonly string LogOn = "logOn";
        public static readonly string LogOut = "logOut";
        public static readonly string Kick = "kick";
        public static readonly string UpdateRoom = "updateRoom";
        public static readonly string UpdateActivity = "updateActivity";
        public static readonly string MarkInactive = "markInactive";
        public static readonly string SendPrivateMessage = "sendPrivateMessage";
        public static readonly string SetTyping = "setTyping";
        public static readonly string JoinRoom = "joinRoom";
        public static readonly string RoomCreated = "roomCreated";
        public static readonly string GravatarChanged = "changeGravatar";
        public static readonly string MeMessageReceived = "sendMeMessage";
        public static readonly string UsernameChanged = "changeUserName";
        public static readonly string NoteChanged = "changeNote";
        public static readonly string FlagChanged = "changeFlag";
        public static readonly string TopicChanged = "changeTopic";
        public static readonly string OwnerAdded = "addOwner";
        public static readonly string OwnerRemoved = "removeOwner";
    }

    public class DefaultAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _url;

        public DefaultAuthenticationProvider(string url)
        {
            _url = url;
        }

        public async Task<HubConnection> Connect(string userName, string password)
        {
            var authUri = new UriBuilder(_url);
            authUri.Path += authUri.Path.EndsWith("/") ? "account/login" : "/account/login";

            var cookieJar = new CookieContainer();

#if PORTABLE
            var handler = new HttpClientHandler
            {
#else
            var handler = new WebRequestHandler
            {
#endif
                CookieContainer = cookieJar
            };

            var client = new HttpClient(handler);

            var parameters = new Dictionary<string, string> {
                { "username" , userName },
                { "password" , password }
            };

            var response = await client.PostAsync(authUri.Uri, new FormUrlEncodedContent(parameters));

            response.EnsureSuccessStatusCode();

            // Create a hub connection and give it our cookie jar
            var connection = new HubConnection(_url)
            {
                CookieContainer = cookieJar
            };

            return connection;
        }
    }

    /// <summary>
    /// Interface that wraps SignalR's IClientTransport and provides a way to add authentication information
    /// </summary>
    public interface IAuthenticationProvider
    {
        Task<HubConnection> Connect(string userName, string password);
    }

    public interface IJabbRClient
    {
        event Action<Models.Message, string> MessageReceived;
        event Action<IEnumerable<string>> LoggedOut;
        event Action<User, string, bool> UserJoined;
        event Action<User, string> UserLeft;
        event Action<string> Kicked;
        event Action<string, string, string> PrivateMessage;
        event Action<User, string> UserTyping;
        event Action<User, string> GravatarChanged;
        event Action<string, string, string> MeMessageReceived;
        event Action<string, User, string> UsernameChanged;
        event Action<User, string> NoteChanged;
        event Action<User, string> FlagChanged;
        event Action<string, string, string> TopicChanged;
        event Action<User, string> OwnerAdded;
        event Action<User, string> OwnerRemoved;
        event Action<string, string, string> AddMessageContent;
        event Action<Room> JoinedRoom;
        event Action<Room> RoomChanged;
        event Action<User> UserActivityChanged;
        event Action<IEnumerable<User>> UsersInactive;
        event Action Disconnected;
        event Action<StateChange> StateChanged;

        string SourceUrl { get; }
        bool AutoReconnect { get; set; }
        ICredentials Credentials { get; set; }

        Task<LogOnInfo> Connect(string name, string password);
        Task<User> GetUserInfo();
        Task LogOut();
        Task<bool> Send(string message, string roomName);
        Task<bool> Send(ClientMessage message);
        Task CreateRoom(string roomName);
        Task JoinRoom(string roomName);
        Task LeaveRoom(string roomName);
        Task SetFlag(string countryCode);
        Task SetNote(string noteText);
        Task SendPrivateMessage(string userName, string message);
        Task Kick(string userName, string roomName);
        Task<bool> CheckStatus();
        Task SetTyping(string roomName);
        Task PostNotification(ClientNotification notification);
        Task PostNotification(ClientNotification notification, bool executeContentProviders);
        Task<IEnumerable<JabbrClient.Models.Message>> GetPreviousMessages(string fromId);
        Task<Room> GetRoomInfo(string roomName);
        Task<IEnumerable<Room>> GetRooms();
        void Disconnect();
    }

    public class JabbRClient : IJabbRClient
    {
        private readonly IAuthenticationProvider _defaultAuthenticationProvider;
        private readonly Func<IClientTransport> _transportFactory;

        private IHubProxy _chat;
        private HubConnection _connection;
        private IAuthenticationProvider _authenticationProvider;

        public JabbRClient(string url) :
            this(url, transportFactory: () => new AutoTransport(new DefaultHttpClient()))
        {
        }

        public JabbRClient(string url, Func<IClientTransport> transportFactory)
        {
            SourceUrl = url;
            _transportFactory = transportFactory;
            TraceLevel = TraceLevels.All;

            _defaultAuthenticationProvider = new DefaultAuthenticationProvider(url);
        }

        public event Action<JabbrClient.Models.Message, string> MessageReceived;
        public event Action<IEnumerable<string>> LoggedOut;
        public event Action<User, string, bool> UserJoined;
        public event Action<User, string> UserLeft;
        public event Action<string> Kicked;
        public event Action<string, string, string> PrivateMessage;
        public event Action<User, string> UserTyping;
        public event Action<User, string> GravatarChanged;
        public event Action<string, string, string> MeMessageReceived;
        public event Action<string, User, string> UsernameChanged;
        public event Action<User, string> NoteChanged;
        public event Action<User, string> FlagChanged;
        public event Action<string, string, string> TopicChanged;
        public event Action<User, string> OwnerAdded;
        public event Action<User, string> OwnerRemoved;
        public event Action<string, string, string> AddMessageContent;
        public event Action<Room> JoinedRoom;

        // Global
        public event Action<Room> RoomChanged;
        public event Action<User> UserActivityChanged;
        public event Action<IEnumerable<User>> UsersInactive;

        public string SourceUrl { get; private set; }
        public bool AutoReconnect { get; set; }
        public TextWriter TraceWriter { get; set; }
        public TraceLevels TraceLevel { get; set; }

        public IAuthenticationProvider AuthenticationProvider
        {
            get
            {
                return _authenticationProvider ?? _defaultAuthenticationProvider;
            }
            set
            {
                _authenticationProvider = value;
            }
        }

        public HubConnection Connection
        {
            get
            {
                return _connection;
            }
        }

        public ICredentials Credentials
        {
            get
            {
                return _connection.Credentials;
            }
            set
            {
                _connection.Credentials = value;
            }
        }

        public event Action Disconnected
        {
            add
            {
                _connection.Closed += value;
            }
            remove
            {
                _connection.Closed -= value;
            }
        }

        public event Action<StateChange> StateChanged
        {
            add
            {
                _connection.StateChanged += value;
            }
            remove
            {
                _connection.StateChanged -= value;
            }
        }

        public async Task<LogOnInfo> Connect(string name, string password)
        {
            _connection = await AuthenticationProvider.Connect(name, password);

            if (TraceWriter != null)
            {
                _connection.TraceWriter = TraceWriter;
            }

            _connection.TraceLevel = TraceLevel;

            _chat = _connection.CreateHubProxy("chat");

            SubscribeToEvents();

            await _connection.Start(_transportFactory());

            return await LogOn();
        }

        private async Task<LogOnInfo> LogOn()
        {
            var tcs = new TaskCompletionSource<LogOnInfo>();

            IDisposable logOn = null;

            // Wait for the logOn callback to get triggered
            logOn = _chat.On<IEnumerable<Room>, JArray>(ClientEvents.LogOn, (rooms, privateRooms) =>
            {
                logOn.Dispose();

                tcs.TrySetResult(new LogOnInfo
                {
                    Rooms = rooms,
                    UserId = (string)_chat["id"]
                });
            });

            // Join JabbR
            await _chat.Invoke("Join");

            return await tcs.Task;
        }

        public Task<User> GetUserInfo()
        {
            return _chat.Invoke<User>("GetUserInfo");
        }

        public Task LogOut()
        {
            return _chat.Invoke("LogOut");
        }

        public Task<bool> Send(string message, string roomName)
        {
            return _chat.Invoke<bool>("Send", message, roomName);
        }

        public Task<bool> Send(ClientMessage message)
        {
            return _chat.Invoke<bool>("Send", message);
        }

        public Task PostNotification(ClientNotification notification, bool executeContentProviders)
        {
            return _chat.Invoke("PostNotification", notification, executeContentProviders);
        }

        public Task PostNotification(ClientNotification notification)
        {
            return _chat.Invoke("PostNotification", notification);
        }

        public async Task CreateRoom(string roomName)
        {
            var tcs = new TaskCompletionSource<object>();

            IDisposable createRoom = null;

            createRoom = _chat.On<Room>(ClientEvents.RoomCreated, room =>
            {
                createRoom.Dispose();

                tcs.SetResult(null);
            });

            await SendCommand("create {0}", roomName);

            await tcs.Task;
        }

        public async Task JoinRoom(string roomName)
        {
            var tcs = new TaskCompletionSource<object>();

            IDisposable joinRoom = null;

            joinRoom = _chat.On<Room>(ClientEvents.JoinRoom, room =>
            {
                joinRoom.Dispose();

                tcs.SetResult(null);
            });

            await SendCommand("join {0}", roomName);

            await tcs.Task;
        }

        public Task LeaveRoom(string roomName)
        {
            return SendCommand("leave {0}", roomName);
        }

        public Task SetFlag(string countryCode)
        {
            return SendCommand("flag {0}", countryCode);
        }

        public Task SetNote(string noteText)
        {
            return SendCommand("note {0}", noteText);
        }

        public Task SendPrivateMessage(string userName, string message)
        {
            return SendCommand("msg {0} {1}", userName, message);
        }

        public Task Kick(string userName, string roomName)
        {
            return SendCommand("kick {0} {1}", userName, roomName);
        }

        public Task<bool> CheckStatus()
        {
            return _chat.Invoke<bool>("CheckStatus");
        }

        public Task SetTyping(string roomName)
        {
            return _chat.Invoke("Typing", roomName);
        }

        public Task<IEnumerable<Models.Message>> GetPreviousMessages(string fromId)
        {
            return _chat.Invoke<IEnumerable<Models.Message>>("GetPreviousMessages", fromId);
        }

        public Task<Room> GetRoomInfo(string roomName)
        {
            return _chat.Invoke<Room>("GetRoomInfo", roomName);
        }

        public Task<IEnumerable<Room>> GetRooms()
        {
            return _chat.Invoke<IEnumerable<Room>>("GetRooms");
        }

        public void Disconnect()
        {
            _connection.Stop();
        }

        private void SubscribeToEvents()
        {
            if (AutoReconnect)
            {
                Disconnected += OnDisconnected;
            }

            _chat.On<JabbrClient.Models.Message, string>(ClientEvents.AddMessage, (message, room) =>
            {
                Execute(MessageReceived, messageReceived => messageReceived(message, room));
            });

            _chat.On<IEnumerable<string>>(ClientEvents.LogOut, rooms =>
            {
                Execute(LoggedOut, loggedOut => loggedOut(rooms));
            });

            _chat.On<User, string, bool>(ClientEvents.AddUser, (user, room, isOwner) =>
            {
                Execute(UserJoined, userJoined => userJoined(user, room, isOwner));
            });

            _chat.On<User, string>(ClientEvents.Leave, (user, room) =>
            {
                Execute(UserLeft, userLeft => userLeft(user, room));
            });

            _chat.On<string>(ClientEvents.Kick, room =>
            {
                Execute(Kicked, kicked => kicked(room));
            });

            _chat.On<Room>(ClientEvents.UpdateRoom, (room) =>
            {
                Execute(RoomChanged, roomChanged => roomChanged(room));
            });

            _chat.On<User, string>(ClientEvents.UpdateActivity, (user, roomName) =>
            {
                Execute(UserActivityChanged, userActivityChanged => userActivityChanged(user));
            });

            _chat.On<string, string, string>(ClientEvents.SendPrivateMessage, (from, to, message) =>
            {
                Execute(PrivateMessage, privateMessage => privateMessage(from, to, message));
            });

            _chat.On<IEnumerable<User>>(ClientEvents.MarkInactive, (users) =>
            {
                Execute(UsersInactive, usersInactive => usersInactive(users));
            });

            _chat.On<User, string>(ClientEvents.SetTyping, (user, room) =>
            {
                Execute(UserTyping, userTyping => userTyping(user, room));
            });

            _chat.On<User, string>(ClientEvents.GravatarChanged, (user, room) =>
            {
                Execute(GravatarChanged, gravatarChanged => gravatarChanged(user, room));
            });

            _chat.On<string, string, string>(ClientEvents.MeMessageReceived, (user, content, room) =>
            {
                Execute(MeMessageReceived, meMessageReceived => meMessageReceived(user, content, room));
            });

            _chat.On<string, User, string>(ClientEvents.UsernameChanged, (oldUserName, user, room) =>
            {
                Execute(UsernameChanged, usernameChanged => usernameChanged(oldUserName, user, room));
            });

            _chat.On<User, string>(ClientEvents.NoteChanged, (user, room) =>
            {
                Execute(NoteChanged, noteChanged => noteChanged(user, room));
            });

            _chat.On<User, string>(ClientEvents.FlagChanged, (user, room) =>
            {
                Execute(FlagChanged, flagChanged => flagChanged(user, room));
            });

            _chat.On<string, string, string>(ClientEvents.TopicChanged, (roomName, topic, who) =>
            {
                Execute(TopicChanged, topicChanged => topicChanged(roomName, topic, who));
            });

            _chat.On<User, string>(ClientEvents.OwnerAdded, (user, room) =>
            {
                Execute(OwnerAdded, ownerAdded => ownerAdded(user, room));
            });

            _chat.On<User, string>(ClientEvents.OwnerRemoved, (user, room) =>
            {
                Execute(OwnerRemoved, ownerRemoved => ownerRemoved(user, room));
            });

            _chat.On<string, string, string>(ClientEvents.AddMessageContent, (messageId, extractedContent, roomName) =>
            {
                Execute(AddMessageContent, addMessageContent => addMessageContent(messageId, extractedContent, roomName));
            });

            _chat.On<Room>(ClientEvents.JoinRoom, (room) =>
            {
                Execute(JoinedRoom, joinedRoom => joinedRoom(room));
            });
        }

        private async void OnDisconnected()
        {
            await TaskAsyncHelper.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await _connection.Start(_transportFactory());

                // Join JabbR
                await _chat.Invoke("Join", false);
            }
            catch (Exception ex)
            {
                _connection.Trace(TraceLevels.Events, ex.Message);
            }
        }

        private void Execute<T>(T handlers, Action<T> action) where T : class
        {
            Task.Factory.StartNew(() =>
            {
                if (handlers != null)
                {
                    try
                    {
                        action(handlers);
                    }
                    catch (Exception ex)
                    {
                        _connection.Trace(TraceLevels.Events, ex.Message);
                    }
                }
            });
        }

        private Task SendCommand(string command, params object[] args)
        {
            return _chat.Invoke("Send", String.Format("/" + command, args), "");
        }
    }

    internal static class TaskAsyncHelper
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This is a shared file")]
        public static Task Delay(TimeSpan timeOut)
        {
#if NETFX_CORE || PORTABLE
            return Task.Delay(timeOut);
#else
            var tcs = new TaskCompletionSource<object>();

            var timer = new Timer(tcs.SetResult,
            null,
            timeOut,
            TimeSpan.FromMilliseconds(-1));

            return tcs.Task.ContinueWith(_ =>
            {
                timer.Dispose();
            },
            TaskContinuationOptions.ExecuteSynchronously);
#endif
        }
    }
}