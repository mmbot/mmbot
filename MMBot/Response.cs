using System;
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
    }

    public interface IResponse<out T> where T : Message
    {
        Task Send(params string[] messages);
        Task Reply(params string[] message);
        Task Emote(params string[] message);
        Task Topic(params string[] message);
        Task Play(params string[] message);
        Task Locked(params string[] message);
        Task Random(params string[] message);
        void Finish();
        MatchCollection Match { get; }
        T Message { get; }
    }

    public class Response<T> : IResponse<T> where T : Message
    {
        private readonly Robot _robot;
        private readonly Envelope _envelope;

        public Response(Robot robot, T textMessage, MatchResult matchResult)
        {
            _robot = robot;
            
            _envelope = new Envelope(textMessage);
            Match = matchResult.Match;
            Message = textMessage;
        }

        public async Task Send(params string[] messages)
        {
            await _robot.Adapter.Send(_envelope, messages);
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

        public Task Random(params string[] message)
        {
            return TaskAsyncHelper.Empty;
        }

        public void Finish()
        {
            Message.Finish();
        }

        public MatchCollection Match { get; private set; }

        public T Message { get; private set; }
        
    }
}