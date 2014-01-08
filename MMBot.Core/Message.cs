namespace MMBot
{
    public class Message
    {
        public User User { get; protected set; }

        public bool Done { get; set; }

        public void Finish()
        {
            Done = true;
        }
    }

    public class TextMessage : Message
    {
        public TextMessage(User user, string text, string id)
        {
            User = user;
            Text = text;
        }

        public string Text { get; private set; }
    }

    public class EnterMessage : Message
    {
        public EnterMessage(User user)
        {
            User = user;
        }
    }

    public class LeaveMessage : Message
    {
        public LeaveMessage(User user)
        {
            User = user;
        }
    }

    public class TopicMessage : Message
    {
        public TopicMessage(User user, string topic)
        {
            User = user;
            Topic = topic;
        }

        public string Topic { get; private set; }
    }

    public class CatchAllMessage : Message
    {
        public CatchAllMessage(User user, string textData)
        {
            User = user;
            Text = textData;
        }

        public string Text { get; private set; }
    }
}