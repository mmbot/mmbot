namespace MMBot
{
    public class Message
    {
        public User User { get; private set; }

        public bool Done { get; set; }

        public void Finish()
        {
            Done = true;
        }
    }

    public class TextMessage : Message
    {
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