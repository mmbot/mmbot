using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MMBot
{
    public abstract class Response
    {
        public static IResponse<T> Create<T>(Robot robot, T message, MatchResult matchResult) where T : Message
        {
            if (message is TextMessage)
            {
                return new Response<T>(robot, message, matchResult);
            }

            throw new NotImplementedException();
        }

        public static IResponse<T> Create<T>(Robot robot, T message) where T : Message
        {
            if (message is EnterMessage || message is LeaveMessage || message is CatchAllMessage)
            {
                return new Response<T>(robot, message);
            }

            throw new NotImplementedException();
        }
    }

    public interface IResponse<out T> where T : Message
    {
        Task Send(params string[] messages);
        Task Send(IDictionary<string, string> adapterArgs, params string[] messages);
        Task SendFormat(string format, params object[] args);
        Task SendFormat(IDictionary<string, string> adapterArgs, string format, params object[] args);
        Task Reply(params string[] message);
        Task Reply(IDictionary<string, string> adapterArgs, params string[] message);
        Task ReplyFormat(string format, params object[] args);
        Task ReplyFormat(IDictionary<string, string> adapterArgs, string format, params object[] args);
        Task Emote(params string[] message);
        Task Emote(IDictionary<string, string> adapterArgs, params string[] message);
        Task Topic(params string[] message);
        Task Topic(IDictionary<string, string> adapterArgs, params string[] message);
        Task Play(params string[] message);
        Task Play(IDictionary<string, string> adapterArgs, params string[] message);
        TRand Random<TRand>(IEnumerable<TRand> message);

        void Finish();
        string[] Match { get; }
        MatchCollection Matches { get; }
        T Message { get; }

        HttpWrapper Http(string url);
    }

    public class Response<T> : IResponse<T> where T : Message
    {
        private readonly Robot _robot;
        private readonly Envelope _envelope;

        public Response(Robot robot, T textMessage, MatchResult matchResult)
        {
            _robot = robot;

            _envelope = new Envelope(textMessage);
            Matches = matchResult.Match;
            Match = matchResult.Match == null || matchResult.Match.Count == 0 ? new string[0] : matchResult.Match[0].Groups.Cast<Group>().Select(g => g.Value).ToArray();
            Message = textMessage;
        }

        public Response(Robot robot, T rosterMessage)
        {
            _robot = robot;
            _envelope = new Envelope(rosterMessage);
            Message = rosterMessage;
        }

        public Task Send(IDictionary<string, string> adapterArgs, params string[] messages)
        {
            var adapter = FindAdapter();
            if (adapter == null) return Task.FromResult(0);

            return adapter.Send(_envelope, adapterArgs, messages);
        }

        public Task Send(params string[] messages)
        {
            return Send(_robot.EmptyAdapterArgs, messages);
        }

        public Task SendFormat(IDictionary<string, string> adapterArgs, string format, params object[] args)
        {
            return Send(adapterArgs, string.Format(format, args));
        }

        public Task SendFormat(string format, params object[] args)
        {
            return Send(string.Format(format, args));
        }

        public Task Reply(IDictionary<string, string> adapterArgs, params string[] message)
        {
            var adapter = FindAdapter();
            if (adapter == null) return Task.FromResult(0);

            return adapter.Reply(_envelope, adapterArgs, message);
        }

        public Task Reply(params string[] message)
        {
            return Reply(_robot.EmptyAdapterArgs, message);
        }

        public Task ReplyFormat(IDictionary<string, string> adapterArgs, string format, params object[] args)
        {
            return Reply(adapterArgs, string.Format(format, args));
        }

        public Task ReplyFormat(string format, params object[] args)
        {
            return Reply(_robot.EmptyAdapterArgs, string.Format(format, args));
        }

        public Task Emote(IDictionary<string, string> adapterArgs, params string[] message)
        {
            var adapter = FindAdapter();
            if (adapter == null) return Task.FromResult(0);

            return adapter.Emote(_envelope, adapterArgs, message);
        }

        public Task Emote(params string[] message)
        {
            return Emote(_robot.EmptyAdapterArgs, message);
        }

        public Task Topic(IDictionary<string, string> adapterArgs, params string[] message)
        {
            var adapter = FindAdapter();
            if (adapter == null) return Task.FromResult(0);

            return adapter.Topic(_envelope, adapterArgs, message);
        }

        public Task Topic(params string[] message)
        {
            return Topic(_robot.EmptyAdapterArgs, message);
        }

        public Task Play(IDictionary<string, string> adapterArgs, params string[] message)
        {
            var adapter = FindAdapter();
            if (adapter == null) return Task.FromResult(0);

            return adapter.Play(_envelope, adapterArgs, message);
        }

        public Task Play(params string[] message)
        {
            return Play(_robot.EmptyAdapterArgs, message);
        }

        static Random _random = new Random(DateTime.Now.Millisecond);
        public TRand Random<TRand>(IEnumerable<TRand> messages)
        {
            if (messages == null || !messages.Any())
            {
                return default(TRand);
            }
            return messages.ElementAt(_random.Next(messages.Count()));
        }

        public void Finish()
        {
            Message.Finish();
        }

        public string[] Match { get; private set; }

        public MatchCollection Matches { get; private set; }

        public T Message { get; private set; }

        public HttpWrapper Http(string url)
        {
            return new HttpWrapper(url, _robot.Logger, _envelope);
        }

        private IAdapter FindAdapter()
        {
            var adapter = _robot.GetAdapter(_envelope.User.AdapterId);

            if (adapter == null)
            {
                _robot.Logger.Warn(string.Format("Could not find adapter matching key '{0}'", _envelope.User.AdapterId));
            }

            return adapter;
        }
    }
}