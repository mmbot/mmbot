namespace MMBot
{
    public class Envelope
    {
        public Envelope(Message message)
        {
            User = message.User;
            var textMessage = message as TextMessage;
            if (textMessage != null)
            {
                Message = textMessage.Text;
            }
        }

        public User User { get; set; }

        public string Room { get; set; }

        public string Message { get; set; }
    }
}