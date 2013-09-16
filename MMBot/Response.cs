using System;
using System.Text.RegularExpressions;

namespace MMBot
{
    public abstract class Response
    {
        public static IResponse<Message> Create(Robot robot, Message message, MatchResult matchResult)
        {
            if (message is TextMessage)
            {
                return new Response<TextMessage>(robot, message as TextMessage, matchResult);
            }

            throw new NotImplementedException();
        }
    }

    public interface IResponse<out T> where T : Message
    {
    }

    public class Response<T> : IResponse<T> where T : Message
    {
        public Response(Robot robot, TextMessage textMessage, MatchResult matchResult)
        {
            
        }

        public void Send(params string[] messages)
        {

        }

        public void Reply(params string[] message)
        {
            
        }

        public void Emote(params string[] message)
        {

        }

        public void Topic(params string[] message)
        {

        }

        public void Play(params string[] message)
        {

        }

        public void Locked(params string[] message)
        {

        }

        public void Random(params string[] message)
        {

        }

        public void Finish()
        {
            Message.Finish();
        }

        public MatchCollection Match { get; private set; }

        public T Message { get; private set; }
        
    }
}