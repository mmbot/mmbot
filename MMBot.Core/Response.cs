using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
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
        Task Reply(params string[] message);
        Task Emote(params string[] message);
        Task Topic(params string[] message);
        Task Play(params string[] message);
        Task Locked(params string[] message);
        T Random<T>(IEnumerable<T> message);

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

        public async Task Send(params string[] messages)
        {
            var adapter = _robot.GetAdapter(_envelope.User.AdapterId);

            if (adapter == null)
            {
                _robot.Logger.Warn(string.Format("Could not find adapter matching key '{0}'", _envelope.User.AdapterId));
                return;
            }
            await _robot.Adapters[_envelope.User.AdapterId].Send(_envelope, messages);
        }

        public Task Reply(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task Emote(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task Topic(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task Play(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        public Task Locked(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        static Random _random = new Random(DateTime.Now.Millisecond);
        public T Random<T>(IEnumerable<T> messages)
        {
            if (messages == null || !messages.Any())
            {
                return default(T);
            }
            return messages.ElementAt(_random.Next(messages.Count() - 1));
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
        
    }
}