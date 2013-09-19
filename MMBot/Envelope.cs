using System.Globalization;
using System.Runtime.Remoting.Messaging;

namespace MMBot
{
    public class Envelope
    {
        public Envelope(Message message)
        {
            User = message.User;
            if (message is TextMessage)
            {
                Message = ((TextMessage) message).Text;
            }
        }

        public User User { get; set; }

        public string Room { get; set; }

        public string Message { get; set; }

        public HttpWrapper Http(string url)
        {
            return new HttpWrapper(url);
        }
    }
}