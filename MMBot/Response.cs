using System.Text.RegularExpressions;

namespace MMBot
{
    public class Response<T> where T : Message
    {
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