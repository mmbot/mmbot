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
            Id = id;
        }

        public string Id { get; private set; }
        public string Text { get; private set; }
    }

    public class EnterMessage : Message
    {

    }

    public class LeaveMessage : Message
    {

    }

    public class TopicMessage : Message
    {

    }

    public class CatchAllMessage : Message
    {

    }
}